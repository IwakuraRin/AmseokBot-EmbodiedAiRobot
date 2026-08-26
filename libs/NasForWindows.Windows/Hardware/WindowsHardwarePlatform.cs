using System.ComponentModel;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using NasForWindows.Contracts.System;

namespace NasForWindows.Windows.Hardware;

[SupportedOSPlatform("windows")]
public sealed class WindowsHardwarePlatform : IHardwarePlatform
{
    private readonly object sync = new();
    private IReadOnlyList<GpuDeviceResponse> gpuDevices = [];
    private CpuTimes? previousCpuTimes;
    private DateTimeOffset? previousSampleTime;

    public ValueTask<HardwareInventoryResponse> ReadInventoryAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureWindows();

        var memory = ReadMemory();
        var gpus = DxgiGpuReader.ReadInventory();
        lock (sync)
        {
            gpuDevices = gpus;
        }

        return ValueTask.FromResult(new HardwareInventoryResponse(
            DateTimeOffset.UtcNow,
            RuntimeInformation.OSDescription,
            ReadCpu(),
            memory.TotalBytes,
            gpus,
            ReadPhysicalDisks(),
            ReadMainboard()));
    }

    public ValueTask<HardwareMetricsResponse> SampleMetricsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureWindows();

        var now = DateTimeOffset.UtcNow;
        var currentCpuTimes = ReadCpuTimes();
        CpuMetricsResponse cpu;
        double intervalSeconds;
        IReadOnlyList<GpuDeviceResponse> currentGpuDevices;

        lock (sync)
        {
            intervalSeconds = previousSampleTime is null ? 0 : (now - previousSampleTime.Value).TotalSeconds;
            cpu = CalculateCpuMetrics(previousCpuTimes, currentCpuTimes);
            previousCpuTimes = currentCpuTimes;
            previousSampleTime = now;
            currentGpuDevices = gpuDevices;
        }

        if (currentGpuDevices.Count == 0)
        {
            currentGpuDevices = DxgiGpuReader.ReadInventory();
            lock (sync)
            {
                gpuDevices = currentGpuDevices;
            }
        }

        return ValueTask.FromResult(new HardwareMetricsResponse(
            now,
            intervalSeconds,
            cpu,
            ReadMemory(),
            DxgiGpuReader.ReadMetrics(currentGpuDevices)));
    }

    private static CpuDeviceResponse ReadCpu()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
        using var results = searcher.Get();
        var processors = results.Cast<ManagementObject>().ToArray();
        var models = processors
            .Select(processor => Text(processor["Name"]))
            .Where(model => model is not null)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new CpuDeviceResponse(
            models.Length == 0 ? "Unknown CPU" : string.Join(" / ", models!),
            processors.Sum(processor => ToInt32(processor["NumberOfCores"])),
            processors.Sum(processor => ToInt32(processor["NumberOfLogicalProcessors"])));
    }

    private static MemoryMetricsResponse ReadMemory()
    {
        var memory = WindowsNativeMethods.MemoryStatusEx.Create();
        if (!WindowsNativeMethods.GlobalMemoryStatusEx(ref memory))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var used = memory.TotalPhysical - memory.AvailablePhysical;
        var utilization = memory.TotalPhysical == 0 ? 0 : used * 100d / memory.TotalPhysical;
        return new MemoryMetricsResponse(
            memory.TotalPhysical,
            used,
            memory.AvailablePhysical,
            Math.Clamp(utilization, 0d, 100d));
    }

    private static MainboardResponse ReadMainboard()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Manufacturer, Product, Version FROM Win32_BaseBoard");
        using var results = searcher.Get();
        var board = results.Cast<ManagementObject>().FirstOrDefault();
        return board is null
            ? new MainboardResponse(null, null, null)
            : new MainboardResponse(
                Text(board["Manufacturer"]),
                Text(board["Product"]),
                Text(board["Version"]));
    }

    private static PhysicalDiskResponse[] ReadPhysicalDisks()
    {
        try
        {
            var scope = new ManagementScope(@"\\.\ROOT\Microsoft\Windows\Storage");
            scope.Connect();
            using var searcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT Number, FriendlyName, SerialNumber, Size, BusType FROM MSFT_Disk"));
            using var results = searcher.Get();
            return results
                .Cast<ManagementObject>()
                .Select(disk => new PhysicalDiskResponse(
                    $"windows-disk-{ToInt32(disk["Number"])}",
                    Text(disk["FriendlyName"]) ?? "Unknown disk",
                    Text(disk["SerialNumber"]),
                    ToUInt64(disk["Size"]),
                    BusTypeName(ToInt32(disk["BusType"]))))
                .ToArray();
        }
        catch (ManagementException)
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, Model, SerialNumber, Size, InterfaceType FROM Win32_DiskDrive");
            using var results = searcher.Get();
            return results
                .Cast<ManagementObject>()
                .Select(disk => new PhysicalDiskResponse(
                    Text(disk["DeviceID"]) ?? "windows-disk-unknown",
                    Text(disk["Model"]) ?? "Unknown disk",
                    Text(disk["SerialNumber"]),
                    ToUInt64(disk["Size"]),
                    Text(disk["InterfaceType"]) ?? "Unknown"))
                .ToArray();
        }
    }

    private static CpuTimes ReadCpuTimes()
    {
        if (!WindowsNativeMethods.GetSystemTimes(out var idle, out var kernel, out var user))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return new CpuTimes(idle.ToUInt64(), kernel.ToUInt64(), user.ToUInt64());
    }

    private static CpuMetricsResponse CalculateCpuMetrics(CpuTimes? previous, CpuTimes current)
    {
        if (previous is null)
        {
            return new CpuMetricsResponse(null, MetricAvailability.TemporarilyUnavailable);
        }

        var idleDelta = current.Idle - previous.Value.Idle;
        var totalDelta = current.Kernel - previous.Value.Kernel + current.User - previous.Value.User;
        if (totalDelta == 0)
        {
            return new CpuMetricsResponse(null, MetricAvailability.TemporarilyUnavailable);
        }

        var utilization = (totalDelta - Math.Min(idleDelta, totalDelta)) * 100d / totalDelta;
        return new CpuMetricsResponse(Math.Clamp(utilization, 0d, 100d), MetricAvailability.Available);
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows hardware APIs are only available on Windows.");
        }
    }

    private static string? Text(object? value)
    {
        var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static int ToInt32(object? value) => value is null
        ? 0
        : Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);

    private static ulong ToUInt64(object? value) => value is null
        ? 0
        : Convert.ToUInt64(value, System.Globalization.CultureInfo.InvariantCulture);

    private static string BusTypeName(int value) => value switch
    {
        3 => "ATA",
        7 => "USB",
        10 => "SAS",
        11 => "SATA",
        14 => "Virtual",
        15 => "FileBackedVirtual",
        16 => "StorageSpaces",
        17 => "NVMe",
        18 => "SCM",
        19 => "UFS",
        _ => "Unknown",
    };

    private readonly record struct CpuTimes(ulong Idle, ulong Kernel, ulong User);
}
