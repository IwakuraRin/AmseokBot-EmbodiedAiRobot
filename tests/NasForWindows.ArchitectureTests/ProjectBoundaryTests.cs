using System.Xml.Linq;

namespace NasForWindows.ArchitectureTests;

public sealed class ProjectBoundaryTests
{
    [Fact]
    public void ApiDoesNotReferencePrivilegedAgentOrWindowsAdapter()
    {
        var references = GetProjectReferences("apps/api/NasForWindows.Api.csproj");

        Assert.DoesNotContain("apps/agent/NasForWindows.Agent.csproj", references);
        Assert.DoesNotContain("libs/NasForWindows.Windows/NasForWindows.Windows.csproj", references);
    }

    [Fact]
    public void AgentDoesNotReferenceApiOrPluginSdk()
    {
        var references = GetProjectReferences("apps/agent/NasForWindows.Agent.csproj");

        Assert.DoesNotContain("apps/api/NasForWindows.Api.csproj", references);
        Assert.DoesNotContain("libs/NasForWindows.PluginSdk/NasForWindows.PluginSdk.csproj", references);
    }

    [Theory]
    [InlineData("apps/agent/NasForWindows.Agent.csproj")]
    [InlineData("apps/manager/NasForWindows.Manager.csproj")]
    public void NonApiHostsDoNotOwnWebIdentityPersistence(string project)
    {
        var packages = GetPackageReferences(project);

        Assert.DoesNotContain(
            packages,
            package => package.StartsWith("Microsoft.AspNetCore.Identity", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            packages,
            package => package.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ApiCallersUseTheWebAccessRootEntryPoint()
    {
        var repositoryRoot = FindRepositoryRoot();
        var apiRoot = Path.Combine(repositoryRoot.FullName, "apps", "api");
        var webAccessRoot = Path.Combine(apiRoot, "Features", "WebAccess");
        var deepImports = Directory
            .EnumerateFiles(apiRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.StartsWith(webAccessRoot, StringComparison.Ordinal))
            .Where(path => !path.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains(
                "using NasForWindows.Api.Features.WebAccess.",
                StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(repositoryRoot.FullName, path).Replace('\\', '/'))
            .ToArray();

        Assert.Empty(deepImports);
    }

    [Fact]
    public void ManagerDoesNotReferenceBackendHostsOrWindowsAdapter()
    {
        var references = GetProjectReferences("apps/manager/NasForWindows.Manager.csproj");

        Assert.DoesNotContain("apps/api/NasForWindows.Api.csproj", references);
        Assert.DoesNotContain("apps/agent/NasForWindows.Agent.csproj", references);
        Assert.DoesNotContain("libs/NasForWindows.Windows/NasForWindows.Windows.csproj", references);
    }

    [Fact]
    public void SpectreConsoleIsConfinedToManagerHost()
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectsUsingSpectreConsole = Directory
            .EnumerateFiles(repositoryRoot.FullName, "*.csproj", SearchOption.AllDirectories)
            .Where(project => !project.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .Where(project => XDocument
                .Load(project)
                .Descendants("PackageReference")
                .Any(element => string.Equals(
                    element.Attribute("Include")?.Value,
                    "Spectre.Console",
                    StringComparison.OrdinalIgnoreCase)))
            .Select(project => Path.GetRelativePath(repositoryRoot.FullName, project).Replace('\\', '/'))
            .ToArray();

        Assert.Equal(["apps/manager/NasForWindows.Manager.csproj"], projectsUsingSpectreConsole);
    }

    [Fact]
    public void SystemManagementIsConfinedToWindowsAdapter()
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectsUsingSystemManagement = Directory
            .EnumerateFiles(repositoryRoot.FullName, "*.csproj", SearchOption.AllDirectories)
            .Where(project => !project.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .Where(project => XDocument
                .Load(project)
                .Descendants("PackageReference")
                .Any(element => string.Equals(
                    element.Attribute("Include")?.Value,
                    "System.Management",
                    StringComparison.OrdinalIgnoreCase)))
            .Select(project => Path.GetRelativePath(repositoryRoot.FullName, project).Replace('\\', '/'))
            .ToArray();

        Assert.Equal(["libs/NasForWindows.Windows/NasForWindows.Windows.csproj"], projectsUsingSystemManagement);
    }

    [Theory]
    [InlineData("libs/NasForWindows.Contracts/NasForWindows.Contracts.csproj")]
    [InlineData("libs/NasForWindows.Operations/NasForWindows.Operations.csproj")]
    [InlineData("libs/NasForWindows.PluginSdk/NasForWindows.PluginSdk.csproj")]
    [InlineData("libs/NasForWindows.Windows/NasForWindows.Windows.csproj")]
    public void LibrariesDoNotReferenceApplicationHosts(string project)
    {
        var references = GetProjectReferences(project);

        Assert.DoesNotContain(references, reference => reference.StartsWith("apps/", StringComparison.Ordinal));
    }

    private static string[] GetProjectReferences(string project)
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectPath = Path.Combine(repositoryRoot.FullName, project);
        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        var document = XDocument.Load(projectPath);

        return document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!.Replace('\\', Path.DirectorySeparatorChar))
            .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, include)))
            .Select(path => Path.GetRelativePath(repositoryRoot.FullName, path).Replace('\\', '/'))
            .ToArray();
    }

    private static string[] GetPackageReferences(string project)
    {
        var repositoryRoot = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(repositoryRoot.FullName, project));

        return document
            .Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!)
            .ToArray();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NasForWindows.slnx")))
            {
                return directory;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the NasForWindows repository root.");
    }
}
