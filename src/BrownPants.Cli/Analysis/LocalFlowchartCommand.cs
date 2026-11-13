using System.Text;
using System.Text.Json;

namespace BrownPants.Cli.Analysis;

internal static class LocalFlowchartCommand
{
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 1 && args[0] is "--help" or "-h") return WriteHelp(output);

        string? repository = null;
        string? sourcePath = null;
        string? outputPath = null;
        var format = "png";
        var formatSupplied = false;
        var inputFormat = "markdown";
        var inputFormatSupplied = false;
        var overwrite = false;
        var direction = LocalFlowchartDirection.TopDown;
        var directionSupplied = false;
        int? tileSize = null;
        var overlap = 64;
        var overlapSupplied = false;
        var profile = "software";
        var profileSupplied = false;
        string? themeName = null;
        string? themeFile = null;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--theme":
                    if (themeName is not null || ++index >= args.Length || args[index].StartsWith("-", StringComparison.Ordinal))
                        return Invalid(error, "--theme requires one name: default, blue-water, or high-contrast.");
                    themeName = args[index];
                    break;
                case "--theme-file":
                    if (themeFile is not null || ++index >= args.Length || args[index].StartsWith("-", StringComparison.Ordinal))
                        return Invalid(error, "--theme-file requires one repository-relative JSON path.");
                    themeFile = args[index];
                    break;
                case "--output":
                    if (outputPath is not null || ++index >= args.Length || args[index].StartsWith("-", StringComparison.Ordinal))
                        return Invalid(error, "--output requires one repository-relative path.");
                    outputPath = args[index];
                    break;
                case "--format":
                    if (formatSupplied || ++index >= args.Length) return Invalid(error, "--format requires one value: png or drawio.");
                    formatSupplied = true;
                    format = args[index].ToLowerInvariant();
                    break;
                case "--input-format":
                    if (inputFormatSupplied || ++index >= args.Length)
                        return Invalid(error, "--input-format requires one value: markdown or drawio.");
                    inputFormatSupplied = true;
                    inputFormat = args[index].ToLowerInvariant();
                    break;
                case "--direction":
                    if (directionSupplied || ++index >= args.Length) return Invalid(error, "--direction requires one value: top-down or left-to-right.");
                    directionSupplied = true;
                    direction = args[index].ToLowerInvariant() switch
                    {
                        "top-down" => LocalFlowchartDirection.TopDown,
                        "left-to-right" => LocalFlowchartDirection.LeftToRight,
                        _ => (LocalFlowchartDirection)(-1)
                    };
                    if ((int)direction < 0) return Invalid(error, "Unsupported --direction. Expected top-down or left-to-right.");
                    break;
                case "--profile":
                    if (profileSupplied || ++index >= args.Length) return Invalid(error, "--profile requires one value: software.");
                    profileSupplied = true;
                    profile = args[index].ToLowerInvariant();
                    if (profile != "software") return Invalid(error, "Unsupported --profile. The supported profile is software.");
                    break;
                case "--tile-size":
                    if (tileSize is not null || ++index >= args.Length || !int.TryParse(args[index], out var parsedTileSize))
                        return Invalid(error, "--tile-size requires one integer from 256 to 4000.");
                    tileSize = parsedTileSize;
                    break;
                case "--overlap":
                    if (overlapSupplied || ++index >= args.Length || !int.TryParse(args[index], out overlap))
                        return Invalid(error, "--overlap requires one non-negative integer smaller than half the tile size.");
                    overlapSupplied = true;
                    break;
                case "--overwrite":
                    if (overwrite) return Invalid(error, "--overwrite may be supplied only once.");
                    overwrite = true;
                    break;
                default:
                    if (args[index].StartsWith("-", StringComparison.Ordinal))
                        return Invalid(error, $"Unknown diagram option '{args[index]}'.");
                    if (repository is null) repository = args[index];
                    else if (sourcePath is null) sourcePath = args[index];
                    else return Invalid(error, "Provide one repository path and one Markdown source path.");
                    break;
            }
        }

        if (repository is null || sourcePath is null)
            return Invalid(error, "A repository path and a Markdown source path are required.");
        if (outputPath is null) return Invalid(error, "An explicit --output repository-relative path is required.");
        if (themeName is not null && themeFile is not null) return Invalid(error, "Choose either --theme or --theme-file, not both.");
        if (format is not ("png" or "drawio")) return Invalid(error, "Unsupported --format. Expected png or drawio.");
        if (inputFormat is not ("markdown" or "drawio")) return Invalid(error, "Unsupported --input-format. Expected markdown or drawio.");
        if (tileSize is not null && format != "png") return Invalid(error, "--tile-size can be used only with PNG output.");
        if (overlapSupplied && tileSize is null) return Invalid(error, "--overlap requires --tile-size.");
        if (tileSize is < 256 or > 4000) return Invalid(error, "--tile-size must be between 256 and 4000.");
        if (tileSize is not null && (overlap < 0 || overlap >= tileSize.Value / 2))
            return Invalid(error, "--overlap must be zero or greater and less than half the tile size.");
        var expectedInputExtension = inputFormat == "markdown" ? ".md" : ".drawio";
        if (!Path.GetExtension(sourcePath).Equals(expectedInputExtension, StringComparison.OrdinalIgnoreCase))
            return Invalid(error, $"The selected {inputFormat} input requires the '{expectedInputExtension}' extension.");
        var expectedExtension = format == "png" ? ".png" : ".drawio";
        if (!Path.GetExtension(outputPath).Equals(expectedExtension, StringComparison.OrdinalIgnoreCase))
            return Invalid(error, $"The selected {format} format requires the '{expectedExtension}' output extension.");

        var relativeSource = sourcePath.Replace('\\', '/');
        var relativeOutput = outputPath.Replace('\\', '/');
        try
        {
            var root = Path.GetFullPath(repository);
            if (!Directory.Exists(root)) throw new ArgumentException("The repository path must name an existing directory.");
            if (HasReparsePointAncestor(root, root))
                throw new ArgumentException("The selected repository root cannot be a symbolic link or reparse point.");
            var theme = ChartThemeParser.Select(root, themeName, themeFile);

            var source = ResolveInside(root, sourcePath, "Input");
            var destination = ResolveInside(root, outputPath, "Output");
            if (inputFormat == "drawio" && string.Equals(source, destination, OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                throw new ArgumentException("Imported diagrams cannot overwrite their source file.");
            if (HasReparsePointAncestor(source, root))
                throw new ArgumentException($"Input '{relativeSource}' passes through a symbolic link or reparse point.");
            if (HasReparsePointAncestor(destination, root))
                throw new ArgumentException($"Output '{relativeOutput}' passes through a symbolic link or reparse point.");
            if (!File.Exists(source)) throw new FileNotFoundException($"Input Markdown file '{relativeSource}' was not found.");

            byte[] bytes;
            using (var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (sourceStream.Length > LocalFlowchartParser.MaximumSourceBytes)
                    throw new LocalFlowchartFormatException($"{relativeSource}: source exceeds the {LocalFlowchartParser.MaximumSourceBytes}-byte limit.");
                var total = 0;
                using var boundedBuffer = new MemoryStream();
                var chunk = new byte[81_920];
                while (true)
                {
                    var read = sourceStream.Read(chunk, 0, chunk.Length);
                    if (read == 0) break;
                    total += read;
                    if (total > LocalFlowchartParser.MaximumSourceBytes)
                        throw new LocalFlowchartFormatException($"{relativeSource}: source exceeds the {LocalFlowchartParser.MaximumSourceBytes}-byte limit.");
                    boundedBuffer.Write(chunk, 0, read);
                }
                bytes = boundedBuffer.ToArray();
            }
            LocalFlowchartDocument document;
            if (inputFormat == "drawio")
            {
                document = LocalFlowchartDrawioParser.Parse(bytes, relativeSource);
            }
            else
            {
                string markdown;
                try
                {
                    markdown = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
                }
                catch (DecoderFallbackException)
                {
                    throw new LocalFlowchartFormatException($"{relativeSource}:1: source is not valid UTF-8.");
                }
                document = LocalFlowchartParser.Parse(markdown, relativeSource);
            }
            var layoutOptions = new LocalFlowchartLayoutOptions(direction);
            var layout = LocalFlowchartLayoutEngine.Create(document, layoutOptions);
            WriteLayoutDiagnostics(layout, output);
            if (RequiresTiling(format, tileSize, layout))
            {
                var selectedTileSize = tileSize ?? 4000;
                var selectedOverlap = tileSize is null ? 64 : overlap;
                var baseName = Path.GetFileNameWithoutExtension(outputPath);
                var directory = Path.GetDirectoryName(outputPath);
                var bundleDirectory = CombineRelative(directory, baseName);
                var manifestRelative = CombineRelative(bundleDirectory, baseName + ".tiles.json");
                var tileCount = WriteTiledBundle(root, outputPath, bundleDirectory, baseName, manifestRelative,
                    layout, document, relativeSource, selectedTileSize, selectedOverlap, overwrite, theme);
                output.WriteLine(tileSize is null
                    ? $"The chart exceeded the single-PNG raster capacity; automatically created {tileCount} PNG tiles with tile-local editable XML sidecars and one complete chart source."
                    : $"Created {tileCount} PNG tiles with tile-local editable XML sidecars and one complete chart source.");
                output.WriteLine($"Tile manifest: {manifestRelative}");
                output.WriteLine($"Complete editable source: {CombineRelative(bundleDirectory, baseName + ".drawio")}");
                output.WriteLine(Describe(document));
                return 0;
            }

                var artifact = format == "png" ? LocalFlowchartPngRenderer.Render(document, layoutOptions, theme) : LocalFlowchartDrawioWriter.Write(document, layoutOptions, theme);
            if (format == "png")
            {
                var sidecarRelative = Path.ChangeExtension(outputPath, ".drawio").Replace('\\', '/');
                var sidecar = ResolveInside(root, sidecarRelative, "XML sidecar");
                if (HasReparsePointAncestor(sidecar, root))
                    throw new ArgumentException($"XML sidecar '{sidecarRelative}' passes through a symbolic link or reparse point.");
                var xml = LocalFlowchartDrawioWriter.Write(document, layoutOptions, theme);
                AtomicWriteBundle(root, [(destination, artifact, relativeOutput), (sidecar, xml, sidecarRelative)], overwrite);
                output.WriteLine($"Created editable source: {sidecarRelative}");
            }
            else
            {
                AtomicWrite(root, destination, artifact, overwrite, relativeOutput);
            }
            output.WriteLine($"Created diagram: {relativeOutput}");
            output.WriteLine(Describe(document));
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException
            or LocalFlowchartFormatException or System.Security.SecurityException or DecoderFallbackException)
        {
            error.WriteLine($"Unable to render diagram: {exception.Message}");
            return 1;
        }
    }

    internal static int WriteHelp(TextWriter output)
    {
        output.WriteLine("Usage: brownpants diagram <repository> <relative-input-path> --output <relative-path> [--input-format markdown|drawio] [--format png|drawio] [--direction top-down|left-to-right] [--profile software] [--theme default|blue-water|high-contrast | --theme-file <relative.json>] [--tile-size 256..4000] [--overlap pixels] [--overwrite]");
        output.WriteLine("  Reads one brownpants-flow, brownpants-org, or brownpants-brainstorm Markdown block, or a supported diagrams.net XML file with --input-format drawio.");
        output.WriteLine("  PNG is the default; drawio writes editable diagrams.net XML. Processing is local and does not access the network.");
        output.WriteLine("  Keep visible labels concise; node/edge labels wrap within reserved bounds. Add `details \"...\"` to save longer explanations in editable XML tooltips.");
        output.WriteLine("  Labels that cannot fit without covering shapes, other labels, or unrelated connectors are rejected before any output is written.");
        output.WriteLine("  Remaining connector crossings are reported when the graph has at most 20,000 edges; larger graphs report that crossing analysis was skipped.");
        output.WriteLine("  --direction selects top-down (default) or left-to-right; --profile software uses a documented software-flow symbol set.");
        output.WriteLine("  --theme selects a built-in palette; --theme-file reads a validated repository-local JSON palette. They are mutually exclusive.");
        output.WriteLine("  Flow charts show processes; org charts show declared reporting lines; brainstorm maps show declared idea branches.");
        output.WriteLine("  Charts use only supplied relationships. Org and brainstorm charts require one rooted tree; ambiguous or unsupported relationships are rejected.");
        output.WriteLine("  Source and output must remain inside the repository and cannot pass through symbolic links.");
        output.WriteLine("  Rendering is local and deterministic: no network, Mermaid, browser, or external renderer is used.");
        output.WriteLine("  Input limit: 128 MiB; graph limits: 1,000,000 nodes and 1,000,000 edges. Single PNGs use up to 90% of the renderer's maximum RGB raster buffer; scanlines are encoded incrementally.");
        output.WriteLine("  PNG output also writes a matching .drawio editable source beside the image.");
        output.WriteLine("  Charts exceeding single-PNG raster capacity are tiled automatically at 4000 pixels; --tile-size overrides the tile size and --overlap defaults to 64 pixels.");
        output.WriteLine("  Tiled PNGs, matching .drawio sidecars, and the manifest are grouped in a subfolder named for the output stem; tile names include canvas pixel origins.");
        output.WriteLine("  Logical dimensions stay 10% below PNG's 2^31-1 pixel-side limit. Explicit tile sizes are 256 to 4000 pixels; each tile stays within 16 million pixels.");
        output.WriteLine("  Existing output files are preserved unless --overwrite is supplied.");
        output.WriteLine("  Example: brownpants diagram . docs/flow.md --output docs/flow.png");
        return 0;
    }

    internal static int WriteGeneratedDocument(string repository, string outputPath, string format,
        LocalFlowchartDocument document, LocalFlowchartDirection direction, int? tileSize, int overlap,
        bool overwrite, string summary, TextWriter output, TextWriter error, ChartTheme? theme = null)
    {
        if (format is not ("png" or "drawio")) return Invalid(error, "Unsupported --format. Expected png or drawio.");
        if (!Path.GetExtension(outputPath).Equals(format == "png" ? ".png" : ".drawio", StringComparison.OrdinalIgnoreCase))
            return Invalid(error, $"The selected {format} format requires the '{(format == "png" ? ".png" : ".drawio")}' output extension.");
        if (tileSize is not null && format != "png") return Invalid(error, "--tile-size can be used only with PNG output.");
        if (tileSize is < 256 or > 4000 || (tileSize is not null && (overlap < 0 || overlap >= tileSize.Value / 2)))
            return Invalid(error, "Tile size must be 256 to 4000 pixels and overlap must be less than half the tile size.");

        var relativeOutput = outputPath.Replace('\\', '/');
        try
        {
            var root = Path.GetFullPath(repository);
            if (!Directory.Exists(root)) throw new ArgumentException("The repository path must name an existing directory.");
            if (HasReparsePointAncestor(root, root))
                throw new ArgumentException("The selected repository root cannot be a symbolic link or reparse point.");
            var destination = ResolveInside(root, outputPath, "Output");
            if (HasReparsePointAncestor(destination, root))
                throw new ArgumentException($"Output '{relativeOutput}' passes through a symbolic link or reparse point.");

            var layoutOptions = new LocalFlowchartLayoutOptions(direction);
            var layout = LocalFlowchartLayoutEngine.Create(document, layoutOptions);
            WriteLayoutDiagnostics(layout, output);
            if (RequiresTiling(format, tileSize, layout))
            {
                var selectedTileSize = tileSize ?? 4000;
                var selectedOverlap = tileSize is null ? 64 : overlap;
                var baseName = Path.GetFileNameWithoutExtension(outputPath);
                var directory = Path.GetDirectoryName(outputPath);
                var bundleDirectory = CombineRelative(directory, baseName);
                var manifestRelative = CombineRelative(bundleDirectory, baseName + ".tiles.json");
                var count = WriteTiledBundle(root, outputPath, bundleDirectory, baseName, manifestRelative,
                    layout, document, document.SourcePath, selectedTileSize, selectedOverlap, overwrite, theme);
                output.WriteLine(tileSize is null
                    ? $"The chart exceeded the single-PNG raster capacity; automatically created {count} PNG tiles with tile-local editable XML sidecars and one complete chart source."
                    : $"Created {count} PNG tiles with tile-local editable XML sidecars and one complete chart source.");
                output.WriteLine($"Tile manifest: {manifestRelative}");
                output.WriteLine($"Complete editable source: {CombineRelative(bundleDirectory, baseName + ".drawio")}");
            }
            else
            {
                var artifact = format == "png"
                    ? LocalFlowchartPngRenderer.Render(document, layoutOptions, theme)
                    : LocalFlowchartDrawioWriter.Write(document, layoutOptions, theme);
                if (format == "png")
                {
                    var sidecarRelative = Path.ChangeExtension(outputPath, ".drawio").Replace('\\', '/');
                    var sidecar = ResolveInside(root, sidecarRelative, "XML sidecar");
                    if (HasReparsePointAncestor(sidecar, root))
                        throw new ArgumentException($"XML sidecar '{sidecarRelative}' passes through a symbolic link or reparse point.");
                    AtomicWriteBundle(root,
                        [(destination, artifact, relativeOutput),
                         (sidecar, LocalFlowchartDrawioWriter.Write(document, layoutOptions, theme), sidecarRelative)], overwrite);
                    output.WriteLine($"Created editable source: {sidecarRelative}");
                }
                else AtomicWrite(root, destination, artifact, overwrite, relativeOutput);
                output.WriteLine($"Created diagram: {relativeOutput}");
            }
            output.WriteLine(summary);
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException
            or LocalFlowchartFormatException or System.Security.SecurityException)
        {
            error.WriteLine($"Unable to render diagram: {exception.Message}");
            return 1;
        }
    }

    private static string Describe(LocalFlowchartDocument document) => document.Kind switch
    {
        LocalFlowchartKind.Organization => $"This chart shows {document.Nodes.Count} people and {document.Edges.Count} reporting links. It makes team ownership and reporting paths easier to review. It reflects relationships written in the source, not a live HR system.",
        LocalFlowchartKind.Brainstorm => $"This chart groups {document.Nodes.Count} ideas into {document.Edges.Count} branches. It helps the team see themes and how suggestions connect. It reflects the supplied notes, not independently verified facts.",
        _ => $"This chart shows {document.Nodes.Count} workflow steps and {document.Edges.Count} connections. It maps the decisions and routes declared in the source so the team can see where work moves or loops back. It reflects the written description, not observed runtime behavior."
    };

    internal static void WriteLayoutDiagnostics(LocalFlowchartLayout layout, TextWriter output)
    {
        if (layout.Diagnostics is null) return;
        foreach (var diagnostic in layout.Diagnostics)
            output.WriteLine($"Layout diagnostic [{diagnostic.Code}]: {diagnostic.Message}");
    }

    private static int Invalid(TextWriter error, string message)
    {
        error.WriteLine(message);
        return 2;
    }

    internal static bool RequiresTiling(string format, int? requestedTileSize, LocalFlowchartLayout layout) =>
        format == "png" && (requestedTileSize is not null
            || !LocalFlowchartPngRenderer.FitsSingleRaster(layout.Width, layout.Height));

    private static string ResolveInside(string root, string relativePath, string description)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new ArgumentException($"{description} path must be repository-relative.");
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        var rootPrefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!fullPath.StartsWith(rootPrefix, comparison))
            throw new ArgumentException($"{description} path must remain inside the selected repository.");
        return fullPath;
    }

    private static bool HasReparsePointAncestor(string fullPath, string root)
    {
        var current = fullPath;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        while (true)
        {
            if ((File.Exists(current) || Directory.Exists(current))
                && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                return true;
            if (string.Equals(current, root, comparison)) return false;
            current = Path.GetDirectoryName(current) ?? root;
        }
    }

    private static void AtomicWrite(string root, string destination, byte[] content, bool overwrite, string relativePath)
    {
        AtomicWriteBundle(root, [(destination, content, relativePath)], overwrite);
    }

    private static string CombineRelative(string? directory, string fileName) =>
        (string.IsNullOrEmpty(directory) ? fileName : Path.Combine(directory, fileName)).Replace('\\', '/');

    private static long WriteTiledBundle(string root, string outputPath, string? relativeDirectory, string baseName,
        string manifestRelative, LocalFlowchartLayout layout, LocalFlowchartDocument document, string sourcePath,
        int tileSize, int overlap, bool overwrite, ChartTheme? theme)
    {
        var expectedArtifacts = EnumerateTileArtifactPaths(layout, relativeDirectory, baseName, tileSize, overlap, manifestRelative).ToArray();
        var completeSourceRelative = CombineRelative(relativeDirectory, baseName + ".drawio");
        var bundleDirectory = ResolveInside(root, relativeDirectory ?? baseName, "Tile bundle directory");
        if (File.Exists(bundleDirectory))
            throw new IOException($"Tile bundle path '{relativeDirectory ?? baseName}' is an existing file.");
        if (HasReparsePointAncestor(bundleDirectory, root))
            throw new ArgumentException($"Tile bundle path '{relativeDirectory ?? baseName}' passes through a symbolic link or reparse point.");

        var expectedNames = expectedArtifacts.Select(artifact => artifact.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var obsoleteArtifacts = ReadPreviousTileArtifacts(root, bundleDirectory, relativeDirectory, baseName,
            manifestRelative, expectedNames);

        foreach (var artifact in expectedArtifacts.Concat(obsoleteArtifacts))
        {
            var destination = ResolveInside(root, artifact.RelativePath, "Tile output");
            if (Directory.Exists(destination)) throw new IOException($"Output '{artifact.RelativePath}' is an existing directory.");
            if (File.Exists(destination) && !overwrite)
                throw new IOException($"Output '{artifact.RelativePath}' already exists. Supply --overwrite to replace it.");
            if (HasReparsePointAncestor(destination, root))
                throw new ArgumentException($"Output '{artifact.RelativePath}' passes through a symbolic link or reparse point.");
        }

        var outputDirectory = Path.GetDirectoryName(ResolveInside(root, outputPath, "Output"))!;
        Directory.CreateDirectory(outputDirectory);
        if (HasReparsePointAncestor(outputDirectory, root))
            throw new ArgumentException("Tile output directory passes through a symbolic link or reparse point.");
        var stageDirectory = Path.Combine(outputDirectory, $".brownpants-tiles-{Guid.NewGuid():N}");
        var backupDirectory = Path.Combine(stageDirectory, ".backups");
        Directory.CreateDirectory(stageDirectory);
        try
        {
            var completeSourceStage = Path.Combine(stageDirectory, Path.GetFileName(completeSourceRelative));
            using (var xmlStream = new FileStream(completeSourceStage, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                       bufferSize: 64 * 1024, FileOptions.SequentialScan))
                LocalFlowchartDrawioWriter.Write(document, layout, xmlStream, theme);
            var manifestName = Path.GetFileName(manifestRelative);
            var manifestPath = Path.Combine(stageDirectory, manifestName);
            long tileCount = 0;
            var spatialIndex = new LocalFlowchartSpatialIndex(layout);
            using (var manifestStream = new FileStream(manifestPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new Utf8JsonWriter(manifestStream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WriteNumber("schemaVersion", 1);
                writer.WriteString("diagramId", document.Id);
                writer.WriteString("sourcePath", sourcePath);
                writer.WriteString("editableSource", completeSourceRelative);
                writer.WriteNumber("canvasWidth", layout.Width);
                writer.WriteNumber("canvasHeight", layout.Height);
                writer.WriteNumber("tileSize", tileSize);
                writer.WriteNumber("requestedOverlap", overlap);
                writer.WriteStartArray("tiles");
                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                foreach (var region in LocalFlowchartPngRenderer.EnumerateTileRegions(layout, tileSize, overlap))
                {
                    tileCount++;
                    var paths = TilePaths(relativeDirectory, baseName, region, tileSize, overlap);
                    var imageStage = Path.Combine(stageDirectory, Path.GetFileName(paths.Image));
                    var xmlStage = Path.Combine(stageDirectory, Path.GetFileName(paths.Xml));
                    File.WriteAllBytes(imageStage, LocalFlowchartPngRenderer.RenderTile(layout, region, spatialIndex, theme).Png);
                    using (var xmlStream = new FileStream(xmlStage, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                               bufferSize: 16 * 1024, FileOptions.SequentialScan))
                        LocalFlowchartDrawioWriter.WriteTile(document, layout, spatialIndex, region, xmlStream, theme);
                    JsonSerializer.Serialize(writer, new LocalFlowchartTileManifestEntry(tileCount,
                        region.Y / (tileSize - overlap) + 1, region.X / (tileSize - overlap) + 1,
                        paths.Image, paths.Xml, region.X, region.Y, region.Width, region.Height,
                        region.X == 0 ? 0 : overlap, region.Y == 0 ? 0 : overlap), jsonOptions);
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.Flush();
                manifestStream.Flush(flushToDisk: true);
            }

            Directory.CreateDirectory(backupDirectory);
            CommitTiledBundle(root, outputDirectory, bundleDirectory, stageDirectory, backupDirectory,
                expectedArtifacts, obsoleteArtifacts, overwrite);
            return tileCount;
        }
        finally
        {
            if (Directory.Exists(stageDirectory)) Directory.Delete(stageDirectory, recursive: true);
        }
    }

    private static IEnumerable<(string FileName, string RelativePath)> EnumerateTileArtifactPaths(
        LocalFlowchartLayout layout, string? relativeDirectory, string baseName, int tileSize, int overlap,
        string manifestRelative)
    {
        foreach (var region in LocalFlowchartPngRenderer.EnumerateTileRegions(layout, tileSize, overlap))
        {
            var paths = TilePaths(relativeDirectory, baseName, region, tileSize, overlap);
            yield return (Path.GetFileName(paths.Image), paths.Image);
            yield return (Path.GetFileName(paths.Xml), paths.Xml);
        }
        yield return (Path.GetFileName(manifestRelative), manifestRelative);
        var completeSourceRelative = CombineRelative(relativeDirectory, baseName + ".drawio");
        yield return (Path.GetFileName(completeSourceRelative), completeSourceRelative);
    }

    private static List<(string FileName, string RelativePath)> ReadPreviousTileArtifacts(string root,
        string bundleDirectory, string? relativeDirectory, string baseName, string manifestRelative,
        IReadOnlySet<string> expectedNames)
    {
        var manifestPath = ResolveInside(root, manifestRelative, "Previous tile manifest");
        if (!File.Exists(manifestPath)) return [];
        if (HasReparsePointAncestor(manifestPath, root))
            throw new ArgumentException($"Previous tile manifest '{manifestRelative}' passes through a symbolic link or reparse point.");

        System.Text.Json.JsonDocument manifest;
        try { manifest = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(manifestPath)); }
        catch (System.Text.Json.JsonException exception)
        {
            throw new IOException($"Existing tile manifest '{manifestRelative}' is invalid; move it before replacing this bundle.", exception);
        }

        using (manifest)
        {
            if (!manifest.RootElement.TryGetProperty("tiles", out var tiles) || tiles.ValueKind != System.Text.Json.JsonValueKind.Array)
                throw new IOException($"Existing tile manifest '{manifestRelative}' has no valid tiles list; move it before replacing this bundle.");
            var expectedBundle = Path.GetFullPath(bundleDirectory);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var seen = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            var tilePattern = new System.Text.RegularExpressions.Regex(
                $"^{System.Text.RegularExpressions.Regex.Escape(baseName)}-x[0-9]{{7}}-y[0-9]{{7}}\\.(?:png|drawio)$",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var obsolete = new List<(string FileName, string RelativePath)>();
            foreach (var tile in tiles.EnumerateArray())
            {
                foreach (var propertyName in new[] { "image", "editableSource" })
                {
                    if (!tile.TryGetProperty(propertyName, out var value) || value.ValueKind != System.Text.Json.JsonValueKind.String)
                        throw new IOException($"Existing tile manifest '{manifestRelative}' contains an invalid tile path; move it before replacing this bundle.");
                    var relative = value.GetString()!.Replace('/', Path.DirectorySeparatorChar);
                    var candidate = ResolveInside(root, relative, "Previous tile output");
                    var fileName = Path.GetFileName(candidate);
                    if (!tilePattern.IsMatch(fileName) || !string.Equals(Path.GetDirectoryName(candidate), expectedBundle, comparison))
                        throw new IOException($"Existing tile manifest '{manifestRelative}' contains a path outside this BrownPants tile bundle; move it before replacing this bundle.");
                    if (expectedNames.Contains(fileName) || !seen.Add(candidate) || !File.Exists(candidate)) continue;
                    if (HasReparsePointAncestor(candidate, root))
                        throw new ArgumentException($"Previous tile '{relative}' passes through a symbolic link or reparse point.");
                    obsolete.Add((fileName, CombineRelative(relativeDirectory, fileName)));
                }
            }
            return obsolete;
        }
    }

    private static (string Image, string Xml) TilePaths(string? relativeDirectory, string baseName,
        LocalFlowchartPngRenderer.TileRegion region, int tileSize, int overlap)
    {
        var stem = $"{baseName}-x{region.X:D7}-y{region.Y:D7}";
        return (CombineRelative(relativeDirectory, stem + ".png"), CombineRelative(relativeDirectory, stem + ".drawio"));
    }

    private static void CommitTiledBundle(string root, string outputDirectory, string bundleDirectory,
        string stageDirectory, string backupDirectory,
        IReadOnlyList<(string FileName, string RelativePath)> expectedArtifacts,
        IReadOnlyList<(string FileName, string RelativePath)> obsoleteArtifacts, bool overwrite)
    {
        var bundleExisted = Directory.Exists(bundleDirectory);
        Directory.CreateDirectory(bundleDirectory);
        var allArtifacts = expectedArtifacts.Concat(obsoleteArtifacts).ToArray();
        try
        {
            foreach (var artifact in allArtifacts)
            {
                var destination = ResolveInside(root, artifact.RelativePath, "Tile output");
                if (HasReparsePointAncestor(destination, root))
                    throw new ArgumentException($"Output '{artifact.RelativePath}' passes through a symbolic link or reparse point.");
                if (!File.Exists(destination)) continue;
                if (!overwrite) throw new IOException($"Output '{artifact.RelativePath}' already exists. Supply --overwrite to replace it.");
                File.Move(destination, Path.Combine(backupDirectory, artifact.FileName));
            }

            foreach (var artifact in expectedArtifacts)
            {
                var destination = ResolveInside(root, artifact.RelativePath, "Tile output");
                if (HasReparsePointAncestor(destination, root))
                    throw new ArgumentException($"Output '{artifact.RelativePath}' passes through a symbolic link or reparse point.");
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Move(Path.Combine(stageDirectory, artifact.FileName), destination);
            }
        }
        catch
        {
            foreach (var artifact in allArtifacts)
            {
                var destination = ResolveInside(root, artifact.RelativePath, "Tile output");
                var backup = Path.Combine(backupDirectory, artifact.FileName);
                var staged = Path.Combine(stageDirectory, artifact.FileName);
                if (File.Exists(backup))
                {
                    if (File.Exists(destination)) File.Delete(destination);
                    File.Move(backup, destination);
                }
                else if (!File.Exists(staged) && File.Exists(destination))
                {
                    File.Delete(destination);
                }
            }
            if (!bundleExisted && Directory.Exists(bundleDirectory) && !Directory.EnumerateFileSystemEntries(bundleDirectory).Any())
                Directory.Delete(bundleDirectory);
            throw;
        }
    }

    private static void AtomicWriteBundle(string root,
        IReadOnlyList<(string Destination, byte[] Content, string RelativePath)> files, bool overwrite)
    {
        foreach (var file in files)
        {
            if (Directory.Exists(file.Destination))
                throw new IOException($"Output '{file.RelativePath}' is an existing directory.");
            if (File.Exists(file.Destination) && !overwrite)
                throw new IOException($"Output '{file.RelativePath}' already exists. Supply --overwrite to replace it.");
            if (HasReparsePointAncestor(file.Destination, root))
                throw new ArgumentException($"Output '{file.RelativePath}' passes through a symbolic link or reparse point.");
            Directory.CreateDirectory(Path.GetDirectoryName(file.Destination)!);
        }

        var staged = new List<(string Temporary, string Destination, string RelativePath)>();
        try
        {
            foreach (var file in files)
            {
                var temporary = Path.Combine(Path.GetDirectoryName(file.Destination)!, $".brownpants-{Guid.NewGuid():N}.tmp");
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(file.Content);
                    stream.Flush(flushToDisk: true);
                }
                staged.Add((temporary, file.Destination, file.RelativePath));
            }

            foreach (var file in staged)
            {
                if (HasReparsePointAncestor(file.Destination, root))
                    throw new ArgumentException($"Output '{file.RelativePath}' passes through a symbolic link or reparse point.");
                if (overwrite) File.Move(file.Temporary, file.Destination, overwrite: true);
                else File.Move(file.Temporary, file.Destination);
            }
        }
        finally
        {
            foreach (var file in staged)
                if (File.Exists(file.Temporary)) File.Delete(file.Temporary);
        }
    }
}
