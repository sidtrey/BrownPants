# Evidence JSON Contract

The command emits one JSON object with `schemaVersion: 1`, `root`, `context`, `findings`, `completeness`, and `limitations`. Paths are repository-relative with `/` separators. Lists are sorted ordinally by category, path, line, signal, caller, then target.

Each finding has `id`, `category`, `status`, `signal`, `path`, nullable `line`, nullable `callerSignature`, nullable `targetSignature`, and `evidence`. Managed calls require both signatures and `status: resolved`. All architecture, framework, and test-path findings use `status: candidate`. Framework signal families include explicit MFC, COM, C++/CLI, and NHibernate markers, plus signals located within a project identified as legacy .NET Framework. The command must never infer a native-to-managed call edge.

Completeness reports emitted and total finding counts and whether sampling omitted findings. Limitations state whether semantic analysis ran, whether semantic project coverage is complete, and relevant unresolved-call/workspace issues. The contract excludes benchmark-specific expected IDs and answer keys.
