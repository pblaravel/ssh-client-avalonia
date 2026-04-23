using CommunityToolkit.Mvvm.ComponentModel;
using SSHClientAvalonia.Models;

namespace SSHClientAvalonia.ViewModels;

public partial class ConnectionItemViewModel : ViewModelBase
{
    public ConnectionItemViewModel(SshConnectionProfile profile)
    {
        Profile = profile;
    }

    public SshConnectionProfile Profile { get; }

    public Guid Id => Profile.Id;

    public string DisplayName => string.IsNullOrWhiteSpace(Profile.Name) ? Profile.Host : Profile.Name;

    public void NotifyDisplayChanged() => OnPropertyChanged(nameof(DisplayName));
}
