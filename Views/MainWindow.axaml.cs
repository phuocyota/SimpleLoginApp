using Avalonia.Controls;

namespace SimpleLoginApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => Root.Classes.Add("loaded");
    }
}
