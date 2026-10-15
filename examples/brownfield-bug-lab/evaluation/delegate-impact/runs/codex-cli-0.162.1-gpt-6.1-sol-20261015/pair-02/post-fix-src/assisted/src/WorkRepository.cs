namespace DelegateImpact.Fixture;

public sealed record WorkItem(int Id);

public sealed class WorkRepository
{
    private readonly List<WorkItem> _saved = [];
    public IReadOnlyList<WorkItem> Saved => _saved;

    public void Save(WorkItem item) { _saved.Add(item); }
}
