using LegacyOrderDesk.Domain;

namespace LegacyOrderDesk.Persistence;

public sealed class OrderRepository<T>
{
    private readonly List<T> _saved = new();

    public IReadOnlyList<T> Saved => _saved;

    public void Save(T item) => _saved.Add(item);

    public void Save(IEnumerable<T> items)
    {
        foreach (var item in items)
            _saved.Add(item);
    }

    public void SaveMapped<TSource>(IEnumerable<TSource> rows, Func<TSource, T> convert)
    {
        Save(rows.Select(convert));
    }
}
