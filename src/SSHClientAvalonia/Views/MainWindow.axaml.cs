using Avalonia.Controls;
using Avalonia.Interactivity;
using SSHClientAvalonia.ViewModels;

namespace SSHClientAvalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.AttachHost(this);
    }
}