using CommunityToolkit.Mvvm.ComponentModel;
using SSHClientAvalonia.Models;

namespace SSHClientAvalonia.ViewModels;

public partial class ConnectionEditViewModel : ViewModelBase
{
    public ConnectionEditViewModel(SshConnectionProfile profile, bool isNew)
    {
        IsNew = isNew;
        Name = profile.Name;
        Host = profile.Host;
        Port = profile.Port;
        Username = profile.Username;
        Password = profile.Password ?? string.Empty;
        PrivateKeyPath = profile.PrivateKeyPath ?? string.Empty;
        PrivateKeyPassphrase = profile.PrivateKeyPassphrase ?? string.Empty;
        WorkingProfile = profile;
    }

    public bool IsNew { get; }

    public SshConnectionProfile WorkingProfile { get; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _host = string.Empty;

    [ObservableProperty]
    private int _port = 22;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _privateKeyPath = string.Empty;

    [ObservableProperty]
    private string _privateKeyPassphrase = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public void ApplyToProfile()
    {
        WorkingProfile.Name = Name.Trim();
        WorkingProfile.Host = Host.Trim();
        WorkingProfile.Port = Port is >= 1 and <= 65535 ? Port : 22;
        WorkingProfile.Username = Username.Trim();
        WorkingProfile.Password = string.IsNullOrEmpty(Password) ? null : Password;
        WorkingProfile.PrivateKeyPath = string.IsNullOrWhiteSpace(PrivateKeyPath) ? null : PrivateKeyPath.Trim();
        WorkingProfile.PrivateKeyPassphrase = string.IsNullOrEmpty(PrivateKeyPassphrase)
            ? null
            : PrivateKeyPassphrase;
    }
}
