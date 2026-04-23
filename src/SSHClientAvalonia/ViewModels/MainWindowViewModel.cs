using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SSHClientAvalonia.Models;
using SSHClientAvalonia.Services;

namespace SSHClientAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ConnectionStorageService _storage = new();
    private readonly CommandHistoryRepository _historyRepository = new();
    private Window? _hostWindow;

    public MainWindowViewModel()
    {
        _ = LoadConnectionsAsync();
    }

    public void AttachHost(Window host) => _hostWindow = host;

    public ObservableCollection<ConnectionItemViewModel> Connections { get; } = new();

    public ObservableCollection<TabSessionViewModel> Tabs { get; } = new();

    [ObservableProperty]
    private ConnectionItemViewModel? _selectedConnection;

    [ObservableProperty]
    private TabSessionViewModel? _selectedTab;

    private async Task LoadConnectionsAsync()
    {
        await _historyRepository.LoadFromDiskAsync().ConfigureAwait(false);
        var saved = await _storage.LoadAsync().ConfigureAwait(false);
        var merged = new List<SshConnectionProfile> { DemoConnections.CreateLocalDemo() };
        foreach (var p in saved.Where(p => p.Id != DemoConnections.LocalDemoId))
            merged.Add(p);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Connections.Clear();
            foreach (var p in merged)
                Connections.Add(new ConnectionItemViewModel(p));

            SelectedConnection ??= Connections.FirstOrDefault();
        });
    }

    private async Task SaveConnectionsAsync()
    {
        var profiles = Connections
            .Select(c => c.Profile)
            .Where(p => p.Id != DemoConnections.LocalDemoId)
            .ToList();
        await _storage.SaveAsync(profiles).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task AddConnectionAsync()
    {
        var profile = new SshConnectionProfile();
        var editVm = new ConnectionEditViewModel(profile, isNew: true);
        var dlg = new Views.ConnectionEditWindow { DataContext = editVm };
        var owner = _hostWindow ?? throw new InvalidOperationException("Host window is not set.");
        var accepted = await dlg.ShowDialog<bool>(owner).ConfigureAwait(true);
        if (!accepted)
            return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Connections.Add(new ConnectionItemViewModel(profile));
            SelectedConnection = Connections[^1];
        });
        await SaveConnectionsAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task EditConnectionAsync()
    {
        if (SelectedConnection is null)
            return;

        var editVm = new ConnectionEditViewModel(SelectedConnection.Profile, isNew: false);
        var dlg = new Views.ConnectionEditWindow { DataContext = editVm };
        var owner = _hostWindow ?? throw new InvalidOperationException("Host window is not set.");
        var accepted = await dlg.ShowDialog<bool>(owner).ConfigureAwait(true);
        if (!accepted)
            return;

        SelectedConnection.NotifyDisplayChanged();
        await SaveConnectionsAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task DeleteConnectionAsync()
    {
        if (SelectedConnection is null)
            return;

        var toRemove = SelectedConnection;
        var profileId = toRemove.Profile.Id;
        var tabsToClose = Tabs.Where(t => t.Profile.Id == profileId).ToList();

        foreach (var tab in tabsToClose)
            await CloseTabAsync(tab).ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Connections.Remove(toRemove);
            if (SelectedConnection == toRemove)
                SelectedConnection = Connections.FirstOrDefault();
        });
        await SaveConnectionsAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private void OpenConnection()
    {
        if (SelectedConnection is null)
            return;

        var tab = new TabSessionViewModel(SelectedConnection.Profile, _historyRepository, CloseTabRequested);
        Tabs.Add(tab);
        SelectedTab = tab;
    }

    private void CloseTabRequested(TabSessionViewModel tab)
    {
        _ = CloseTabAsync(tab);
    }

    private async Task CloseTabAsync(TabSessionViewModel tab)
    {
        await tab.DisposeAsync().ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Tabs.Remove(tab);
            if (SelectedTab == tab)
                SelectedTab = Tabs.FirstOrDefault();
        });
    }

}
