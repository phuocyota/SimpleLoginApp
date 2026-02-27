using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
#if NET8_0_OR_GREATER
using AvaloniaPdfViewer;
#endif

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

        var fullPath = Path.GetFullPath(path);
        Title = string.IsNullOrWhiteSpace(title) ? "PDF" : title;

#if NET8_0_OR_GREATER
        if (RootGrid.Children.Count == 0 || RootGrid.Children[0] is not PdfViewer viewer)
        {
            viewer = new PdfViewer();
            RootGrid.Children.Clear();
            RootGrid.Children.Add(viewer);
        }
        viewer.Source = fullPath;
#else
        PdfStatus.IsVisible = true;
        PdfStatus.Text = "Legacy mode: mở PDF bằng ứng dụng mặc định của hệ thống.";
        _ = Process.Start(new ProcessStartInfo
        {
            FileName = fullPath,
            UseShellExecute = true,
        });
#endif
    }
}
