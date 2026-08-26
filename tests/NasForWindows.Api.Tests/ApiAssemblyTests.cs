using NasForWindows.Api;

namespace NasForWindows.Api.Tests;

public sealed class ApiAssemblyTests
{
    [Fact]
    public void AssemblyMarkerPointsToApiAssembly()
    {
        Assert.Equal("NasForWindows.Api", typeof(AssemblyMarker).Assembly.GetName().Name);
    }
}
