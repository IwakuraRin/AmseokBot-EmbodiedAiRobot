using NasForWindows.Contracts.Disks;

namespace NasForWindows.Windows.Storage;

public interface IStoragePlatform
{
    ValueTask<IReadOnlyList<DiskSnapshot>> ListDisksAsync(CancellationToken cancellationToken);
}
