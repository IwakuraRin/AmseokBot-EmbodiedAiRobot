using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Management;
using NasForWindows.Contracts.System;

namespace NasForWindows.Windows.Hardware;

[SupportedOSPlatform("windows")]
internal static partial class DxgiGpuReader
{
    private const int DxgiErrorNotFound = unchecked((int)0x887A0002);
    private static readonly Guid FactoryInterfaceId = new("770AAE78-F26F-4DBA-A829-253C83D1B387");

    internal static IReadOnlyList<GpuDeviceResponse> ReadInventory()
    {
        var devices = ReadDxgiInventory();
        return devices.Count > 0 ? devices : ReadWmiFallbackInventory();
    }

    internal static IReadOnlyList<GpuMetricsResponse> ReadMetrics(IReadOnlyList<GpuDeviceResponse> devices)
    {
        var engineUsage = ReadEngineUsage();
        var memoryUsage = ReadMemoryUsage();

        return devices
            .Select(device =>
            {
                var hasEngine = engineUsage.TryGetValue(device.Id, out var utilization);
                var hasMemory = memoryUsage.TryGetValue(device.Id, out var usedMemory);
                return new GpuMetricsResponse(
                    device.Id,
                    hasEngine ? Math.Clamp(utilization, 0d, 100d) : null,
                    hasMemory ? usedMemory : null,
                    hasEngine ? MetricAvailability.Available : MetricAvailability.TemporarilyUnavailable,
                    hasMemory ? MetricAvailability.Available : MetricAvailability.TemporarilyUnavailable);
            })
            .ToArray();
    }

    private static List<GpuDeviceResponse> ReadDxgiInventory()
    {
        if (WindowsNativeMethods.CreateDxgiFactory1(FactoryInterfaceId, out var factory) < 0)
        {
            return [];
        }

        var devices = new List<GpuDeviceResponse>();
        try
        {
            var enumAdapters = GetDelegate<EnumAdapters1>(factory, 12);
            for (uint index = 0; ; index++)
            {
                var result = enumAdapters(factory, index, out var adapter);
                if (result == DxgiErrorNotFound)
                {
                    break;
                }

                if (result < 0 || adapter == 0)
                {
                    continue;
                }

                try
                {
                    var getDescription = GetDelegate<GetDescription1>(adapter, 10);
                    if (getDescription(adapter, out var description) < 0)
                    {
                        continue;
                    }

                    var dedicatedMemory = description.DedicatedVideoMemory.ToUInt64();
                    devices.Add(new GpuDeviceResponse(
                        FormatLuid(description.AdapterLuid),
                        description.Description.TrimEnd('\0', ' '),
                        VendorName(description.VendorId),
                        dedicatedMemory > 0 ? GpuMemoryKind.Dedicated : GpuMemoryKind.Shared,
                        dedicatedMemory > 0 ? dedicatedMemory : null));
                }
                finally
                {
                    Release(adapter);
                }
            }
        }
        finally
        {
            Release(factory);
        }

        return devices;
    }

    private static GpuDeviceResponse[] ReadWmiFallbackInventory()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT DeviceID, Name, AdapterCompatibility, AdapterRAM FROM Win32_VideoController");
        using var results = searcher.Get();
        return results
            .Cast<ManagementObject>()
            .Select((device, index) => new GpuDeviceResponse(
                Convert.ToString(device["DeviceID"], System.Globalization.CultureInfo.InvariantCulture)
                    ?? $"windows-gpu-{index}",
                Convert.ToString(device["Name"], System.Globalization.CultureInfo.InvariantCulture)
                    ?? "Windows GPU",
                Convert.ToString(device["AdapterCompatibility"], System.Globalization.CultureInfo.InvariantCulture)
                    ?? "Unknown",
                ToUInt64(device["AdapterRAM"]) > 0 ? GpuMemoryKind.Dedicated : GpuMemoryKind.Shared,
                ToUInt64(device["AdapterRAM"]) is var memory && memory > 0 ? memory : null))
            .ToArray();
    }

    private static Dictionary<string, double> ReadEngineUsage()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, UtilizationPercentage " +
                "FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine");
            using var results = searcher.Get();
            return results
                .Cast<ManagementObject>()
                .Select(item => (
                    Id: TryParseLuid(Convert.ToString(item["Name"], System.Globalization.CultureInfo.InvariantCulture)),
                    Value: ToDouble(item["UtilizationPercentage"])))
                .Where(item => item.Id is not null)
                .GroupBy(item => item.Id!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Max(item => item.Value),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (ManagementException)
        {
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static Dictionary<string, ulong> ReadMemoryUsage()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, DedicatedUsage " +
                "FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUAdapterMemory");
            using var results = searcher.Get();
            return results
                .Cast<ManagementObject>()
                .Select(item => (
                    Id: TryParseLuid(Convert.ToString(item["Name"], System.Globalization.CultureInfo.InvariantCulture)),
                    Value: ToUInt64(item["DedicatedUsage"])))
                .Where(item => item.Id is not null)
                .GroupBy(item => item.Id!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Max(item => item.Value),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (ManagementException)
        {
            return new Dictionary<string, ulong>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static T GetDelegate<T>(nint instance, int methodIndex)
        where T : Delegate
    {
        var vtable = Marshal.ReadIntPtr(instance);
        var address = Marshal.ReadIntPtr(vtable, methodIndex * IntPtr.Size);
        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    private static void Release(nint instance)
    {
        if (instance != 0)
        {
            GetDelegate<ReleaseComObject>(instance, 2)(instance);
        }
    }

    private static string FormatLuid(AdapterLuid luid) =>
        $"windows-luid-{unchecked((uint)luid.HighPart):x8}-{luid.LowPart:x8}";

    private static string? TryParseLuid(string? value)
    {
        var match = value is null ? Match.Empty : LuidPattern().Match(value);
        if (!match.Success
            || !uint.TryParse(match.Groups["high"].Value, System.Globalization.NumberStyles.HexNumber, null, out var high)
            || !uint.TryParse(match.Groups["low"].Value, System.Globalization.NumberStyles.HexNumber, null, out var low))
        {
            return null;
        }

        return $"windows-luid-{high:x8}-{low:x8}";
    }

    private static string VendorName(uint vendorId) => vendorId switch
    {
        0x10DE => "NVIDIA",
        0x1002 or 0x1022 => "AMD",
        0x8086 => "Intel",
        0x1414 => "Microsoft",
        _ => $"PCI {vendorId:X4}",
    };

    private static ulong ToUInt64(object? value)
    {
        try
        {
            return value is null
                ? 0
                : Convert.ToUInt64(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (OverflowException)
        {
            return 0;
        }
    }

    private static double ToDouble(object? value) => value is null
        ? 0
        : Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);

    [GeneratedRegex("luid_0x(?<high>[0-9a-f]+)_0x(?<low>[0-9a-f]+)", RegexOptions.IgnoreCase)]
    private static partial Regex LuidPattern();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int EnumAdapters1(nint factory, uint adapterIndex, out nint adapter);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetDescription1(nint adapter, out AdapterDescription1 description);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint ReleaseComObject(nint instance);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct AdapterDescription1
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        internal string Description;
        internal uint VendorId;
        internal uint DeviceId;
        internal uint SubSystemId;
        internal uint Revision;
        internal UIntPtr DedicatedVideoMemory;
        internal UIntPtr DedicatedSystemMemory;
        internal UIntPtr SharedSystemMemory;
        internal AdapterLuid AdapterLuid;
        internal uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct AdapterLuid
    {
        internal readonly uint LowPart;
        internal readonly int HighPart;
    }
}
