using System.Runtime.InteropServices;

namespace WinTune.Monitor;

/// <summary>
/// Lock-free ring buffer para métricas de sistema (CPU, RAM, disco, red).
/// Escrito por WinTune.Monitor, leído por la UI sin bloquear.
/// </summary>
public sealed class MetricRingBuffer
{
    private const int Capacity = 3600; // 1h a 1 muestra/seg
    private readonly SystemMetricSample[] _buffer = new SystemMetricSample[Capacity];
    private volatile int _head;

    public void Write(SystemMetricSample sample)
    {
        var idx = Interlocked.Increment(ref _head) % Capacity;
        _buffer[idx] = sample;
    }

    public SystemMetricSample[] ReadLast(int count)
    {
        int h = _head;
        count = Math.Min(count, Capacity);
        var result = new SystemMetricSample[count];
        for (int i = 0; i < count; i++)
        {
            int idx = (h - i + Capacity) % Capacity;
            result[i] = _buffer[idx];
        }
        return result;
    }

    public SystemMetricSample Latest => _buffer[_head % Capacity];
}

[StructLayout(LayoutKind.Sequential)]
public struct SystemMetricSample
{
    public long TimestampUtcTicks;
    public float CpuPercent;
    public float RamUsedMb;
    public float RamTotalMb;
    public float DiskReadMbps;
    public float DiskWriteMbps;
    public float NetworkDownMbps;
    public float NetworkUpMbps;
    public float GpuPercent;
    public float CpuTempC;
}
