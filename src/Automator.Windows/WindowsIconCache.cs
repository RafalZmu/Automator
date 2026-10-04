using System.Collections.Concurrent;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Automator.Application.Logging;

namespace Automator.Windows;

/// <summary>Extracts shell-associated Windows icons once per normalized executable path.</summary>
public sealed class WindowsIconCache
{
    private const int MaximumDataUrlLength = 12_000;
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly IApplicationLog _log;

    public WindowsIconCache(IApplicationLog? log = null) => _log = log ?? NullApplicationLog.Instance;

    public string? GetDataUrl(string targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath)) return null;
        string key;
        try { key = Path.GetFullPath(targetPath); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            _log.Write(ApplicationLogLevel.Warning, "Catalog.IconPathInvalid", "Could not normalize an app icon path.", exception,
                new Dictionary<string, object?> { ["path"] = targetPath });
            return null;
        }
        var data = _cache.GetOrAdd(key, ExtractDataUrl);
        return data.Length == 0 ? null : data;
    }

    private string ExtractDataUrl(string path)
    {
        if (!File.Exists(path)) return string.Empty;
        try
        {
            using var icon = Icon.ExtractAssociatedIcon(path);
            if (icon is null) return string.Empty;
            using var bitmap = icon.ToBitmap();
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            var dataUrl = "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
            if (dataUrl.Length <= MaximumDataUrlLength) return dataUrl;
            _log.Write(ApplicationLogLevel.Warning, "Catalog.IconTooLarge", "An app icon exceeded the renderer payload limit and was omitted.",
                properties: new Dictionary<string, object?> { ["path"] = path, ["length"] = dataUrl.Length });
            return string.Empty;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or System.Runtime.InteropServices.ExternalException)
        {
            _log.Write(ApplicationLogLevel.Warning, "Catalog.IconExtractionFailed", "Could not extract the app icon.", exception,
                new Dictionary<string, object?> { ["path"] = path });
            return string.Empty;
        }
    }
}
