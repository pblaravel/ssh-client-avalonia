using System.Collections.ObjectModel;
using System.Text;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SSHClientAvalonia.Models;
using SSHClientAvalonia.Services;

namespace SSHClientAvalonia.ViewModels;

public partial class TabSessionViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly CommandHistoryRepository _historyRepository;
    private readonly SshShellSession _session = new();
    private readonly StringBuilder _outputBuffer = new();
    private const int MaxOutputLength = 512_000;

    public TabSessionViewModel(
        SshConnectionProfile profile,
        CommandHistoryRepository historyRepository,
        Action<TabSessionViewModel>? requestClose)
    {
        Profile = profile;
        _historyRepository = historyRepository;
        RequestClose = requestClose;
        Title = string.IsNullOrWhiteSpace(profile.Name) ? profile.Host : profile.Name;

        _session.OutputReceived += OnOutputReceived;
        _session.ConnectionFailed += OnConnectionFailed;

        RefreshHistoryFromRepository();
        _ = ConnectAsync();
    }

    public SshConnectionProfile Profile { get; }

    public Action<TabSessionViewModel>? RequestClose { get; set; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _terminalOutput = string.Empty;

    [ObservableProperty]
    private string _commandInput = string.Empty;

    [ObservableProperty]
    private string _historyFilter = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Подключение…";

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<CommandHistoryRowViewModel> HistoryItems { get; } = new();

    partial void OnHistoryFilterChanged(string value) => ApplyHistoryFilter();

    private void RefreshHistoryFromRepository()
    {
        HistoryItems.Clear();
        foreach (var entry in _historyRepository.GetHistory(Profile.Id))
            HistoryItems.Add(new CommandHistoryRowViewModel(entry));
    }

    private void ApplyHistoryFilter()
    {
        var filter = HistoryFilter.Trim();
        HistoryItems.Clear();
        foreach (var entry in _historyRepository.GetHistory(Profile.Id))
        {
            if (filter.Length == 0 || entry.Command.Contains(filter, StringComparison.OrdinalIgnoreCase))
                HistoryItems.Add(new CommandHistoryRowViewModel(entry));
        }
    }

    private void OnOutputReceived(string chunk)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_outputBuffer.Length + chunk.Length > MaxOutputLength)
                _outputBuffer.Remove(0, _outputBuffer.Length / 2);

            _outputBuffer.Append(chunk);
            TerminalOutput = _outputBuffer.ToString();
        });
    }

    private void OnConnectionFailed(Exception ex)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = "Ошибка: " + ex.Message;
            IsBusy = false;
        });
    }

    private async Task ConnectAsync()
    {
        IsBusy = true;
        StatusMessage = "Подключение…";
        try
        {
            await _session.ConnectAsync(Profile).ConfigureAwait(false);
            StatusMessage = "Подключено";
        }
        catch (Exception ex)
        {
            StatusMessage = "Не удалось подключиться: " + ex.Message;
        }
        finally
        {
            Dispatcher.UIThread.Post(() => IsBusy = false);
        }
    }

    [RelayCommand]
    private async Task SendCommandAsync()
    {
        var line = CommandInput.ReplaceLineEndings(string.Empty).TrimEnd();
        if (line.Length == 0)
            return;

        CommandInput = string.Empty;

        var entry = new CommandHistoryEntry { Command = line };
        _historyRepository.Add(Profile.Id, entry);
        ApplyHistoryFilter();

        if (!_session.IsConnected)
        {
            StatusMessage = "Нет активного соединения";
            return;
        }

        _session.SendLine(line);
    }

    [RelayCommand]
    private void SendControlC()
    {
        if (_session.IsConnected)
            _session.SendRaw("\u0003");
    }

    [RelayCommand]
    private void RunHistoryCommand(CommandHistoryRowViewModel? row)
    {
        if (row is null || !_session.IsConnected)
            return;

        CommandInput = row.Command;
        _ = SendCommandAsync();
    }

    [RelayCommand]
    private void RemoveHistoryEntry(CommandHistoryRowViewModel? row)
    {
        if (row is null)
            return;

        _historyRepository.Remove(Profile.Id, row.Entry.Id);
        ApplyHistoryFilter();
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _historyRepository.Clear(Profile.Id);
        ApplyHistoryFilter();
    }

    [RelayCommand]
    private async Task ReconnectAsync()
    {
        IsBusy = true;
        StatusMessage = "Переподключение…";
        _outputBuffer.Clear();
        Dispatcher.UIThread.Post(() => TerminalOutput = string.Empty);

        await _session.DisconnectAsync().ConfigureAwait(false);
        await ConnectAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private void CloseTab()
    {
        RequestClose?.Invoke(this);
    }

    public async ValueTask DisposeAsync()
    {
        _session.OutputReceived -= OnOutputReceived;
        _session.ConnectionFailed -= OnConnectionFailed;
        await _session.DisposeAsync().ConfigureAwait(false);
    }
}
