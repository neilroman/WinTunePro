using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTune.Data;

namespace WinTune.App.ViewModels;

public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly ChangeJournal _journal;

    [ObservableProperty] private IReadOnlyList<JournalEntry> _entries = [];
    [ObservableProperty] private bool _isEmpty = true;

    public HistoryViewModel(ChangeJournal journal)
    {
        _journal = journal;
        Refresh();
    }

    [RelayCommand]
    private void Refresh()
    {
        Entries = _journal.GetRecent(200);
        IsEmpty = Entries.Count == 0;
    }
}
