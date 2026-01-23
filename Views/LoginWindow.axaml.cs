using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using SimpleLoginApp.Services;

namespace SimpleLoginApp.Views;

public partial class LoginWindow : Window
{
    private bool _showPassword;
    private bool _isLoggingIn;
    private readonly LoginService _loginService = new();

    public LoginWindow()
    {
        InitializeComponent();
    }

    private void TogglePassword(object? sender, RoutedEventArgs e)
    {
        _showPassword = !_showPassword;
        PasswordBox.PasswordChar = _showPassword ? '\0' : '*';

        // Nếu bạn có icon_eye_off.png thì bật 2 dòng dưới và đổi path đúng:
        // EyeIcon.Source = new Avalonia.Media.Imaging.Bitmap(_showPassword
        //     ? "avares://KidoTeacher/Assets/icon_eye_off.png"
        //     : "avares://KidoTeacher/Assets/icon_eye.png");
    }

    private async void HandleLogin(object? sender, RoutedEventArgs e)
    {
        var username = UsernameBox.Text?.Trim() ?? string.Empty;
        var password = PasswordBox.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            StatusLabel.Text = "Vui long nhap tai khoan va mat khau";
            return;
        }

        if (_isLoggingIn)
        {
            return;
        }

        _isLoggingIn = true;
        LoginButton.IsEnabled = false;
        StatusLabel.Text = "Dang dang nhap...";

        try
        {
            var deviceId = DeviceIdProvider.Get();
            var result = await _loginService.LoginAsync(username, password, deviceId);

            if (result.IsSuccess)
            {
                SessionStore.UserId = result.UserId;
                SessionStore.AccessToken = result.AccessToken;
                SessionStore.UserType = result.UserType;
                SessionStore.DeviceId = result.DeviceId ?? deviceId;

                var dashboard = new DashboardWindow();
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = dashboard;
                }

                dashboard.Show();
                Close();
            }
            else
            {
                StatusLabel.Text = result.ErrorMessage ?? "Dang nhap that bai";
            }
        }
        catch
        {
            StatusLabel.Text = "Khong the ket noi may chu";
        }
        finally
        {
            _isLoggingIn = false;
            LoginButton.IsEnabled = true;
        }
    }

    private void HandleLoginKey(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            e.Handled = true;
            HandleLogin(sender, new RoutedEventArgs());
        }
    }
}
