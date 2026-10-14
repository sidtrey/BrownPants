using System.Xml;
using System.Xml.Linq;

namespace BrownPants.Cli.Analysis;

public static class DotNetProjectAnalyzer
{
    private static readonly string[] FrameworkElementNames =
    {
        "TargetFrameworkVersion", "TargetFramework", "TargetFrameworks"
    };

    public static IReadOnlyList<DotNetProjectInfo> Analyze(string root, IEnumerable<string> projectPaths)
    {
        var projects = new List<DotNetProjectInfo>();
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
                var frameworks = Values(document, FrameworkElementNames)
                    .SelectMany(value => value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, RepositoryPathComparer.Instance)
                    .ToArray();
                var languageVersions = Values(document, new[] { "LangVersion" });
                var references = document.Descendants()
                    .Where(element => element.Name.LocalName == "ProjectReference")
                    .Select(element => (string?)element.Attribute("Include"))
                    .Where(include => !string.IsNullOrWhiteSpace(include))
                    .Select(include => include!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(include => include, RepositoryPathComparer.Instance)
                    .ToArray();

                projects.Add(new DotNetProjectInfo(
                    relativePath,
                    frameworks,
                    languageVersions,
                    references,
                    frameworks.Any(IsLegacyFramework),
                    null));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException or ArgumentException)
            {
                projects.Add(new DotNetProjectInfo(relativePath, Array.Empty<string>(), Array.Empty<string>(),
                    Array.Empty<string>(), false, exception.Message));
            }
        }
        return projects;
    }

    private static string[] Values(XDocument document, IReadOnlyCollection<string> elementNames) => document
        .Descendants()
        .Where(element => elementNames.Contains(element.Name.LocalName, StringComparer.Ordinal))
        .Select(element => element.Value.Trim())
        .Where(value => value.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, RepositoryPathComparer.Instance)
        .ToArray();

    private static bool IsLegacyFramework(string framework) =>
        framework.StartsWith("v4.", StringComparison.OrdinalIgnoreCase)
        || framework.StartsWith("4.", StringComparison.OrdinalIgnoreCase)
        || framework.StartsWith("net4", StringComparison.OrdinalIgnoreCase);
}
