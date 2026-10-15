# Instruction Section Retrieval Contract

## Invocation

```text
brownpants instructions <repository> --source <relative-path> --section <anchor> [--format markdown|json]
```

- `--section` selects exactly one heading anchor from the selected source.
- When `--section` is present, exactly one `--source` is required.
- The source must be within the repository, readable UTF-8, within the existing 1 MiB source limit, and not pass through a symbolic link/reparse point.
- Anchors use the rules and duplicate suffixes shown by `brownpants instructions <repository> [--source <relative-path>]`.
- A section request reads only the explicitly selected instruction file; it does not scan or return the other instruction files.
- Exit `0` for a returned section, `1` for a source/section read or lookup failure, and `2` for invalid arguments or unsupported output format.

## Markdown result

Print one source path, heading, anchor, 1-based inclusive line range, and a fenced block containing only the selected section. Include the host-loading caveat. Do not include neighboring sections or fall back to full-file content.

## JSON result

Emit a single object with stable schema version and fields equivalent to:

```json
{
  "SchemaVersion": 1,
  "SourcePath": "AGENTS.md",
  "Heading": "Build and Test",
  "Anchor": "build-and-test",
  "StartLine": 4,
  "EndLine": 6,
  "Content": "## Build and Test\nRun tests.\n"
}
```

Property order and LF-normalized content are stable for the same repository state and options. Diagnostics go to stderr and contain no alternative instruction content.

## Host boundary

The caller must choose and request the section. This operation does not rank relevance, unload other instruction files, or guarantee reduced context or token cost. An SDD engine or host adapter may invoke it if that host supports retrieval tools.
