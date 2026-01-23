using System.IO;
using Avalonia.Controls;

namespace SimpleLoginApp.Views;

public partial class LecturePdfWindow : Window
{
    public LecturePdfWindow()
    {
        InitializeComponent();
    }

    public void OpenPdf(string path, string? title = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        Title = string.IsNullOrWhiteSpace(title) ? "PDF" : title;
        PdfViewer.Source = Path.GetFullPath(path);
    }
}
