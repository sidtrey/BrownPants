using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BrownPants.Cli.Analysis;

internal static class ChartThemeParser
{
    internal const int MaximumThemeBytes = 16 * 1024;

    private static readonly string[] TopLevelProperties =
    ["schemaVersion", "name", "canvas", "text", "connector", "edgeLabelBackground", "edgeLabelText", "shadow", "shapes"];

    private static readonly (string Name, LocalFlowchartNodeShape Shape)[] ShapeProperties =
    [
        ("start", LocalFlowchartNodeShape.Start),
        ("process", LocalFlowchartNodeShape.Process),
        ("decision", LocalFlowchartNodeShape.Decision),
        ("inputOutput", LocalFlowchartNodeShape.InputOutput),
        ("person", LocalFlowchartNodeShape.Person),
        ("cloud", LocalFlowchartNodeShape.Cloud),
        ("end", LocalFlowchartNodeShape.End)
    ];

    private static readonly Regex NamePattern = new("^[A-Za-z0-9][A-Za-z0-9 _-]*$", RegexOptions.CultureInvariant);
    private static readonly Regex ColorPattern = new("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant);
    private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    internal static ChartTheme Load(string repositoryRoot, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new LocalFlowchartFormatException("Theme file must use a repository-relative path.");

        var root = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var relative = relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(root, relative));
        var rootPrefix = root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootPrefix, PathComparison))
            throw new LocalFlowchartFormatException($"Theme file '{relativePath}' escapes the selected repository.");
        if (HasReparsePointAncestor(path, root))
            throw new LocalFlowchartFormatException($"Theme file '{relativePath}' passes through a symbolic link or reparse point.");

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length == 0) throw Error(relativePath, "file is empty");
            if (stream.Length > MaximumThemeBytes) throw Error(relativePath, $"file exceeds the {MaximumThemeBytes}-byte limit");
            var bytes = new byte[checked((int)stream.Length)];
            stream.ReadExactly(bytes);
            return Parse(bytes, relativePath);
        }
        catch (LocalFlowchartFormatException)
        {
            throw;
        }
        catch (FileNotFoundException)
        {
            throw Error(relativePath, "file was not found");
        }
        catch (DirectoryNotFoundException)
        {
            throw Error(relativePath, "parent directory was not found");
        }
        catch (UnauthorizedAccessException)
        {
            throw Error(relativePath, "file could not be read; check local permissions");
        }
        catch (IOException exception)
        {
            throw Error(relativePath, $"file could not be read: {exception.Message}");
        }
    }

    internal static ChartTheme? Select(string repositoryRoot, string? themeName, string? themeFile)
    {
        if (themeName is not null && themeFile is not null)
            throw new LocalFlowchartFormatException("choose either --theme or --theme-file, not both");
        if (themeFile is not null) return Load(repositoryRoot, themeFile);
        if (themeName is null) return null;
        if (ChartThemeCatalog.TryGet(themeName, out var theme)) return theme;
        throw new LocalFlowchartFormatException(
            $"unknown chart theme '{themeName}'. Choose default, blue-water, high-contrast, or supply --theme-file <repository-relative-json-path>");
    }

    internal static ChartTheme Parse(ReadOnlySpan<byte> utf8Json, string source)
    {
        if (utf8Json.Length == 0) throw Error(source, "file is empty");
        if (utf8Json.Length > MaximumThemeBytes) throw Error(source, $"file exceeds the {MaximumThemeBytes}-byte limit");

        string json;
        try
        {
            json = new UTF8Encoding(false, true).GetString(utf8Json);
        }
        catch (DecoderFallbackException)
        {
            throw Error(source, "file must be valid UTF-8 JSON");
        }
        if (json.Length > 0 && json[0] == '\uFEFF') json = json[1..];

        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            ValidateProperties(root, source, "theme", TopLevelProperties, requireAll: false);

            var version = Required(root, "schemaVersion", source);
            if (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var schemaVersion) || schemaVersion != 1)
                throw Error(source, "schemaVersion must be the integer 1");
            ValidateRequiredProperties(root, source, "theme", TopLevelProperties);

            var nameElement = Required(root, "name", source);
            if (nameElement.ValueKind != JsonValueKind.String)
                throw Error(source, "name must be a string of 1 to 40 letters, digits, spaces, underscores, or hyphens");
            var name = nameElement.GetString()!;
            if (name.Length is < 1 or > 40 || !NamePattern.IsMatch(name))
                throw Error(source, "name must be 1 to 40 letters, digits, spaces, underscores, or hyphens, starting with a letter or digit");

            var canvas = ReadColor(root, "canvas", source);
            var text = ReadColor(root, "text", source);
            var connector = ReadColor(root, "connector", source);
            var edgeLabelBackground = ReadColor(root, "edgeLabelBackground", source);
            var edgeLabelText = ReadColor(root, "edgeLabelText", source);
            var shadow = ReadColor(root, "shadow", source);
            var shapeObject = Required(root, "shapes", source);
            ValidateProperties(shapeObject, source, "shapes", ShapeProperties.Select(property => property.Name).ToArray());
            var shapes = new Dictionary<LocalFlowchartNodeShape, ChartShapeColors>();
            foreach (var (propertyName, shape) in ShapeProperties)
            {
                var shapeElement = Required(shapeObject, propertyName, source);
                ValidateProperties(shapeElement, source, $"shapes.{propertyName}", ["fill", "stroke"]);
                shapes.Add(shape, new ChartShapeColors(
                    ReadColor(shapeElement, "fill", source, $"shapes.{propertyName}.fill"),
                    ReadColor(shapeElement, "stroke", source, $"shapes.{propertyName}.stroke")));
            }

            var theme = new ChartTheme(name, schemaVersion, canvas, text, connector, edgeLabelBackground, edgeLabelText, shadow, shapes);
            foreach (var (propertyName, shape) in ShapeProperties)
            {
                if (theme.Text.ContrastRatio(theme.Shape(shape).Fill) < 4.5)
                    throw Error(source, $"text color must have at least 4.5:1 contrast against shapes.{propertyName}.fill");
            }
            if (theme.EdgeLabelText.ContrastRatio(theme.EdgeLabelBackground) < 4.5)
                throw Error(source, "edgeLabelText must have at least 4.5:1 contrast against edgeLabelBackground");
            return theme;
        }
        catch (JsonException exception)
        {
            throw Error(source, $"invalid JSON: {exception.Message}");
        }
    }

    private static void ValidateProperties(JsonElement element, string source, string objectName, IReadOnlyCollection<string> allowed,
        bool requireAll = true)
    {
        if (element.ValueKind != JsonValueKind.Object) throw Error(source, $"{objectName} must be a JSON object");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!seen.Add(property.Name)) throw Error(source, $"duplicate property '{objectName}.{property.Name}' is not allowed");
            if (!allowed.Contains(property.Name, StringComparer.Ordinal))
                throw Error(source, $"unknown property '{objectName}.{property.Name}' is not supported");
        }
        if (!requireAll) return;
        ValidateRequiredProperties(element, source, objectName, allowed);
    }

    private static void ValidateRequiredProperties(JsonElement element, string source, string objectName,
        IReadOnlyCollection<string> required)
    {
        foreach (var property in required)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out _))
                throw Error(source, $"required property '{objectName}.{property}' is missing");
        }
    }

    private static JsonElement Required(JsonElement parent, string propertyName, string source)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(propertyName, out var value))
            throw Error(source, $"required property '{propertyName}' is missing");
        return value;
    }

    private static ChartColor ReadColor(JsonElement parent, string propertyName, string source, string? role = null)
    {
        var element = Required(parent, propertyName, source);
        var label = role ?? propertyName;
        if (element.ValueKind != JsonValueKind.String)
            throw Error(source, $"color '{label}' must use #RRGGBB hex format");
        var hex = element.GetString()!;
        if (!ColorPattern.IsMatch(hex)) throw Error(source, $"color '{label}' must use #RRGGBB hex format");
        return new ChartColor(Convert.ToByte(hex.Substring(1, 2), 16), Convert.ToByte(hex.Substring(3, 2), 16),
            Convert.ToByte(hex.Substring(5, 2), 16));
    }

    private static bool HasReparsePointAncestor(string path, string root)
    {
        var current = path;
        while (!string.Equals(current, root, PathComparison))
        {
            if (File.Exists(current) || Directory.Exists(current))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return true;
            }
            var parent = Path.GetDirectoryName(current);
            if (parent is null) break;
            current = parent;
        }
        return false;
    }

    private static LocalFlowchartFormatException Error(string source, string message) =>
        new($"{source}: chart theme {message}.");
}
