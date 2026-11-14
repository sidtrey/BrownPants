using System.Text;

namespace BrownPants.Cli.Analysis;

public static class WorkMarkerAnalyzer
{
    private const int MaximumFileBytes = 1024 * 1024;
    private const long MaximumAggregateBytes = 32 * 1024 * 1024;

    private static readonly HashSet<string> SourceExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".vb", ".c", ".cc", ".cpp", ".cxx", ".h", ".hh", ".hpp", ".hxx",
        ".js", ".jsx", ".mjs", ".cjs", ".ts", ".tsx", ".mts", ".cts",
        ".py", ".pyw", ".pyi", ".java", ".kt", ".kts", ".go", ".rs", ".rb", ".php",
        ".swift", ".m", ".mm", ".dart", ".ex", ".exs", ".scala", ".sc", ".fs", ".fsi", ".fsx",
        ".erl", ".hrl", ".groovy", ".gvy", ".sh", ".bash", ".zsh", ".ps1", ".psm1", ".lua",
        ".pl", ".pm", ".hs", ".clj", ".cljs", ".cljc", ".r", ".sql", ".html", ".htm",
        ".css", ".scss", ".sass", ".less"
    };

    private static readonly string[] MarkerKinds = { "TODO", "FIXME", "HACK", "XXX" };

    public static WorkMarkerAnalysis Analyze(string root, IEnumerable<string> sourcePaths, int sampleLimit)
        => Analyze(root, sourcePaths, sampleLimit, MaximumFileBytes, MaximumAggregateBytes);

    internal static WorkMarkerAnalysis Analyze(
        string root,
        IEnumerable<string> sourcePaths,
        int sampleLimit,
        int maximumFileBytes,
        long maximumAggregateBytes)
    {
        var markers = new List<WorkMarker>();
        var markerCount = 0;
        var omittedMarkerCount = 0;
        var sourceFilesScanned = 0;
        var filesSkipped = 0;
        long bytesScanned = 0;

        foreach (var relativePath in sourcePaths
                     .Where(path => SourceExtensions.Contains(Path.GetExtension(path)))
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(path => path, StringComparer.Ordinal))
        {
            var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var length = stream.Length;
                if (length > maximumFileBytes || length > maximumAggregateBytes - bytesScanned)
                {
                    filesSkipped++;
                    continue;
                }

                var content = new byte[(int)length];
                var bytesRead = 0;
                while (bytesRead < content.Length)
                {
                    var read = stream.Read(content, bytesRead, content.Length - bytesRead);
                    if (read == 0)
                        break;
                    bytesRead += read;
                }

                bytesScanned += bytesRead;
                if (bytesRead != content.Length || stream.Length != length)
                {
                    filesSkipped++;
                    continue;
                }

                sourceFilesScanned++;
                using var reader = new StreamReader(new MemoryStream(content, 0, bytesRead), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var sourceText = reader.ReadToEnd();
                using var lineReader = new StringReader(sourceText);
                var lineNumber = 0;
                while (lineReader.ReadLine() is { } line)
                {
                    lineNumber++;
                    foreach (var match in FindMarkers(line))
                    {
                        markerCount++;
                        if (markers.Count < sampleLimit)
                        {
                            markers.Add(new WorkMarker(
                                relativePath.Replace('\\', '/'),
                                lineNumber,
                                match.Index + 1,
                                match.Kind));
                        }
                        else
                        {
                            omittedMarkerCount++;
                        }
                    }
                }
            }
            catch (IOException)
            {
                filesSkipped++;
            }
            catch (UnauthorizedAccessException)
            {
                filesSkipped++;
            }
        }

        return new WorkMarkerAnalysis(
            sourceFilesScanned,
            bytesScanned,
            markerCount,
            omittedMarkerCount,
            filesSkipped,
            markers);
    }

    private static IEnumerable<(int Index, string Kind)> FindMarkers(string line)
    {
        var matches = new List<(int Index, string Kind)>();
        foreach (var kind in MarkerKinds)
        {
            var searchStart = 0;
            while (searchStart < line.Length)
            {
                var index = line.IndexOf(kind, searchStart, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    break;

                var end = index + kind.Length;
                if ((index == 0 || !IsIdentifierCharacter(line[index - 1]))
                    && (end == line.Length || !IsIdentifierCharacter(line[end])))
                {
                    matches.Add((index, kind));
                }

                searchStart = index + kind.Length;
            }
        }

        return matches.OrderBy(match => match.Index).ThenBy(match => match.Kind, StringComparer.Ordinal);
    }

    private static bool IsIdentifierCharacter(char value) => char.IsLetterOrDigit(value) || value == '_';
}
