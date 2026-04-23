using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SSHClientAvalonia.ViewModels;

namespace SSHClientAvalonia.Views;

public partial class TabSessionView : UserControl
{
    private TabSessionViewModel? _vm;
    private bool _terminalSync;
    /// <summary>Префикс текста, который пришёл только с сервера (последняя синхронизация).</summary>
    private string _serverTextSnapshot = string.Empty;

    public TabSessionView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) =>
        {
            TerminalBox.PointerReleased += (_, _) =>
                Dispatcher.UIThread.Post(ClampCaretToEditableRegion, DispatcherPriority.Input);
        };
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm.ReplayCommandRequested -= OnReplayCommandRequested;
        }

        _vm = DataContext as TabSessionViewModel;
        if (_vm is not null)
        {
            _vm.PropertyChanged += OnVmPropertyChanged;
            _vm.ReplayCommandRequested += OnReplayCommandRequested;
            ApplyServerTextToTerminal(_vm.ServerOutput, preserveUserSuffix: false);
        }
    }

    private void OnReplayCommandRequested(string command)
    {
        if (_vm is null)
            return;

        _terminalSync = true;
        try
        {
            var prefix = _vm.ServerOutput;
            _serverTextSnapshot = prefix;
            var text = prefix + command;
            TerminalBox.Text = text;
            TerminalBox.CaretIndex = text.Length;
            TerminalBox.Focus();
        }
        finally
        {
            _terminalSync = false;
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TabSessionViewModel.ServerOutput))
        {
            ApplyServerTextToTerminal(_vm!.ServerOutput, preserveUserSuffix: true);
            ScrollTerminalToEnd();
        }
    }

    private void ApplyServerTextToTerminal(string newServerText, bool preserveUserSuffix)
    {
        if (_terminalSync)
            return;

        _terminalSync = true;
        try
        {
            var cur = TerminalBox.Text ?? string.Empty;
            string suffix = string.Empty;
            if (preserveUserSuffix && cur.StartsWith(_serverTextSnapshot, StringComparison.Ordinal))
                suffix = cur[_serverTextSnapshot.Length..];

            _serverTextSnapshot = newServerText;
            var combined = newServerText + suffix;
            if (TerminalBox.Text != combined)
            {
                var caret = Math.Clamp(TerminalBox.CaretIndex, newServerText.Length, combined.Length);
                TerminalBox.Text = combined;
                TerminalBox.CaretIndex = caret;
            }
        }
        finally
        {
            _terminalSync = false;
        }
    }

    private void OnTerminalTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_terminalSync || _vm is null)
            return;

        var prefix = _vm.ServerOutput;
        var t = TerminalBox.Text ?? string.Empty;
        if (t.Length >= prefix.Length && t.StartsWith(prefix, StringComparison.Ordinal))
            return;

        _terminalSync = true;
        try
        {
            TerminalBox.Text = prefix;
            TerminalBox.CaretIndex = prefix.Length;
        }
        finally
        {
            _terminalSync = false;
        }
    }

    private void OnTerminalGotFocus(object? sender, RoutedEventArgs e) => ClampCaretToEditableRegion();

    private void ClampCaretToEditableRegion()
    {
        if (_terminalSync || _vm is null)
            return;

        var prefix = _vm.ServerOutput;
        if (TerminalBox.CaretIndex < prefix.Length)
        {
            _terminalSync = true;
            try
            {
                TerminalBox.CaretIndex = prefix.Length;
            }
            finally
            {
                _terminalSync = false;
            }
        }
    }

    private void OnTerminalKeyDown(object? sender, KeyEventArgs e)
    {
        if (_vm is null)
            return;

        var prefix = _vm.ServerOutput;
        var box = TerminalBox;

        if (box.CaretIndex < prefix.Length &&
            (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left))
        {
            e.Handled = true;
            box.CaretIndex = prefix.Length;
            return;
        }

        if (e.Key != Key.Enter)
            return;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            return;

        e.Handled = true;
        var full = box.Text ?? string.Empty;
        if (!full.StartsWith(prefix, StringComparison.Ordinal))
            return;

        var userPart = full[prefix.Length..];
        _vm.SubmitLocalCommand(userPart);

        _terminalSync = true;
        try
        {
            _serverTextSnapshot = _vm.ServerOutput;
            box.Text = _vm.ServerOutput;
            box.CaretIndex = box.Text.Length;
        }
        finally
        {
            _terminalSync = false;
        }

        ScrollTerminalToEnd();
    }

    private void ScrollTerminalToEnd()
    {
        Dispatcher.UIThread.Post(() =>
        {
            TerminalScroll.Offset = new Avalonia.Vector(TerminalScroll.Offset.X, TerminalScroll.Extent.Height);
        }, DispatcherPriority.Background);
    }
}
