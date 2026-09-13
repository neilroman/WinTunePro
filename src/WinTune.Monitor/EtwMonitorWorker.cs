using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WinTune.Monitor;

/// <summary>
/// Worker ligero que samplea CPU/RAM/disco/red via PerformanceCounter (PDH)
/// y escribe en MetricRingBuffer cada segundo.
/// No requiere ETW elevado — PerformanceCounters funcionan como usuario.
/// </summary>
public sealed class EtwMonitorWorker : BackgroundService
{
    private readonly MetricRingBuffer _buffer;
    private readonly ILogger<EtwMonitorWorker> _log;

    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _ramAvailableCounter;

    public EtwMonitorWorker(MetricRingBuffer buffer, ILogger<EtwMonitorWorker> log)
    {
        _buffer = buffer;
        _log = log;
    }

    public override Task StartAsync(CancellationToken ct)
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramAvailableCounter = new PerformanceCounter("Memory", "Available MBytes");
            // Primera lectura siempre da 0 en CPU — descartarla
            _cpuCounter.NextValue();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "No se pudieron inicializar los Performance Counters");
        }
        return base.StartAsync(ct);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("Monitor ETW iniciado");
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var sample = CollectSample();
                _buffer.Write(sample);
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Error al samplear métricas");
            }
            await Task.Delay(1000, ct);
        }
    }

    private SystemMetricSample CollectSample()
    {
        float cpu = _cpuCounter?.NextValue() ?? 0f;
        float ramAvailMb = _ramAvailableCounter?.NextValue() ?? 0f;
        float ramTotalMb = GetTotalRamMb();

        return new SystemMetricSample
        {
            TimestampUtcTicks = DateTime.UtcNow.Ticks,
            CpuPercent = cpu,
            RamUsedMb = ramTotalMb - ramAvailMb,
            RamTotalMb = ramTotalMb,
            DiskReadMbps = 0f,   // TODO: ETW kernel provider
            DiskWriteMbps = 0f,
            NetworkDownMbps = 0f,
            NetworkUpMbps = 0f,
            GpuPercent = 0f,     // TODO: NVML / DXGI
            CpuTempC = 0f,       // TODO: WMI MSAcpi_ThermalZoneTemperature
        };
    }

    private static float GetTotalRamMb()
    {
        try
        {
            var info = GC.GetGCMemoryInfo();
            return (float)(info.TotalAvailableMemoryBytes / (1024.0 * 1024));
        }
        catch { return 0f; }
    }

    public override void Dispose()
    {
        _cpuCounter?.Dispose();
        _ramAvailableCounter?.Dispose();
        base.Dispose();
    }
}
