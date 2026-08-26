using NasForWindows.Contracts.Disks;

namespace NasForWindows.Windows.Storage;

public sealed class WindowsStoragePlatform : IStoragePlatform
{
    public ValueTask<IReadOnlyList<DiskSnapshot>> ListDisksAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows storage APIs are only available on Windows.");
        }

        throw new NotSupportedException("The Windows disk inventory adapter has not been implemented yet.");
    }
}
