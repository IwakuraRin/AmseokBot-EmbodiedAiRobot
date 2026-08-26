using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using NasForWindows.Api.Features.System;
using NasForWindows.Contracts.Agent;
using NasForWindows.Contracts.System;

namespace NasForWindows.Api.Infrastructure.AgentIpc;

internal sealed class NamedPipeAgentHardwareClient : IAgentHardwareClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(2);

    public Task<HardwareInventoryResponse> GetInventoryAsync(CancellationToken cancellationToken) =>
        SendAsync<HardwareInventoryResponse>(AgentCommand.GetHardwareInventory, cancellationToken);

    public Task<HardwareMetricsResponse> GetMetricsAsync(CancellationToken cancellationToken) =>
        SendAsync<HardwareMetricsResponse>(AgentCommand.GetHardwareMetrics, cancellationToken);

    private static async Task<T> SendAsync<T>(
        AgentCommand command,
        CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid();
        try
        {
            await using var pipe = new NamedPipeClientStream(
                ".",
                AgentIpcDefaults.PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ConnectTimeout);
            await pipe.ConnectAsync(timeout.Token);

            await using var writer = new StreamWriter(
                pipe,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                bufferSize: 1024,
                leaveOpen: true)
            {
                AutoFlush = true,
            };
            using var reader = new StreamReader(
                pipe,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var request = new AgentCommandRequest(requestId, command);
            await writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions));
            var responseJson = await reader.ReadLineAsync(timeout.Token);
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                throw new AgentHardwareUnavailableException("agent_empty_response");
            }

            var response = JsonSerializer.Deserialize<AgentResult<T>>(responseJson, JsonOptions);
            if (response is null || response.RequestId != requestId)
            {
                throw new AgentHardwareUnavailableException("agent_invalid_response");
            }

            if (!response.Succeeded || response.Value is null)
            {
                throw new AgentHardwareUnavailableException(response.ErrorCode ?? "agent_request_failed");
            }

            return response.Value;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AgentHardwareUnavailableException("agent_timeout", exception);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new AgentHardwareUnavailableException("agent_unavailable", exception);
        }
    }
}
