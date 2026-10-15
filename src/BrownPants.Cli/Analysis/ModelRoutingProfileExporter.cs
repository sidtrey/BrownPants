using System.Text;
using System.Text.Json;

namespace BrownPants.Cli.Analysis;

public sealed record ModelRoutingExportResult(string Host, int Created, int Unchanged, IReadOnlyList<string> Paths);

public static class ModelRoutingProfileExporter
{
    private static readonly UTF8Encoding Utf8 = new(false);

    public static ModelRoutingExportResult Export(string repositoryRoot, ModelRoutingPolicy policy, string host)
    {
        if (host is not ("vscode" or "visual-studio" or "claude" or "codex"))
            throw new ArgumentException($"Unsupported model host '{host}'. Expected vscode, visual-studio, claude, or codex.");

        var root = Path.GetFullPath(repositoryRoot);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Repository directory '{repositoryRoot}' was not found.");
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            throw new ArgumentException("The selected repository root cannot be a symbolic link or reparse point.");

        var outputDirectory = host switch
        {
            "vscode" or "visual-studio" => ".github/agents",
            "claude" => ".claude/agents",
            "codex" => ".codex/agents",
            _ => throw new InvalidOperationException()
        };

        var artifacts = new List<ExportArtifact>();
        foreach (var profile in policy.Profiles.OrderBy(profile => profile.Id, StringComparer.Ordinal))
        {
            if (!profile.Models.TryGetValue(host, out var model) || string.IsNullOrWhiteSpace(model))
                throw new InvalidDataException($"Profile '{profile.Id}' has no model identifier for host '{host}'. No files were written.");
            var extension = host switch { "vscode" or "visual-studio" => ".agent.md", "claude" => ".md", _ => ".toml" };
            var fileName = host == "visual-studio" ? $"brownpants-visual-studio-{profile.Id}" : $"brownpants-{profile.Id}";
            var relativePath = $"{outputDirectory}/{fileName}{extension}";
            var destination = ModelRoutingPolicyReader.ResolveInside(root, relativePath, "Export");
            ModelRoutingPolicyReader.RejectReparseAncestors(root, destination, "Export");
            if (Directory.Exists(destination)) throw new IOException($"Export destination '{relativePath}' is a directory. No files were written.");

            var content = RenderProfile(host, profile, model);
            var bytes = Utf8.GetBytes(content);
            var unchanged = false;
            if (File.Exists(destination))
            {
                var existing = File.ReadAllBytes(destination);
                if (!existing.AsSpan().SequenceEqual(bytes))
                    throw new IOException($"Export destination '{relativePath}' already exists with different content. It was preserved; no files were written.");
                unchanged = true;
            }
            artifacts.Add(new ExportArtifact(relativePath, destination, bytes, unchanged));
        }

        var staged = new List<(ExportArtifact Artifact, string Temporary)>();
        var created = new List<string>();
        try
        {
            foreach (var artifact in artifacts.Where(artifact => !artifact.Unchanged))
            {
                var directory = Path.GetDirectoryName(artifact.Destination)!;
                Directory.CreateDirectory(directory);
                ModelRoutingPolicyReader.RejectReparseAncestors(root, artifact.Destination, "Export");
                var temporary = Path.Combine(directory, $".brownpants-{Guid.NewGuid():N}.tmp");
                staged.Add((artifact, temporary));
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(artifact.Bytes);
                    stream.Flush(flushToDisk: true);
                }
            }

            foreach (var entry in staged)
            {
                ModelRoutingPolicyReader.RejectReparseAncestors(root, entry.Artifact.Destination, "Export");
                File.Move(entry.Temporary, entry.Artifact.Destination);
                created.Add(entry.Artifact.Destination);
            }
        }
        catch
        {
            foreach (var path in created)
            {
                try { File.Delete(path); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            throw;
        }
        finally
        {
            foreach (var entry in staged)
            {
                try { if (File.Exists(entry.Temporary)) File.Delete(entry.Temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        return new ModelRoutingExportResult(host, created.Count, artifacts.Count(artifact => artifact.Unchanged),
            artifacts.Select(artifact => artifact.RelativePath).ToArray());
    }

    private static string RenderProfile(string host, ModelRoutingProfile profile, string model)
    {
        var description = $"BrownPants {profile.QualityTier} profile for {string.Join(", ", profile.Categories.OrderBy(value => value, StringComparer.Ordinal))}. {profile.Rationale}";
        var body = $"# BrownPants task profile: {profile.Id}\n\n"
            + $"Categories: {string.Join(", ", profile.Categories.OrderBy(value => value, StringComparer.Ordinal))}\n\n"
            + $"Quality tier: {profile.QualityTier}. This is a team label, not a quality or cost guarantee.\n\n"
            + $"## Team instructions\n\n{profile.Instructions}\n";

        return host switch
        {
            "vscode" => "---\n"
                + $"name: {YamlString($"BrownPants {profile.Id}")}\n"
                + $"description: {YamlString(description)}\n"
                + $"model: {YamlString(model)}\n"
                + "---\n\n"
                + body,
            "visual-studio" => "---\n"
                + $"name: {YamlString($"BrownPants Visual Studio {profile.Id}")}\n"
                + $"description: {YamlString($"Visual Studio task profile. {description}")}\n"
                + $"model: {YamlString(model)}\n"
                + "---\n\n"
                + body,
            "claude" => "---\n"
                + $"name: {YamlString($"brownpants-{profile.Id}")}\n"
                + $"description: {YamlString(description)}\n"
                + $"model: {YamlString(model)}\n"
                + "---\n\n"
                + body,
            "codex" => $"name = {TomlString($"brownpants-{profile.Id}")}\n"
                + $"description = {TomlString(description)}\n"
                + $"model = {TomlString(model)}\n"
                + $"developer_instructions = {TomlString(body)}\n",
            _ => throw new ArgumentOutOfRangeException(nameof(host))
        };
    }

    private static string YamlString(string value) => JsonSerializer.Serialize(value);

    private static string TomlString(string value)
    {
        var output = new StringBuilder(value.Length + 2).Append('"');
        foreach (var character in value)
        {
            output.Append(character switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\b' => "\\b",
                '\t' => "\\t",
                '\n' => "\\n",
                '\f' => "\\f",
                '\r' => "\\r",
                _ when char.IsControl(character) => $"\\u{(int)character:X4}",
                _ => character.ToString()
            });
        }
        return output.Append('"').ToString();
    }

    private sealed record ExportArtifact(string RelativePath, string Destination, byte[] Bytes, bool Unchanged);
}
