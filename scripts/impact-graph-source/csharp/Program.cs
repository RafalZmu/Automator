using System.Text.Json;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            var (root, output, requestedProject) = Parse(args);
            if (!MSBuildLocator.IsRegistered) MSBuildLocator.RegisterDefaults();
            var result = await Index(root, requestedProject);
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            await File.WriteAllTextAsync(output, json);
            Console.Out.WriteLine(json);
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static async Task<IndexResult> Index(string root, string? requestedProject)
    {
        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, e) => Console.Error.WriteLine(e.Diagnostic.Message);
        IEnumerable<string> projectPaths = requestedProject is not null
            ? new[] { Path.GetFullPath(requestedProject) }
            : Directory.GetFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Path.Combine(root, "tests"), "*.csproj", SearchOption.AllDirectories))
                .Where(p => Path.GetFileName(p).EndsWith(".Specs.csproj", StringComparison.OrdinalIgnoreCase)
                         || Path.GetDirectoryName(p)!.StartsWith(Path.Combine(root, "src") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
        var documents = new List<(SyntaxTree Tree, SemanticModel Model, string Path)>();
        foreach (var projectPath in projectPaths.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var project = await workspace.OpenProjectAsync(projectPath);
            foreach (var doc in project.Documents)
            {
                if (doc.FilePath is null || !doc.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || doc.FilePath.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || doc.FilePath.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
                var tree = await doc.GetSyntaxTreeAsync(); var model = await doc.GetSemanticModelAsync();
                if (tree is not null && model is not null) documents.Add((tree, model, Rel(root, doc.FilePath)));
            }
        }
        var functions = new List<Function>();
        var keys = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);
        foreach (var d in documents)
        foreach (var node in d.Tree.GetRoot().DescendantNodesAndSelf())
        {
            var symbol = FunctionSymbol(node, d.Model);
            if (symbol is null) continue;
            var key = StableKey(symbol, d.Path, node.SpanStart); keys[symbol] = key;
            var line = Line(d.Tree, node.SpanStart);
            functions.Add(new Function(d.Path, key, QualifiedName(symbol, d.Path, line), symbol.ToDisplayString(Signature), Kind(node),
                line, Line(d.Tree, Math.Max(node.SpanStart, node.Span.End - 1))));
        }

        var typePaths = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);
        foreach (var d in documents)
        foreach (var node in d.Tree.GetRoot().DescendantNodesAndSelf())
            if (node is TypeDeclarationSyntax or EnumDeclarationSyntax or DelegateDeclarationSyntax)
                if (d.Model.GetDeclaredSymbol(node) is { } type) typePaths[type.OriginalDefinition] = d.Path;

        var calls = new List<Call>(); var dependencies = new List<Dependency>(); var diagnostics = new List<object>();
        foreach (var d in documents)
        foreach (var node in d.Tree.GetRoot().DescendantNodesAndSelf())
        {
            if (node is InvocationExpressionSyntax invocation)
            {
                var target = d.Model.GetSymbolInfo(invocation).Symbol;
                var caller = CallerKey(invocation, d.Model, keys);
                if (target is not null && caller is not null && keys.TryGetValue(target.OriginalDefinition, out var callee))
                    calls.Add(new Call(caller, callee, d.Path, Line(d.Tree, invocation.SpanStart)));
                else if (target is null && invocation.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(id => d.Model.GetTypeInfo(id).Type?.TypeKind == TypeKind.Dynamic))
                    diagnostics.Add(new { path = d.Path, line = Line(d.Tree, invocation.SpanStart), kind = "unresolved-dynamic-call", message = invocation.Expression.ToString() });
                else if (target is not null && caller is not null && target.ContainingAssembly?.Name != d.Model.Compilation.AssemblyName)
                    diagnostics.Add(new { path = d.Path, line = Line(d.Tree, invocation.SpanStart), kind = "external-call", message = target.ToDisplayString() });
            }
            else if (node is ObjectCreationExpressionSyntax creation)
            {
                var target = d.Model.GetSymbolInfo(creation).Symbol as IMethodSymbol;
                var caller = CallerKey(creation, d.Model, keys);
                if (target is not null && caller is not null && keys.TryGetValue(target.OriginalDefinition, out var callee))
                    calls.Add(new Call(caller, callee, d.Path, Line(d.Tree, creation.SpanStart)));
            }

            if (node is IdentifierNameSyntax or GenericNameSyntax or ObjectCreationExpressionSyntax or BaseTypeSyntax)
            {
                var type = ResolveType(node, d.Model);
                if (type is not null && typePaths.TryGetValue(type.OriginalDefinition, out var from) && from != d.Path)
                    dependencies.Add(new Dependency(from, d.Path, d.Path, Line(d.Tree, node.SpanStart), "type-reference"));
            }
        }
        return new IndexResult(documents.Select(d => d.Path).Distinct().Order().ToArray(), functions.OrderBy(x => x.path).ThenBy(x => x.startLine).ToArray(),
            calls.Distinct().OrderBy(x => x.path).ThenBy(x => x.line).ToArray(), dependencies.Distinct().OrderBy(x => x.path).ThenBy(x => x.line).ToArray(), diagnostics);
    }

    private static ISymbol? FunctionSymbol(SyntaxNode n, SemanticModel m) => n switch
    {
        BaseMethodDeclarationSyntax x => m.GetDeclaredSymbol(x),
        LocalFunctionStatementSyntax x => m.GetDeclaredSymbol(x),
        AnonymousFunctionExpressionSyntax x => m.GetSymbolInfo(x).Symbol,
        _ => null
    };
    private static string Kind(SyntaxNode n) => n switch
    {
        ConstructorDeclarationSyntax => "constructor", LocalFunctionStatementSyntax => "local-function",
        AnonymousFunctionExpressionSyntax => "lambda", _ => "method"
    };
    private static string? CallerKey(SyntaxNode node, SemanticModel m, Dictionary<ISymbol, string> keys)
    {
        foreach (var ancestor in node.Ancestors())
            if (FunctionSymbol(ancestor, m) is { } symbol && keys.TryGetValue(symbol, out var key)) return key;
        return null;
    }
    private static ITypeSymbol? ResolveType(SyntaxNode n, SemanticModel m)
    {
        if (n is ObjectCreationExpressionSyntax creation) return m.GetTypeInfo(creation).Type;
        if (m.GetSymbolInfo(n).Symbol is ITypeSymbol symbol) return symbol;
        var type = m.GetTypeInfo(n).Type;
        return type?.TypeKind == TypeKind.Error ? null : type;
    }
    private static int Line(SyntaxTree tree, int offset) => tree.GetLineSpan(new Microsoft.CodeAnalysis.Text.TextSpan(offset, 0)).StartLinePosition.Line + 1;
    private static string Rel(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
    private static string QualifiedName(ISymbol symbol, string path, int line)
    {
        if (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction }) return $"lambda@{path}:{line}";
        if (symbol is IMethodSymbol method)
        {
            var owner = method.ContainingType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "") ?? "";
            var name = method.MethodKind == MethodKind.Constructor ? method.ContainingType?.Name ?? method.Name : method.Name;
            return $"{owner}.{name}({string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))})";
        }
        return $"lambda@{path}:{line}";
    }
    private static string StableKey(ISymbol symbol, string path, int offset)
    {
        var prefix = $"{symbol.ContainingAssembly?.Identity.Name}:{symbol.Kind}:{symbol.ToDisplayString(Signature)}";
        if (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction }) return $"{prefix}@{path}:{offset}";
        if (symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction }) prefix += $" in {symbol.ContainingSymbol?.ToDisplayString(Qualified)}";
        return $"{prefix}@{path}";
    }
    private static (string Root, string Output, string? Project) Parse(string[] args)
    {
        string? root = null, output = null, project = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (++i >= args.Length) throw new ArgumentException("Missing CLI value");
            switch (args[i - 1]) { case "--root": root = args[i]; break; case "--output": output = args[i]; break; case "--project": project = args[i]; break; default: throw new ArgumentException($"Unknown argument {args[i - 1]}"); }
        }
        return (Path.GetFullPath(root ?? throw new ArgumentException("--root is required")), Path.GetFullPath(output ?? throw new ArgumentException("--output is required")), project);
    }

    private static readonly SymbolDisplayFormat Qualified = new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType);
    private static readonly SymbolDisplayFormat Signature = new(memberOptions: SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeModifiers | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName, miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
    private sealed record IndexResult(string[] files, Function[] functions, Call[] calls, Dependency[] dependencies, List<object> diagnostics);
    private sealed record Function(string path, string symbolKey, string qualifiedName, string signature, string kind, int startLine, int endLine);
    private sealed record Call(string callerKey, string calleeKey, string path, int line);
    private sealed record Dependency(string fromPath, string toPath, string path, int line, string relation);
}
