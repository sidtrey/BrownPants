namespace DelegateImpact.Fixture;

public sealed class ImportWorkflow(WorkRepository repository)
{
    private readonly WorkRepository _repository = repository;

    public void ImportOne(WorkItem item)
    {
        Action<WorkItem> persist = _repository.Save;
        persist(item);
    }
}
