using System.Text;

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
        var overwrite = false;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
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
        if (format is not ("png" or "drawio")) return Invalid(error, "Unsupported --format. Expected png or drawio.");
        if (!Path.GetExtension(sourcePath).Equals(".md", StringComparison.OrdinalIgnoreCase))
            return Invalid(error, "The flowchart source must be a Markdown (.md) file.");
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

            var source = ResolveInside(root, sourcePath, "Input");
            var destination = ResolveInside(root, outputPath, "Output");
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
                var boundedBuffer = new byte[LocalFlowchartParser.MaximumSourceBytes + 1];
                var total = 0;
                while (total < boundedBuffer.Length)
                {
                    var read = sourceStream.Read(boundedBuffer, total, boundedBuffer.Length - total);
                    if (read == 0) break;
                    total += read;
                }
                if (total > LocalFlowchartParser.MaximumSourceBytes)
                    throw new LocalFlowchartFormatException($"{relativeSource}: source exceeds the {LocalFlowchartParser.MaximumSourceBytes}-byte limit.");
                bytes = boundedBuffer[..total];
            }
            string markdown;
            try
            {
                markdown = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                throw new LocalFlowchartFormatException($"{relativeSource}:1: source is not valid UTF-8.");
            }
            var document = LocalFlowchartParser.Parse(markdown, relativeSource);
            var artifact = format == "png"
                ? LocalFlowchartPngRenderer.Render(document)
                : LocalFlowchartDrawioWriter.Write(document);

            AtomicWrite(root, destination, artifact, overwrite, relativeOutput);
            output.WriteLine($"Created diagram: {relativeOutput}");
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
        output.WriteLine("Usage: brownpants diagram <repository> <relative-markdown-path> --output <relative-path> [--format png|drawio] [--overwrite]");
        output.WriteLine("  Reads one fenced brownpants-flow block. PNG is the default; drawio writes native editable XML.");
        output.WriteLine("  Source and output must remain inside the repository and cannot pass through symbolic links.");
        output.WriteLine("  Rendering is local and deterministic: no network, Mermaid, browser, or external renderer is used.");
        output.WriteLine("  Input limit: 1 MiB; graph limits: 200 nodes and 500 edges; image limits: 4096 pixels per side and 16 million pixels total.");
        output.WriteLine("  Existing output is preserved unless --overwrite is supplied.");
        output.WriteLine("  Example: brownpants diagram . docs/flow.md --output docs/flow.png");
        return 0;
    }

    private static int Invalid(TextWriter error, string message)
    {
        error.WriteLine(message);
        return 2;
    }

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
        if (Directory.Exists(destination))
            throw new IOException($"Output '{relativePath}' is an existing directory.");
        if (File.Exists(destination) && !overwrite)
            throw new IOException($"Output '{relativePath}' already exists. Supply --overwrite to replace it.");

        var directory = Path.GetDirectoryName(destination)!;
        Directory.CreateDirectory(directory);
        if (HasReparsePointAncestor(destination, root))
            throw new ArgumentException($"Output '{relativePath}' passes through a symbolic link or reparse point.");

        var temporary = Path.Combine(directory, $".brownpants-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(content);
                stream.Flush(flushToDisk: true);
            }
            if (HasReparsePointAncestor(destination, root))
                throw new ArgumentException($"Output '{relativePath}' passes through a symbolic link or reparse point.");
            if (overwrite) File.Move(temporary, destination, overwrite: true);
            else File.Move(temporary, destination);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
