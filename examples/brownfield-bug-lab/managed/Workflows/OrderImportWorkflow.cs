using LegacyOrderDesk.Domain;
using LegacyOrderDesk.Persistence;

namespace LegacyOrderDesk.Workflows;

public sealed class OrderImportWorkflow<T>
{
    private readonly OrderRepository<T> _repository;

    public OrderImportWorkflow(OrderRepository<T> repository) => _repository = repository;

    public void ImportOne(T item) => _repository.Save(item);

    public void ImportBatch(IEnumerable<T> items) => _repository.Save(items);

    public void ImportMapped<TSource>(IEnumerable<TSource> rows, Func<TSource, T> convert) =>
        _repository.SaveMapped(rows, convert);
}

public sealed class OrderBatchFactory
{
    public OrderImportWorkflow<Order> Create(OrderRepository<Order> repository) =>
        new(repository);

    public static void Register(WorkScope services) =>
        services.AddTransient<OrderImportWorkflow<Order>>(scope =>
            new OrderBatchFactory().Create(scope.Get<OrderRepository<Order>>()));
}

public sealed class WorkScope
{
    private readonly Dictionary<Type, Func<object>> _factories = new();

    public void AddTransient<T>(Func<WorkScope, T> factory) where T : class =>
        _factories[typeof(T)] = () => factory(this);

    public T Get<T>() where T : class => (T)_factories[typeof(T)]();
}

public sealed class AuditJournal
{
    public void Save(IEnumerable<string> entries) { }

    public void WriteExample() => Console.WriteLine("OrderRepository.Save(items) is only a sample string.");
}
