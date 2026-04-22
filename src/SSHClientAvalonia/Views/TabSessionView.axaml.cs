using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using SSHClientAvalonia.ViewModels;

namespace SSHClientAvalonia.Views;

public partial class TabSessionView : UserControl
{
    private TabSessionViewModel? _vm;

    public TabSessionView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
            _vm.PropertyChanged -= OnVmPropertyChanged;

        _vm = DataContext as TabSessionViewModel;
        if (_vm is not null)
            _vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TabSessionViewModel.TerminalOutput))
            ScrollTerminalToEnd();
    }

    private void ScrollTerminalToEnd()
    {
        Dispatcher.UIThread.Post(() =>
        {
            TerminalScroll.Offset = new Avalonia.Vector(TerminalScroll.Offset.X, TerminalScroll.Extent.Height);
        }, DispatcherPriority.Background);
    }

    private void OnCommandKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not TabSessionViewModel vm)
            return;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            return;

        e.Handled = true;
        if (vm.SendCommandCommand.CanExecute(null))
            vm.SendCommandCommand.Execute(null);
    }
}
