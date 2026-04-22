using CommunityToolkit.Mvvm.ComponentModel;
using SSHClientAvalonia.Models;

namespace SSHClientAvalonia.ViewModels;

public partial class CommandHistoryRowViewModel : ViewModelBase
{
    public CommandHistoryRowViewModel(CommandHistoryEntry entry)
    {
        Entry = entry;
    }

    public CommandHistoryEntry Entry { get; }

    public string Command => Entry.Command;
}
