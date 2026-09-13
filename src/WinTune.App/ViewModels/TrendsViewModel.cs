using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTune.Monitor;

namespace WinTune.App.ViewModels;

public sealed partial class TrendsViewModel : ObservableObject
{
    private readonly MetricHistoryBuffer _buffer;

    [ObservableProperty] private bool _is24Hours = true;
    [ObservableProperty] private double[] _cpuValues  = [];
    [ObservableProperty] private double[] _ramValues  = [];
    [ObservableProperty] private double[] _timestamps = [];
    [ObservableProperty] private bool _hasData;

    public TrendsViewModel(MetricHistoryBuffer buffer)
    {
        _buffer = buffer;
        Refresh();
    }

    partial void OnIs24HoursChanged(bool value) => Refresh();

    [RelayCommand]
    private void Refresh()
    {
        var window = TimeSpan.FromHours(Is24Hours ? 24 : 168);
        var samples = _buffer.GetRange(window);

        CpuValues  = samples.Select(s => (double)s.CpuPercent).ToArray();
        RamValues  = samples.Select(s => (double)s.RamPercent).ToArray();
        Timestamps = samples.Select(s => s.Timestamp.ToOADate()).ToArray();
        HasData    = samples.Count > 0;
    }
}
