using System.Runtime.InteropServices;

namespace NasForWindows.Windows.Hardware;

internal static class WindowsNativeMethods
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetSystemTimes(
        out FileTime idleTime,
        out FileTime kernelTime,
        out FileTime userTime);

    [DllImport("dxgi.dll", EntryPoint = "CreateDXGIFactory1")]
    internal static extern int CreateDxgiFactory1(in Guid interfaceId, out nint factory);

    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryStatusEx
    {
        internal uint Length;
        internal uint MemoryLoad;
        internal ulong TotalPhysical;
        internal ulong AvailablePhysical;
        internal ulong TotalPageFile;
        internal ulong AvailablePageFile;
        internal ulong TotalVirtual;
        internal ulong AvailableVirtual;
        internal ulong AvailableExtendedVirtual;

        internal static MemoryStatusEx Create() => new()
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>(),
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct FileTime
    {
        internal readonly uint LowDateTime;
        internal readonly uint HighDateTime;

        internal ulong ToUInt64() => ((ulong)HighDateTime << 32) | LowDateTime;
    }
}
