namespace LegacyOrderDesk.Domain;

public interface IRetryTracked
{
    bool RequiresRetry { get; }
}

public sealed record Order(int Id, bool RequiresRetry) : IRetryTracked;
public sealed record ImportedRow(int SourceId, bool RequiresRetry) : IRetryTracked;
