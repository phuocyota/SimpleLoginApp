using System;
using System.IO;
using Avalonia.Controls;

namespace SimpleLoginApp.Views;

public partial class LectureHtmlWindow : Window
{
    public LectureHtmlWindow()
    {
        InitializeComponent();
    }

    public void OpenHtml(string path, string? title = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        Title = string.IsNullOrWhiteSpace(title) ? "E-Learning" : title;
        var fullPath = Path.GetFullPath(path);
        WebView.Url = new Uri(fullPath);
    }
}
