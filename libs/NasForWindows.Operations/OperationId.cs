namespace NasForWindows.Operations;

public readonly record struct OperationId(Guid Value)
{
    public static OperationId New() => new(Guid.NewGuid());
}
