namespace NasForWindows.Agent.Features.Operations;

internal sealed partial class OperationWorker(ILogger<OperationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogAgentStarted(logger);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            LogAgentStopping(logger);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "NasForWindows privileged agent started")]
    private static partial void LogAgentStarted(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "NasForWindows privileged agent stopping")]
    private static partial void LogAgentStopping(ILogger logger);
}
