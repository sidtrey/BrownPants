using DelegateImpact.Fixture;

var repository = new WorkRepository();
var workflow = new ImportWorkflow(repository);
workflow.ImportOne(new WorkItem(7));
if (repository.Saved.Count != 1 || repository.Saved[0].Id != 7)
    throw new InvalidOperationException("Imported item was not persisted exactly once.");
Console.WriteLine("Delegate impact fixture checks passed.");