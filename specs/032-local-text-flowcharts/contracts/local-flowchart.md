# Contract: Local Flowchart Command

## Command

```text
brownpants diagram <repository> <relative-markdown-path> --output <relative-output-path> [--format png|drawio] [--overwrite]
```

`png` is the default format. The requested extension must match the selected format. Successful execution writes one selected file and reports its repository-relative path. Existing targets are errors unless `--overwrite` is supplied.

## Input grammar

One Markdown fenced block named `brownpants-flow` contains:

```text
flowchart <diagram-id>
node <node-id> <start|process|decision|end> "<printable ASCII label>"
edge <source-id> -> <target-id>
edge <source-id> -> <target-id> label "<printable ASCII branch label>"
```

Blank lines and lines whose first non-space character is `#` are ignored. Declare each node once; declare all nodes before the edges. IDs match `[A-Za-z][A-Za-z0-9_-]{0,39}`. Labels are 1–80 printable ASCII characters (edge labels may be empty only when omitted); double quotes and control characters are unsupported. Each edge pair may occur once.

## Safety and errors

The source and output paths are relative to the explicit repository root. Outside paths, reparse points, missing input, multiple/missing flow blocks, invalid graph references, unsupported syntax, and resource limits return nonzero with source-path/line context when available. A failed operation does not create or alter the target. The command makes no network calls and starts no external process.

Draw.io output is plain uncompressed XML with one `mxfile` page; graph labels are XML-escaped text with HTML interpretation disabled. It contains no scripts, hyperlinks, remote resources, or embedded images.
