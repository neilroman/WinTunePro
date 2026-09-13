using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScottPlot;
using WinTune.App.ViewModels;

namespace WinTune.App.Views;

public sealed partial class TrendsPage : Page
{
    public TrendsViewModel ViewModel { get; } =
        App.Services.GetRequiredService<TrendsViewModel>();

    public TrendsPage()
    {
        InitializeComponent();
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(TrendsViewModel.CpuValues)
                               or nameof(TrendsViewModel.HasData))
                RefreshCharts();
        };
    }

    private void Page_Loaded(object sender, RoutedEventArgs e) => RefreshCharts();

    private void RefreshCharts()
    {
        if (!ViewModel.HasData) return;

        var ts  = ViewModel.Timestamps;
        var cpu = ViewModel.CpuValues;
        var ram = ViewModel.RamValues;

        CpuPlot.Plot.Clear();
        var cpuLine = CpuPlot.Plot.Add.Scatter(ts, cpu);
        cpuLine.Color = Color.FromHex("#0078d4");
        CpuPlot.Plot.Axes.DateTimeTicksBottom();
        CpuPlot.Plot.Axes.SetLimitsY(0, 100);
        CpuPlot.Refresh();

        RamPlot.Plot.Clear();
        var ramLine = RamPlot.Plot.Add.Scatter(ts, ram);
        ramLine.Color = Color.FromHex("#e81123");
        RamPlot.Plot.Axes.DateTimeTicksBottom();
        RamPlot.Plot.Axes.SetLimitsY(0, 100);
        RamPlot.Refresh();
    }
}
