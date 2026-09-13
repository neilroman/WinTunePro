using System.Runtime.InteropServices;

namespace WinTune.Interop;

public static class PowerHelper
{
    [DllImport("powrprof.dll", SetLastError = false)]
    private static extern uint PowerGetActiveScheme(nint UserRootPowerKey, out nint ActivePolicyGuid);

    [DllImport("powrprof.dll", SetLastError = false)]
    private static extern uint PowerSetActiveScheme(nint UserRootPowerKey, in Guid SchemeGuid);

    [DllImport("kernel32.dll")]
    private static extern void LocalFree(nint hMem);

    public static Guid? GetActivePowerScheme()
    {
        var result = PowerGetActiveScheme(nint.Zero, out var guidPtr);
        if (result != 0 || guidPtr == nint.Zero) return null;

        try
        {
            return Marshal.PtrToStructure<Guid>(guidPtr);
        }
        finally
        {
            LocalFree(guidPtr);
        }
    }

    public static bool SetActivePowerScheme(Guid schemeGuid)
    {
        var result = PowerSetActiveScheme(nint.Zero, schemeGuid);
        return result == 0;
    }

    public static bool IsOnBattery()
    {
        var status = new SYSTEM_POWER_STATUS();
        return GetSystemPowerStatus(ref status) && status.ACLineStatus == 0;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(ref SYSTEM_POWER_STATUS lpSystemPowerStatus);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }
}
