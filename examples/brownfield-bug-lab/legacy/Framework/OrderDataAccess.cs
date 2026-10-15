using NHibernate;

namespace LegacyOrderDesk.Framework;

public sealed class OrderDataAccess
{
    private readonly ISessionFactory _sessions;

    public OrderDataAccess(ISessionFactory sessions) => _sessions = sessions;

    public void Save(OrderEntity order)
    {
        using var session = _sessions.OpenSession();
        using var transaction = session.BeginTransaction();
        session.SaveOrUpdate(order);
        transaction.Commit();
    }
}

public class OrderEntity
{
    public virtual int Id { get; protected set; }
    public virtual string CustomerCode { get; set; } = string.Empty;
}
