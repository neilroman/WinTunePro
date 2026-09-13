using Microsoft.UI.Xaml.Controls;

namespace WinTune.App.Services;

public sealed class NavigationService
{
    private Frame? _frame;

    public void SetFrame(Frame frame) => _frame = frame;

    public bool Navigate(Type pageType, object? parameter = null) =>
        _frame?.Navigate(pageType, parameter) ?? false;

    public bool CanGoBack => _frame?.CanGoBack ?? false;
    public void GoBack() => _frame?.GoBack();
}
