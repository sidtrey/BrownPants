# Bug report: batch order imports silently omit records

When a batch import contains an order marked `RequiresRetry`, that row is missing from the persisted set. The marker means the operation needs another processing attempt; the order itself must still be retained. Single-item saves work. The mapped generic import may share the same persistence path.

Acceptance:
- Every input row passed to the batch repository overload is persisted exactly once, including retry-marked rows.
- Single-item and mapped generic imports keep their existing behavior and preserve all rows.
- Identify the exact overload and resolved managed callers; do not report same-name audit methods as callers.
- List MFC and NHibernate findings as candidate boundary evidence, not resolved C# call edges.
- Do not change MFC or ORM mapping files unless the source evidence proves they cause this defect.
