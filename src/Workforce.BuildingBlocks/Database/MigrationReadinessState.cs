namespace Workforce.BuildingBlocks.Database;

public sealed class MigrationReadinessState
{
    private int _ready;
    private string? _failureReason;

    public bool IsReady => Volatile.Read(ref _ready) == 1;

    public string? FailureReason => _failureReason;

    public void MarkReady()
    {
        _failureReason = null;
        Volatile.Write(ref _ready, 1);
    }

    public void MarkFailed(Exception exception)
    {
        _failureReason = exception.Message;
        Volatile.Write(ref _ready, 0);
    }
}
