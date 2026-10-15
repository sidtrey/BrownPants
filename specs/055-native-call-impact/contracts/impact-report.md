# Impact Report Contract

`brownpants impact <repository> --changed <relative-source-path> [--changed <relative-source-path> ...] [--format markdown|json] [--limit 1..10000]` accepts discovered managed or native source files.

- Markdown presents managed semantic relationships separately from native lexical candidates.
- JSON retains current fields and adds explicit native candidate data without changing the existing schema version unless compatibility review requires it.
- Each native candidate includes relation kind/status, source and target identities/locations, candidate count, and an explicit evidence classification.
- Invalid, missing, outside-root, and unsupported changed paths return a clear error.
- Incomplete graphs include reasons and never imply that no reported caller means no runtime impact.
