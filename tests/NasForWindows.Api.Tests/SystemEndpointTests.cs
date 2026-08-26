using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NasForWindows.Api.Features.System;
using NasForWindows.Contracts.Agent;
using NasForWindows.Contracts.System;
using System.Text.Json;

namespace NasForWindows.Api.Tests;

public sealed class SystemEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void SystemFeatureMapsHardwareInventoryAndMetricsRoutes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSystemFeature();
        var app = builder.Build();

        app.MapSystemEndpoints();

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToArray();

        Assert.Contains("/api/system/status", routes);
        Assert.Contains("/api/system/hardware", routes);
        Assert.Contains("/api/system/metrics", routes);
    }

    [Fact]
    public void AgentObjectEnvelopeDeserializesIntoTypedHardwareContract()
    {
        var requestId = Guid.NewGuid();
        var inventory = new HardwareInventoryResponse(
            DateTimeOffset.UnixEpoch,
            "Windows",
            new CpuDeviceResponse("Test CPU", 4, 8),
            1024,
            [],
            [],
            new MainboardResponse(null, "Test board", null));
        var json = JsonSerializer.Serialize(
            new AgentResult<object>(requestId, true, inventory, null),
            JsonOptions);

        var result = JsonSerializer.Deserialize<AgentResult<HardwareInventoryResponse>>(
            json,
            JsonOptions);

        Assert.NotNull(result);
        Assert.Equal(requestId, result.RequestId);
        Assert.Equal("Test CPU", result.Value?.Cpu.Model);
    }
}
