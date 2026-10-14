using System.Xml;
using System.Xml.Linq;

namespace BrownPants.Cli.Analysis;

public static class NativeProjectAnalyzer
{
    public static IReadOnlyList<NativeProjectInfo> Analyze(string root, IEnumerable<string> projectPaths)
    {
        var results = new List<NativeProjectInfo>();
        foreach (var relativePath in projectPaths.OrderBy(path => path, RepositoryPathComparer.Instance))
        {
            var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                using var reader = XmlReader.Create(fullPath, new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null
                });
                var document = XDocument.Load(reader);
                var values = (document.Root?.DescendantsAndSelf() ?? Enumerable.Empty<XElement>())
                    .SelectMany(element => element.Attributes().Select(attribute => (Name: attribute.Name.LocalName, Value: attribute.Value))
                        .Append((Name: element.Name.LocalName, Value: element.Value)))
                    .ToArray();
                var mfc = values.Any(item => item.Name.Equals("UseOfMfc", StringComparison.OrdinalIgnoreCase)
                    && IsEnabled(item.Value));
                var clr = values.Any(item => item.Name.Equals("ClrSupport", StringComparison.OrdinalIgnoreCase)
                    && IsEnabled(item.Value))
                    || values.Any(item => item.Name.Equals("ManagedExtensions", StringComparison.OrdinalIgnoreCase)
                        && IsEnabled(item.Value));
                var frameworks = Values(values, "TargetFramework", "TargetFrameworkVersion", "WindowsTargetPlatformVersion");
                var toolsets = Values(values, "PlatformToolset");
                var references = values
                    .Where(item => item.Name.Equals("Include", StringComparison.OrdinalIgnoreCase)
                        || item.Name.Equals("RelativePath", StringComparison.OrdinalIgnoreCase))
                    .Select(item => item.Value.Trim())
                    .Where(value => value.EndsWith(".vcxproj", StringComparison.OrdinalIgnoreCase)
                        || value.EndsWith(".vcproj", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, RepositoryPathComparer.Instance)
                    .ToArray();

                results.Add(new NativeProjectInfo(relativePath, mfc, clr, frameworks, toolsets, references, null));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException or ArgumentException)
            {
                results.Add(new NativeProjectInfo(relativePath, false, false, Array.Empty<string>(),
                    Array.Empty<string>(), Array.Empty<string>(), exception.Message));
            }
        }
        return results;
    }

    private static string[] Values((string Name, string Value)[] values, params string[] names) => values
        .Where(item => names.Contains(item.Name, StringComparer.OrdinalIgnoreCase))
        .Select(item => item.Value.Trim())
        .Where(value => value.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, RepositoryPathComparer.Instance)
        .ToArray();

    private static bool IsEnabled(string value) => value.Trim() is { Length: > 0 } normalized
        && normalized != "0"
        && !normalized.Equals("false", StringComparison.OrdinalIgnoreCase);
}
