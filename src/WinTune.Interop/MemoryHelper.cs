using System.Runtime.InteropServices;

namespace WinTune.Interop;

public static class MemoryHelper
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    public static (long TotalBytes, long AvailableBytes, int UsagePercent) GetMemoryStatus()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref status))
            return (0, 0, 0);

        return ((long)status.ullTotalPhys, (long)status.ullAvailPhys, (int)status.dwMemoryLoad);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPhysicallyInstalledSystemMemory(out ulong TotalMemoryInKilobytes);

    public static long GetInstalledMemoryBytes()
    {
        if (GetPhysicallyInstalledSystemMemory(out var kb))
            return (long)(kb * 1024);
        return 0;
    }
}
