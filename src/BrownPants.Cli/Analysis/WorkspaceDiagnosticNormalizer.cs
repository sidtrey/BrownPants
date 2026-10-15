using System.Text.RegularExpressions;

namespace BrownPants.Cli.Analysis;

internal static class WorkspaceDiagnosticNormalizer
{
    private const string ExternalPathMarker = "[external-path]";

    private static readonly Regex QuotedAbsolutePathPattern = new(
        @"(?<quote>['""])(?<path>(?:[A-Za-z]:[\\/]|\\\\|/)[^'""]+)(?<close>['""])",
        RegexOptions.CultureInvariant);

    private static readonly Regex UnquotedAbsolutePathPattern = new(
        @"(?<path>(?<![A-Za-z0-9])[A-Za-z]:[\\/](?!/)[^\s'""<>|,;)]*|\\\\[^\\/\s]+[\\/][^\\/\s]+[\\/][^\s'""<>|,;)]*|(?<![A-Za-z0-9_.:/-])/[^\s'""<>|,;)]*)",
        RegexOptions.CultureInvariant);

    internal static string Normalize(string message, string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(message);
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryRoot));

        message = QuotedAbsolutePathPattern.Replace(message, match =>
        {
            var path = NormalizePath(match.Groups["path"].Value, fullRoot);
            return match.Groups["quote"].Value + path + match.Groups["close"].Value;
        });
        message = UnquotedAbsolutePathPattern.Replace(message, match =>
            NormalizePath(match.Groups["path"].Value, fullRoot));

        return message.Replace('\\', '/');
    }

    private static string NormalizePath(string path, string fullRoot)
    {
        if (!Path.IsPathRooted(path))
            return ExternalPathMarker;

        try
        {
            var fullPath = Path.GetFullPath(path);
            var relativePath = Path.GetRelativePath(fullRoot, fullPath);
            if (IsOutsideRepository(relativePath))
                return ExternalPathMarker;

            return relativePath.Replace('\\', '/');
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException)
        {
            return ExternalPathMarker;
        }
    }

    private static bool IsOutsideRepository(string relativePath)
    {
        if (Path.IsPathRooted(relativePath) || relativePath == "..")
            return true;

        var separator = Path.DirectorySeparatorChar;
        var alternateSeparator = Path.AltDirectorySeparatorChar;
        return relativePath.StartsWith(".." + separator, StringComparison.Ordinal)
            || relativePath.StartsWith(".." + alternateSeparator, StringComparison.Ordinal);
    }
}
