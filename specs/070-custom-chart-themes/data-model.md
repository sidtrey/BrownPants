# Data Model: Custom Chart Themes

## ChartTheme

An immutable validated palette selected once for one command invocation.

| Field | Type | Rules |
|---|---|---|
| `Name` | string | Built-in identifier or custom `name`; bounded to 40 printable characters |
| `SchemaVersion` | integer | Custom files must specify exactly `1`; built-ins use `1` |
| `Canvas` | RGB color | Required `#RRGGBB` |
| `Text` | RGB color | Required; at least 4.5:1 against every node fill |
| `Connector` | RGB color | Required `#RRGGBB` |
| `EdgeLabelBackground` | RGB color | Required `#RRGGBB` |
| `EdgeLabelText` | RGB color | Required; at least 4.5:1 against edge-label background |
| `Shadow` | RGB color | Required `#RRGGBB` |
| `Shapes` | map of shape role to fill/stroke | Required entries: start, process, decision, input-output, person, cloud, end; all colors `#RRGGBB` |

## Theme selection

Exactly one of the following is supplied: a built-in name (`default`, `blue-water`, `high-contrast`) or a relative custom JSON path. If neither is supplied, `default` is selected. A custom file is UTF-8 JSON, at most 16 KiB, with no external references or additional properties.

## Validation

Reject duplicate or unknown fields, missing roles, invalid color syntax, unsupported schema version, invalid contrast, invalid UTF-8, empty/oversized input, and paths that escape the repository or traverse a reparse point. Validation completes before any output files are staged.
