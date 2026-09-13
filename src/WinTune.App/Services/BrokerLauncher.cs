using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace WinTune.App.Services;

public sealed class BrokerLauncher : IDisposable
{
    private readonly ILogger<BrokerLauncher> _log;
    private Process? _broker;

    public BrokerLauncher(ILogger<BrokerLauncher> log) => _log = log;

    public bool IsRunning =>
        _broker is { HasExited: false };

    public void EnsureRunning()
    {
        if (IsRunning) return;

        var existing = Process.GetProcessesByName("WinTune.Broker");
        if (existing.Length > 0)
        {
            _broker = existing[0];
            foreach (var p in existing.Skip(1)) p.Dispose();
            _log.LogInformation("Broker ya estaba en ejecución (PID {Pid})", _broker.Id);
            return;
        }

        var brokerExe = Path.Combine(AppContext.BaseDirectory, "WinTune.Broker.exe");
        if (!File.Exists(brokerExe))
        {
            _log.LogWarning("WinTune.Broker.exe no encontrado en {Path}", brokerExe);
            return;
        }

        try
        {
            _broker = Process.Start(new ProcessStartInfo
            {
                FileName        = brokerExe,
                UseShellExecute = true,
                Verb            = "runas",
                WindowStyle     = ProcessWindowStyle.Hidden,
            });
            _log.LogInformation("Broker iniciado (PID {Pid})", _broker?.Id);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // ERROR_CANCELLED — user denied the UAC prompt
            _log.LogWarning("El usuario canceló la elevación UAC del Broker");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "No se pudo iniciar el Broker");
        }
    }

    public void Dispose()
    {
        if (_broker is { HasExited: false })
        {
            try { _broker.Kill(); }
            catch (Exception ex) { _log.LogWarning(ex, "Error al terminar el Broker"); }
        }
        _broker?.Dispose();
    }
}
