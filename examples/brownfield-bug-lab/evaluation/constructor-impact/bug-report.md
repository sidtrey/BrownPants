# Bug report: order imports are written to the wrong repository

After a workflow is created with the application's repository, single-item import reports success but the supplied repository remains empty. Batch and mapped import paths may be affected too. The factory and DI registration should continue to supply the repository instance used by the workflow.

Acceptance:

- Every import method writes exactly once to the repository passed to `OrderImportWorkflow<T>`.
- Existing factory and DI registration behavior remains intact.
- Identify source-located constructor call sites, related workflow calls, the factory/DI registration route, and the relevant regression check.
- Keep resolved compiler-bound relationships distinct from architecture or runtime claims. Do not infer that MFC, C++/CLI, or NHibernate causes this managed defect.
- Make the smallest repair and run the specified managed check.
