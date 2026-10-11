import path from 'node:path';
import * as guards from 'typescript/unstable/ast/is';

const normalize = value => String(value).replaceAll('\\', '/').replace(/^\.\//, '');
const nodePath = node => node.getSourceFile().fileName;
const declarationKinds = ['isFunctionDeclaration', 'isMethodDeclaration', 'isConstructorDeclaration', 'isFunctionExpression', 'isArrowFunction'];
const isFunctionNode = node => declarationKinds.some(guard => guards[guard](node));
const isCallNode = node => guards.isCallExpression(node) || guards.isNewExpression(node);
const lineOf = (source, node) => source.getLineAndCharacterOfPosition(node.getStart(source)).line + 1;
const nameText = name => name && (guards.isIdentifier(name) || guards.isPrivateIdentifier(name) || guards.isStringLiteral(name)) ? name.text : undefined;

function functionName(node) {
  if (guards.isConstructorDeclaration(node)) return 'constructor';
  const direct = nameText(node.name);
  if (direct) return direct;
  const parent = node.parent;
  if (parent && guards.isVariableDeclaration(parent)) return nameText(parent.name);
  if (parent && guards.isPropertyAssignment(parent)) return nameText(parent.name);
  return undefined;
}

function ancestors(node, source) {
  const result = [];
  for (let parent = node.parent; parent && parent !== source; parent = parent.parent) {
    if (isFunctionNode(parent)) {
      const name = functionName(parent);
      if (name) result.unshift(name);
    } else if ((guards.isClassDeclaration(parent) || guards.isClassExpression(parent)) && parent.name) result.unshift(parent.name.text);
  }
  return result;
}

function fnKind(node) {
  if (guards.isConstructorDeclaration(node)) return 'constructor';
  if (guards.isMethodDeclaration(node)) return 'method';
  if (guards.isArrowFunction(node) || guards.isFunctionExpression(node)) return functionName(node) ? 'arrow' : 'callback';
  return 'function';
}

function callbackLabel(node) {
  for (let parent = node.parent; parent && !isFunctionNode(parent); parent = parent.parent) {
    if (guards.isCallExpression(parent) && guards.isPropertyAccessExpression(parent.expression)) return parent.expression.name.text;
  }
  return 'callback';
}

function unwrap(node) {
  while (node && (guards.isParenthesizedExpression(node) || guards.isAsExpression(node) || guards.isTypeAssertion(node) || guards.isNonNullExpression(node) || guards.isSatisfiesExpression(node))) node = node.expression;
  return node;
}

function visit(node, callback) {
  callback(node);
  node.forEachChild(child => visit(child, callback));
}

export function scanTypeScript({ root, paths, ts }) {
  const absoluteRoot = path.resolve(root);
  const configPath = path.join(absoluteRoot, 'tsconfig.json');
  const api = typeof ts.API === 'function' ? new ts.API({ cwd: absoluteRoot }) : ts;
  const snapshot = api.updateSnapshot({ openProjects: [configPath] });
  const project = snapshot.getProject(configPath);
  if (!project) throw new Error(`TypeScript did not load project ${configPath}.`);
  const program = project.program;
  const checker = project.checker;
  const selected = new Set(paths.map(value => path.resolve(absoluteRoot, value)));
  const sourceFiles = [...selected].map(filePath => program.getSourceFile(filePath)).filter(Boolean);
  const sourcePaths = new Map(sourceFiles.map(source => [path.resolve(source.fileName), normalize(path.relative(absoluteRoot, source.fileName))]));
  const functions = [];
  const calls = [];
  const dependencies = [];
  const diagnostics = [];
  const keyBySymbol = new Map();
  const keyByNode = new Map();
  const functionNodes = [];
  const classNodes = [];

  for (const source of sourceFiles) {
    const sourcePath = sourcePaths.get(path.resolve(source.fileName));
    visit(source, node => {
      if (isFunctionNode(node)) functionNodes.push({ source, sourcePath, node });
      if (guards.isClassDeclaration(node) && node.name) classNodes.push({ source, sourcePath, node });
      if (guards.isImportDeclaration(node) || guards.isExportDeclaration(node)) {
        const specifier = node.moduleSpecifier;
        if (specifier && guards.isStringLiteral(specifier)) {
          const moduleSymbol = checker.getSymbolAtLocation(specifier);
          const declarations = moduleSymbol?.declarations?.map(handle => handle.resolve(project)).filter(Boolean) ?? [];
          const resolvedSources = new Set(declarations.map(declaration => sourcePaths.get(path.resolve(nodePath(declaration)))).filter(Boolean));
          if (!resolvedSources.size) {
            diagnostics.push({ path: sourcePath, line: lineOf(source, node), code: 'unresolved-module', message: `Could not resolve module ${specifier.text} to an indexed source file.` });
          } else {
            for (const targetPath of resolvedSources) dependencies.push({ from: targetPath, to: sourcePath, evidence: `${sourcePath}:${lineOf(source, node)}` });
          }
        }
      }
    });
  }

  for (const { source, sourcePath, node } of functionNodes) {
    const name = functionName(node);
    const qualifiedName = [...ancestors(node, source), name ?? `${callbackLabel(node)}.callback@${lineOf(source, node)}`].join('.');
    const nameNode = node.name ?? (node.parent && guards.isVariableDeclaration(node.parent) ? node.parent.name : undefined);
    const declarationSymbol = nameNode ? checker.getSymbolAtLocation(nameNode) : undefined;
    const symbolKey = `ts:${sourcePath}:${declarationSymbol?.escapedName ?? name ?? 'callback'}:${node.getStart(source)}`;
    const signature = checker.typeToString(checker.getTypeAtLocation(node), node);
    functions.push({ path: sourcePath, symbolKey, qualifiedName, signature, kind: fnKind(node), startLine: lineOf(source, node), endLine: source.getLineAndCharacterOfPosition(node.getEnd()).line + 1 });
    keyByNode.set(node, symbolKey);
    if (declarationSymbol) keyBySymbol.set(declarationSymbol.id, symbolKey);
  }

  for (const { source, sourcePath, node } of classNodes) {
    const declarationSymbol = checker.getSymbolAtLocation(node.name);
    const explicitConstructor = node.members.find(guards.isConstructorDeclaration);
    let constructorKey = explicitConstructor && keyByNode.get(explicitConstructor);
    if (!constructorKey) {
      constructorKey = `ts:${sourcePath}:${declarationSymbol?.escapedName ?? node.name.text}:constructor`;
      functions.push({ path: sourcePath, symbolKey: constructorKey, qualifiedName: `${node.name.text}.constructor`, signature: checker.typeToString(checker.getTypeAtLocation(node), node), kind: 'constructor', startLine: lineOf(source, node), endLine: source.getLineAndCharacterOfPosition(node.getEnd()).line + 1 });
    }
    if (declarationSymbol) keyBySymbol.set(declarationSymbol.id, constructorKey);
  }

  const resolveSymbol = symbol => {
    if (!symbol) return undefined;
    try {
      if (symbol.flags & ts.SymbolFlags.Alias) symbol = checker.getAliasedSymbol(symbol);
    } catch { return undefined; }
    if (keyBySymbol.has(symbol.id)) return keyBySymbol.get(symbol.id);
    for (const handle of symbol.declarations ?? []) {
      const declaration = handle.resolve(project);
      if (declaration) {
        const key = keyByNode.get(declaration);
        if (key) return key;
      }
    }
    return undefined;
  };

  for (const { source, sourcePath, node: functionNode } of functionNodes) {
    const callerKey = keyByNode.get(functionNode);
    const scan = node => {
      if (node !== functionNode && isFunctionNode(node)) return;
      if (isCallNode(node)) {
        const line = lineOf(source, node);
        const expression = unwrap(node.expression);
        const symbol = guards.isIdentifier(expression) || guards.isPropertyAccessExpression(expression) ? checker.getSymbolAtLocation(guards.isPropertyAccessExpression(expression) ? expression.name : expression) : undefined;
        const calleeKey = resolveSymbol(symbol);
        if (calleeKey) calls.push({ callerKey, calleeKey, path: sourcePath, line });
        else {
          const declarations = symbol?.declarations?.map(handle => handle.resolve(project)).filter(Boolean) ?? [];
          const external = declarations.some(declaration => !sourcePaths.has(path.resolve(nodePath(declaration))));
          diagnostics.push({ path: sourcePath, line, code: external ? 'external-call' : 'unresolved-call', message: external ? `Call to external symbol ${symbol.name} is outside indexed paths.` : `Could not resolve call target ${expression.getText(source)} to an indexed function.` });
        }
      }
      node.forEachChild(scan);
    };
    scan(functionNode);
  }

  const result = {
    files: [...sourcePaths.values()].sort().map(filePath => ({ path: filePath })),
    functions: functions.sort((a, b) => a.path.localeCompare(b.path) || a.startLine - b.startLine || a.symbolKey.localeCompare(b.symbolKey)),
    calls: calls.sort((a, b) => a.path.localeCompare(b.path) || a.line - b.line || a.callerKey.localeCompare(b.callerKey)),
    dependencies: dependencies.sort((a, b) => a.evidence.localeCompare(b.evidence) || a.from.localeCompare(b.from)),
    diagnostics: diagnostics.sort((a, b) => a.path.localeCompare(b.path) || a.line - b.line || a.code.localeCompare(b.code)),
  };
  snapshot.dispose();
  if (api !== ts) api.close();
  return result;
}
