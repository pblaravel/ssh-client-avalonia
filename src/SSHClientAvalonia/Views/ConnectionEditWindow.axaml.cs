using Avalonia.Controls;
using Avalonia.Interactivity;
using SSHClientAvalonia.ViewModels;

namespace SSHClientAvalonia.Views;

public partial class ConnectionEditWindow : Window
{
    public ConnectionEditWindow()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ConnectionEditViewModel vm)
        {
            Close(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(vm.Host) || string.IsNullOrWhiteSpace(vm.Username))
        {
            vm.ValidationMessage = "Укажите хост и имя пользователя.";
            return;
        }

        if (string.IsNullOrEmpty(vm.Password)
            && (string.IsNullOrWhiteSpace(vm.PrivateKeyPath) || !File.Exists(vm.PrivateKeyPath.Trim())))
        {
            vm.ValidationMessage = "Укажите пароль или существующий файл SSH-ключа.";
            return;
        }

        vm.ValidationMessage = string.Empty;

        vm.ApplyToProfile();
        Close(true);
    }
}
