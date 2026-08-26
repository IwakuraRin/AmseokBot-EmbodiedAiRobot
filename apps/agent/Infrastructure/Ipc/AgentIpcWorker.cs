using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using NasForWindows.Agent.Features.SystemOverview;
using NasForWindows.Contracts.Agent;

namespace NasForWindows.Agent.Infrastructure.Ipc;

internal sealed partial class AgentIpcWorker(
    HardwareSnapshotStore snapshots,
    ILogger<AgentIpcWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("The Agent named-pipe host requires Windows.");
        }

        var handlers = new List<Task>();
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var pipe = CreatePipe();
                try
                {
                    await pipe.WaitForConnectionAsync(stoppingToken);
                    handlers.RemoveAll(task => task.IsCompleted);
                    handlers.Add(HandleConnectionAsync(pipe, stoppingToken));
                }
                catch
                {
                    await pipe.DisposeAsync();
                    throw;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during service shutdown.
        }
        finally
        {
            await Task.WhenAll(handlers);
        }
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        await using (pipe)
        {
            try
            {
                using var reader = new StreamReader(
                    pipe,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true);
                await using var writer = new StreamWriter(
                    pipe,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    bufferSize: 1024,
                    leaveOpen: true)
                {
                    AutoFlush = true,
                };

                var requestJson = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(requestJson) || requestJson.Length > 4096)
                {
                    await WriteFailureAsync(writer, Guid.Empty, "invalid_request");
                    return;
                }

                var request = JsonSerializer.Deserialize<AgentCommandRequest>(requestJson, JsonOptions);
                if (request is null)
                {
                    await WriteFailureAsync(writer, Guid.Empty, "invalid_request");
                    return;
                }

                switch (request.Command)
                {
                    case AgentCommand.GetHardwareInventory when snapshots.Inventory is { } inventory:
                        await WriteResultAsync(writer, new AgentResult<object>(
                            request.RequestId,
                            true,
                            inventory,
                            null));
                        break;
                    case AgentCommand.GetHardwareMetrics when snapshots.Metrics is { } metrics:
                        await WriteResultAsync(writer, new AgentResult<object>(
                            request.RequestId,
                            true,
                            metrics,
                            null));
                        break;
                    case AgentCommand.GetHardwareInventory or AgentCommand.GetHardwareMetrics:
                        await WriteFailureAsync(writer, request.RequestId, "hardware_snapshot_unavailable");
                        break;
                    default:
                        await WriteFailureAsync(writer, request.RequestId, "unsupported_command");
                        break;
                }
            }
            catch (JsonException exception)
            {
                LogInvalidRequest(logger, exception);
            }
            catch (IOException exception)
            {
                LogConnectionFailed(logger, exception);
            }
        }
    }

    private static NamedPipeServerStream CreatePipe() => new(
        AgentIpcDefaults.PipeName,
        PipeDirection.InOut,
        NamedPipeServerStream.MaxAllowedServerInstances,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

    private static Task WriteFailureAsync(StreamWriter writer, Guid requestId, string errorCode) =>
        WriteResultAsync(writer, new AgentResult<object>(requestId, false, null, errorCode));

    private static Task WriteResultAsync<T>(StreamWriter writer, AgentResult<T> result) =>
        writer.WriteLineAsync(JsonSerializer.Serialize(result, JsonOptions));

    [LoggerMessage(EventId = 110, Level = LogLevel.Warning, Message = "Rejected invalid Agent IPC JSON")]
    private static partial void LogInvalidRequest(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 111, Level = LogLevel.Debug, Message = "Agent IPC client disconnected")]
    private static partial void LogConnectionFailed(ILogger logger, Exception exception);
}
