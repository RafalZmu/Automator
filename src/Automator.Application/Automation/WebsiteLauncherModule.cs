using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Automator.Application.Launcher;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

public sealed record WebsiteLauncherSite(string Id, string Name, string Url);
public sealed record WebsiteLauncherGroup(string Id, string Name, IReadOnlyList<WebsiteLauncherSite> Websites);
public sealed record WebsiteLauncherRow(string Id, string Name, string Alias, IReadOnlyList<WebsiteLauncherGroup> Groups);
public sealed record WebsiteLauncherSettings(IReadOnlyList<WebsiteLauncherRow> Rows);
public sealed record WebsiteLauncherQuickAction(string Id, string Name, string Alias);

/// <summary>Launches saved website sets through the Windows default browser.</summary>
public sealed class WebsiteLauncherModule : ILauncherTabModuleProvider
{
    public const string IdValue = "website-launcher";
    public const int SettingsVersionValue = 1;
    private static readonly Regex IdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex AliasPattern = new("^[a-z]{1,32}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly AutomationCapabilityRequirement WebsiteLaunch = new(AutomationCapabilityIds.WebsiteLaunch, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    public AutomationModuleDefinition Definition { get; } = new(8, IdValue, "Website Launcher", "globe", "website-launcher", true,
        1, SettingsVersionValue, [WebsiteLaunch], [new("launchRow", 1, [WebsiteLaunch])]);
    public int ContractVersion => Definition.ContractVersion;
    public string Id => Definition.Id;
    public string Title => Definition.Title;
    public LauncherModuleState CreateInitialState() => new(Id, LauncherTabRegistry.Version,
        new Dictionary<string, string>(StringComparer.Ordinal) { ["status"] = "ready" });

    public JsonElement CreateDefaultSettings() => JsonSerializer.SerializeToElement(new WebsiteLauncherSettings([]), JsonOptions);

    public JsonElement MigrateSettings(int fromVersion, JsonElement value) =>
        throw new InvalidOperationException($"Website Launcher settings version {fromVersion} cannot be migrated.");

    public async ValueTask<AutomationResult> ExecuteAsync(string actionId, JsonElement input, JsonElement moduleSettings,
        AutomationServicesContext services, CancellationToken cancellationToken)
    {
        if (actionId != "launchRow") return Result(AutomationStatus.Error, "Unknown Website Launcher action.", new { });
        try
        {
            var settings = ReadAndValidateSettings(moduleSettings);
            if (input.ValueKind != JsonValueKind.Object || !input.TryGetProperty("rowId", out var rowIdValue)
                || rowIdValue.ValueKind != JsonValueKind.String || !IdPattern.IsMatch(rowIdValue.GetString()!))
                throw new InvalidDataException("A valid website shortcut is required.");
            var row = settings.Rows.FirstOrDefault(candidate => candidate.Id == rowIdValue.GetString())
                ?? throw new InvalidDataException("That website shortcut no longer exists.");
            var groups = row.Groups.Select(group => (IReadOnlyList<Uri>)group.Websites
                .Select(site => new Uri(site.Url, UriKind.Absolute)).ToArray()).ToArray();
            var launcher = services.WebsiteLauncher ?? throw new InvalidOperationException("Website launch is unavailable.");
            await launcher.LaunchAsync(groups, cancellationToken).ConfigureAwait(false);
            return Result(AutomationStatus.Success, $"Opened {row.Name} in the default browser.", new { rowId = row.Id, groupCount = groups.Length });
        }
        catch (OperationCanceledException) { throw; }
        catch (InvalidDataException exception) { return Result(AutomationStatus.Error, exception.Message, new { }); }
        catch (JsonException) { return Result(AutomationStatus.Error, "Website shortcut settings are invalid.", new { }); }
        catch { return Result(AutomationStatus.Error, "The website shortcut could not be opened in the default browser.", new { }); }
    }

    public static WebsiteLauncherSettings ReadAndValidateSettings(JsonElement value)
    {
        var settings = JsonSerializer.Deserialize<WebsiteLauncherSettings>(value.GetRawText(), JsonOptions)
            ?? throw new InvalidDataException("Website shortcut settings are invalid.");
        if (settings.Rows is null || settings.Rows.Count > 64) throw new InvalidDataException("Website shortcut settings contain too many rows.");
        var rowIds = new HashSet<string>(StringComparer.Ordinal);
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in settings.Rows)
        {
            if (row is null || !IdPattern.IsMatch(row.Id ?? string.Empty) || !rowIds.Add(row.Id!))
                throw new InvalidDataException("A website shortcut has an invalid or duplicated key.");
            if (string.IsNullOrWhiteSpace(row.Name) || row.Name.Trim().Length > 128 || HasControls(row.Name))
                throw new InvalidDataException("Website shortcut names are required and limited to 128 characters.");
            if (!AliasPattern.IsMatch(row.Alias ?? string.Empty) || !aliases.Add(row.Alias!))
                throw new InvalidDataException("Website shortcut aliases must be unique and contain 1 to 32 letters.");
            if (row.Groups is null || row.Groups.Count is < 1 or > 16)
                throw new InvalidDataException("Each website shortcut needs 1 to 16 browser groups.");
            var groupIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in row.Groups)
            {
                if (group is null || !IdPattern.IsMatch(group.Id ?? string.Empty) || !groupIds.Add(group.Id!))
                    throw new InvalidDataException("A browser group has an invalid or duplicated key.");
                if (string.IsNullOrWhiteSpace(group.Name) || group.Name.Trim().Length > 64 || HasControls(group.Name))
                    throw new InvalidDataException("Browser group names are required and limited to 64 characters.");
                if (group.Websites is null || group.Websites.Count is < 1 or > 32)
                    throw new InvalidDataException("Each browser group needs 1 to 32 websites.");
                var websiteIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var website in group.Websites)
                {
                    if (website is null || !IdPattern.IsMatch(website.Id ?? string.Empty) || !websiteIds.Add(website.Id!))
                        throw new InvalidDataException("A website has an invalid or duplicated key.");
                    if (string.IsNullOrWhiteSpace(website.Name) || website.Name.Trim().Length > 128 || HasControls(website.Name))
                        throw new InvalidDataException("Website names are required and limited to 128 characters.");
                    if (website.Url is null || website.Url.Length > 2048 || !Uri.TryCreate(website.Url, UriKind.Absolute, out var uri)
                        || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || string.IsNullOrWhiteSpace(uri.Host)
                        || !string.IsNullOrEmpty(uri.UserInfo))
                        throw new InvalidDataException("Websites must use an absolute HTTP or HTTPS URL.");
                }
            }
        }
        return settings;
    }

    public static IReadOnlyList<WebsiteLauncherQuickAction> GetQuickActions(JsonElement value) =>
        ReadAndValidateSettings(value).Rows.Select(row => new WebsiteLauncherQuickAction(row.Id, row.Name, row.Alias)).ToArray();

    public static string SerializeQuickActions(JsonElement value) =>
        JsonSerializer.Serialize(GetQuickActions(value), JsonOptions);

    private static bool HasControls(string value) => value.Any(char.IsControl);
    private static AutomationResult Result(AutomationStatus status, string message, object data) =>
        new(AutomationTabContract.CurrentVersion, status, message, JsonSerializer.SerializeToElement(data, JsonOptions), []);
}
