using NasForWindows.Agent;

namespace NasForWindows.Agent.Tests;

public sealed class AgentAssemblyTests
{
    [Fact]
    public void AssemblyMarkerPointsToAgentAssembly()
    {
        Assert.Equal("NasForWindows.Agent", typeof(AssemblyMarker).Assembly.GetName().Name);
    }
}
