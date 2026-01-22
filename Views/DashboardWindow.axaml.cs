using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using SimpleLoginApp.Models;
using SimpleLoginApp.Services;

namespace SimpleLoginApp.Views;

public partial class DashboardWindow : Window
{
    private readonly List<Bitmap> _slides = new();
    private readonly List<Bitmap> _classImages = new();
    private Bitmap? _defaultLessonBitmap;
    private int _slideIndex;
    private DispatcherTimer? _slideTimer;
    private readonly UserService _userService = new();
    private bool _accountLoaded;
    private bool _accountLoading;
    private readonly ClassService _classService = new();
    private bool _classesLoaded;
    private bool _classesLoading;
    private readonly CourseService _courseService = new();
    private bool _coursesLoading;
    private bool _coursesLoaded;
    private string? _coursesClassId;
    private string? _coursesClassName;
    private readonly LectureService _lectureService = new();
    private bool _lecturesLoading;
    private bool _lecturesLoaded;
    private string? _lecturesCourseId;
    private string? _lecturesCourseName;
    private readonly List<Bitmap> _courseImages = new();
    private readonly List<Bitmap> _lectureImages = new();
    private static readonly HttpClient ImageClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15),
    };

    private enum PanelType
    {
        Intro,
        Account,
        Lesson
    }

    public DashboardWindow()
    {
        InitializeComponent();
        LoadSlides();
        ShowPanel(PanelType.Intro);
        StartSlideShow();
    }

    private void ShowIntro(object? sender, RoutedEventArgs e)
    {
        ShowPanel(PanelType.Intro);
    }

    private void ShowAccount(object? sender, RoutedEventArgs e)
    {
        ShowPanel(PanelType.Account);
        _ = LoadAccountAsync();
    }

    private void ShowLesson(object? sender, RoutedEventArgs e)
    {
        ShowPanel(PanelType.Lesson);
        ShowLessonClassView();
        _ = LoadClassesAsync();
    }

    private void ShowPanel(PanelType panel)
    {
        IntroPanel.IsVisible = panel == PanelType.Intro;
        AccountPanel.IsVisible = panel == PanelType.Account;
        LessonPanel.IsVisible = panel == PanelType.Lesson;

        SetActive(IntroButton, panel == PanelType.Intro);
        SetActive(AccountButton, panel == PanelType.Account);
        SetActive(LessonButton, panel == PanelType.Lesson);

        if (panel == PanelType.Intro && _slides.Count > 0)
        {
            IntroSlide.Source = _slides[_slideIndex];
        }
    }

    private static void SetActive(Button button, bool isActive)
    {
        button.Classes.Set("active", isActive);
    }

    private void LoadSlides()
    {
        var slideUris = new[]
        {
            "avares://SimpleLoginApp/Assets/slide2.jpg",
            "avares://SimpleLoginApp/Assets/slide8.jpg",
            "avares://SimpleLoginApp/Assets/slide9.png",
            "avares://SimpleLoginApp/Assets/slide10.png",
            "avares://SimpleLoginApp/Assets/slide11.png",
        };

        foreach (var uri in slideUris)
        {
            using var stream = AssetLoader.Open(new Uri(uri));
            _slides.Add(new Bitmap(stream));
        }

        if (_slides.Count > 0)
        {
            IntroSlide.Source = _slides[0];
        }
    }

    private void StartSlideShow()
    {
        if (_slides.Count <= 1)
        {
            return;
        }

        _slideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _slideTimer.Tick += (_, _) => AdvanceSlide();
        _slideTimer.Start();
    }

    private void AdvanceSlide()
    {
        if (!IntroPanel.IsVisible || _slides.Count == 0)
        {
            return;
        }

        _slideIndex = (_slideIndex + 1) % _slides.Count;
        IntroSlide.Source = _slides[_slideIndex];
    }

    protected override void OnClosed(EventArgs e)
    {
        _slideTimer?.Stop();
        _slideTimer = null;

        foreach (var slide in _slides)
        {
            slide.Dispose();
        }
        _slides.Clear();

        foreach (var image in _classImages)
        {
            image.Dispose();
        }
        _classImages.Clear();

        foreach (var image in _courseImages)
        {
            image.Dispose();
        }
        _courseImages.Clear();

        foreach (var image in _lectureImages)
        {
            image.Dispose();
        }
        _lectureImages.Clear();

        _defaultLessonBitmap?.Dispose();
        _defaultLessonBitmap = null;

        base.OnClosed(e);
    }

    private async Task LoadAccountAsync()
    {
        if (_accountLoading || _accountLoaded)
        {
            return;
        }

        _accountLoading = true;
        AccountStatus.Text = "\u0110ang t\u1EA3i th\u00F4ng tin...";

        try
        {
            var userId = SessionStore.UserId;
            var token = SessionStore.AccessToken;

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                AccountStatus.Text = "Ch\u01B0a \u0111ang nh\u1EADp";
                return;
            }

            var result = await _userService.GetUserAsync(userId, token);
            if (!result.IsSuccess || result.Profile == null)
            {
                AccountStatus.Text = result.ErrorMessage ?? "Kh\u00F4ng th\u1EC3 t\u1EA3i th\u00F4ng tin";
                return;
            }

            var profile = result.Profile;

            AccountIdBox.Text = profile.Id ?? string.Empty;
            AccountTypeBox.Text = profile.UserType ?? string.Empty;
            AccountUsernameBox.Text = profile.UserName ?? string.Empty;
            AccountFullNameBox.Text = profile.FullName ?? string.Empty;
            AccountEmailBox.Text = profile.Email ?? string.Empty;
            AccountActivatedBox.Text = FormatDate(profile.CreatedAt) ?? FormatDate(profile.Birthday) ?? string.Empty;
            AccountExpiresBox.Text = FormatDate(profile.UpdatedAt) ?? string.Empty;

            _accountLoaded = true;
            AccountStatus.Text = string.Empty;
        }
        catch
        {
            AccountStatus.Text = "Kh\u00F4ng th\u1EC3 k\u1EBFt n\u1ED1i m\u00E1y ch\u1EE7";
        }
        finally
        {
            _accountLoading = false;
        }
    }

    private static string? FormatDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var index = value.IndexOf('T');
        return index > 0 ? value[..index] : value;
    }

    private async Task LoadClassesAsync()
    {
        if (_classesLoading || _classesLoaded)
        {
            return;
        }

        _classesLoading = true;
            LessonStatus.Text = "\u0110ang t\u1EA3i gi\u00E1o \u00E1n...";
            LessonStatus.IsVisible = true;

        try
        {
            var token = SessionStore.AccessToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                LessonStatus.Text = "Ch\u01B0a \u0111ang nh\u1EADp";
                return;
            }

            var result = await _classService.GetClassesAsync(token);
            if (!result.IsSuccess)
            {
                LessonStatus.Text = result.ErrorMessage ?? "Kh\u00F4ng th\u1EC3 t\u1EA3i gi\u00E1o \u00E1n";
                return;
            }

            LessonItemsPanel.Children.Clear();

            foreach (var item in result.Classes
                         .OrderBy(c => c.OrderNumber ?? int.MaxValue)
                         .ThenBy(c => c.Name))
            {
                var card = BuildLessonCard(item);
                card.PointerPressed += (_, _) => _ = LoadCoursesAsync(item);
                LessonItemsPanel.Children.Add(card);
            }

            LessonStatus.Text = string.Empty;
            LessonStatus.IsVisible = false;
            _classesLoaded = true;
        }
        catch
        {
            LessonStatus.Text = "Kh\u00F4ng th\u1EC3 k\u1EBFt n\u1ED1i m\u00E1y ch\u1EE7";
            LessonStatus.IsVisible = true;
        }
        finally
        {
            _classesLoading = false;
        }
    }

    private Control BuildLessonCard(ClassInfo info)
    {
        var image = new Image
        {
            Width = 200,
            Height = 200,
            Stretch = Stretch.UniformToFill
        };

        RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.HighQuality);
        image.Source = LoadDefaultLessonBitmap();
        _ = LoadClassImageAsync(image, info);

        var imageHost = new Border
        {
            Width = 200,
            Height = 200,
            CornerRadius = new CornerRadius(10),
            ClipToBounds = true,
            Child = image,
        };
        imageHost.Classes.Add("class-image");

        var content = new StackPanel
        {
            Spacing = 4,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };
        content.Children.Add(imageHost);

        return new Border
        {
            Width = 220,
            Height = 220,
            Margin = new Thickness(8),
            Padding = new Thickness(4),
            Child = content,
            Classes = { "class-card" },
        };
    }

    private Bitmap LoadDefaultLessonBitmap()
    {
        if (_defaultLessonBitmap != null)
        {
            return _defaultLessonBitmap;
        }

        using var stream = AssetLoader.Open(new Uri("avares://SimpleLoginApp/Assets/lessondefault.png"));
        _defaultLessonBitmap = new Bitmap(stream);
        return _defaultLessonBitmap;
    }

    private async Task LoadClassImageAsync(Image image, ClassInfo info)
    {
        var classId = info.Id;
        var cachePath = !string.IsNullOrWhiteSpace(classId)
            ? CachePaths.GetClassImagePath(classId)
            : null;

        var cached = TryLoadCachedBitmap(cachePath);
        if (cached != null)
        {
            _classImages.Add(cached);
            await Dispatcher.UIThread.InvokeAsync(() => image.Source = cached);
            return;
        }

        var bitmap = await TryLoadRemoteImageAsync(info.CurrentImage, cachePath);
        if (bitmap == null)
        {
            return;
        }

        _classImages.Add(bitmap);
        await Dispatcher.UIThread.InvokeAsync(() => image.Source = bitmap);
    }

    private static Uri BuildImageUri(string currentImage)
    {
        if (Uri.TryCreate(currentImage, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        return new Uri(new Uri(ApiConfig.BaseUrl), currentImage.TrimStart('/'));
    }

    private static Bitmap? TryLoadCachedBitmap(string? cachePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(cachePath) || !File.Exists(cachePath))
            {
                return null;
            }

            using var stream = File.OpenRead(cachePath);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<Bitmap?> TryLoadRemoteImageAsync(string? currentImage, string? cachePath)
    {
        if (string.IsNullOrWhiteSpace(currentImage))
        {
            return null;
        }

        try
        {
            var uri = BuildImageUri(currentImage);
            var bytes = await ImageClient.GetByteArrayAsync(uri);

            if (!string.IsNullOrWhiteSpace(cachePath))
            {
                await File.WriteAllBytesAsync(cachePath, bytes);
            }

            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private async Task LoadCoursesAsync(ClassInfo info)
    {
        if (_coursesLoading)
        {
            return;
        }

        if (_coursesLoaded && _coursesClassId == info.Id)
        {
            ShowLessonCourseView(info.Name);
            return;
        }

        _coursesLoading = true;
        _coursesClassId = info.Id;
        _coursesClassName = info.Name;
        ShowLessonCourseView(info.Name);
        LessonStatus.Text = "\u0110ang t\u1EA3i gi\u00E1o \u00E1n...";
        LessonStatus.IsVisible = true;

        try
        {
            var token = SessionStore.AccessToken;
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(info.Id))
            {
                LessonStatus.Text = "Ch\u01B0a \u0111ang nh\u1EADp";
                LessonStatus.IsVisible = true;
                return;
            }

            var result = await _courseService.GetCoursesAsync(token, info.Id);
            if (!result.IsSuccess)
            {
                LessonStatus.Text = result.ErrorMessage ?? "Kh\u00F4ng th\u1EC3 t\u1EA3i gi\u00E1o \u00E1n";
                LessonStatus.IsVisible = true;
                return;
            }

            CourseItemsPanel.Children.Clear();

            foreach (var item in result.Courses
                         .OrderBy(c => c.OrderNumber ?? int.MaxValue)
                         .ThenBy(c => c.Name))
            {
                var card = BuildCourseCard(item);
                card.PointerPressed += (_, _) => _ = LoadLecturesAsync(item);
                CourseItemsPanel.Children.Add(card);
            }

            LessonStatus.Text = string.Empty;
            LessonStatus.IsVisible = false;
            _coursesLoaded = true;
        }
        catch
        {
            LessonStatus.Text = "Kh\u00F4ng th\u1EC3 k\u1EBFt n\u1ED1i m\u00E1y ch\u1EE7";
            LessonStatus.IsVisible = true;
        }
        finally
        {
            _coursesLoading = false;
        }
    }

    private Control BuildCourseCard(CourseInfo info)
    {
        var image = new Image
        {
            Width = 200,
            Height = 200,
            Stretch = Stretch.UniformToFill
        };

        RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.HighQuality);
        image.Source = LoadDefaultLessonBitmap();
        _ = LoadCourseImageAsync(image, info);

        var imageHost = new Border
        {
            Width = 200,
            Height = 200,
            CornerRadius = new CornerRadius(10),
            ClipToBounds = true,
            Child = image,
        };
        imageHost.Classes.Add("course-image");

        var content = new StackPanel
        {
            Spacing = 4,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };
        content.Children.Add(imageHost);

        return new Border
        {
            Width = 220,
            Height = 220,
            Margin = new Thickness(8),
            Padding = new Thickness(4),
            Child = content,
            Classes = { "course-card" },
        };
    }

    private async Task LoadCourseImageAsync(Image image, CourseInfo info)
    {
        var courseId = info.Id;
        var cachePath = !string.IsNullOrWhiteSpace(courseId)
            ? CachePaths.GetCourseImagePath(courseId)
            : null;

        var cached = TryLoadCachedBitmap(cachePath);
        if (cached != null)
        {
            _courseImages.Add(cached);
            await Dispatcher.UIThread.InvokeAsync(() => image.Source = cached);
            return;
        }

        var bitmap = await TryLoadRemoteImageAsync(info.Image ?? info.CurrentImage, cachePath);
        if (bitmap == null)
        {
            return;
        }

        _courseImages.Add(bitmap);
        await Dispatcher.UIThread.InvokeAsync(() => image.Source = bitmap);
    }

    private async Task LoadLecturesAsync(CourseInfo info)
    {
        if (_lecturesLoading)
        {
            return;
        }

        if (_lecturesLoaded && _lecturesCourseId == info.Id)
        {
            ShowLessonLectureView(info.Name);
            return;
        }

        _lecturesLoading = true;
        _lecturesCourseId = info.Id;
        _lecturesCourseName = info.Name;
        ShowLessonLectureView(info.Name);
        LessonStatus.Text = "\u0110ang t\u1EA3i gi\u00E1o \u00E1n...";
        LessonStatus.IsVisible = true;

        try
        {
            var token = SessionStore.AccessToken;
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(info.Id))
            {
                LessonStatus.Text = "Ch\u01B0a \u0111ang nh\u1EADp";
                LessonStatus.IsVisible = true;
                return;
            }

            var result = await _lectureService.GetLecturesAsync(token, info.Id);
            if (!result.IsSuccess)
            {
                LessonStatus.Text = result.ErrorMessage ?? "Kh\u00F4ng th\u1EC3 t\u1EA3i gi\u00E1o \u00E1n";
                LessonStatus.IsVisible = true;
                return;
            }

            LectureItemsPanel.Children.Clear();

            foreach (var item in result.Lectures
                         .OrderBy(l => l.OrderColumn ?? int.MaxValue)
                         .ThenBy(l => l.Title))
            {
                var row = BuildLectureRow(item);
                LectureItemsPanel.Children.Add(row);
            }

            LessonStatus.Text = string.Empty;
            LessonStatus.IsVisible = false;
            _lecturesLoaded = true;
        }
        catch
        {
            LessonStatus.Text = "Kh\u00F4ng th\u1EC3 k\u1EBFt n\u1ED1i m\u00E1y ch\u1EE7";
            LessonStatus.IsVisible = true;
        }
        finally
        {
            _lecturesLoading = false;
        }
    }

    private Control BuildLectureRow(LectureInfo info)
    {
        var image = new Image
        {
            Width = 120,
            Height = 120,
            Stretch = Stretch.UniformToFill
        };
        RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.HighQuality);
        image.Source = LoadDefaultLessonBitmap();
        _ = LoadLectureImageAsync(image, info);

        var imageHost = new Border
        {
            Width = 140,
            Height = 140,
            Background = Brushes.White,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6),
            Child = image,
        };

        var title = new TextBlock
        {
            Text = info.Title ?? string.Empty,
            FontWeight = FontWeight.Bold,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
        };

        var code = new TextBlock
        {
            Text = info.Code != null ? $"M\u00E3 s\u1ED1: {info.Code}" : string.Empty,
            Foreground = Brushes.Blue,
            FontSize = 11,
        };

        var infoPanel = new StackPanel
        {
            Spacing = 6,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };
        infoPanel.Children.Add(title);
        infoPanel.Children.Add(code);

        var onlineGroup = new StackPanel
        {
            Spacing = 6,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0),
        };
        onlineGroup.Children.Add(new TextBlock
        {
            Text = "Xem Online",
            Foreground = Brushes.Green,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        });
        var onlineButtons = new List<Button>
        {
            MakeLectureButton("Gi\u00E1o \u00E1n PDF", "lecture-action online"),
            MakeLectureButton("Video d\u1EA1y m\u1EABu", "lecture-action online"),
            MakeLectureButton("B\u00E0i gi\u1EA3ng E-Learning", "lecture-action online"),
        };
        foreach (var button in onlineButtons)
        {
            onlineGroup.Children.Add(button);
        }

        var offlineGroup = new StackPanel
        {
            Spacing = 6,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0),
        };
        offlineGroup.Children.Add(new TextBlock
        {
            Text = "Xem Offline",
            Foreground = Brushes.Blue,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        });
        offlineGroup.Children.Add(MakeLectureButton("Gi\u00E1o \u00E1n PDF", "lecture-action offline"));
        offlineGroup.Children.Add(MakeLectureButton("Video d\u1EA1y m\u1EABu", "lecture-action offline"));
        offlineGroup.Children.Add(MakeLectureButton("B\u00E0i gi\u1EA3ng E-Learning", "lecture-action offline"));

        var statusGroup = new StackPanel
        {
            Spacing = 6,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0),
        };
        var deleteButton = new Button
        {
            Content = "X\u00F3a",
            Classes = { "lecture-delete" },
        };
        statusGroup.Children.Add(deleteButton);

        var downloadButton = new Button
        {
            Content = "T\u1EA3i v\u1EC1",
            Classes = { "lecture-download" },
        };
        statusGroup.Children.Add(downloadButton);

        var downloadProgress = new ProgressBar
        {
            Width = 110,
            Height = 8,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            IsVisible = false,
        };
        statusGroup.Children.Add(downloadProgress);

        if (HasLectureCache(info.Id))
        {
            downloadButton.IsEnabled = false;
            foreach (var button in onlineButtons)
            {
                button.IsEnabled = false;
            }
        }
        else
        {
            downloadButton.Click += async (_, _) =>
                await DownloadLectureAsync(info, downloadButton, onlineButtons, downloadProgress);
        }

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto,Auto"),
            RowDefinitions = new RowDefinitions("Auto"),
            ColumnSpacing = 20,
        };

        var gridHost = new Border
        {
            Padding = new Thickness(10),
            Child = grid,
        };

        grid.Children.Add(imageHost);
        Grid.SetColumn(imageHost, 0);

        grid.Children.Add(infoPanel);
        Grid.SetColumn(infoPanel, 1);

        grid.Children.Add(onlineGroup);
        Grid.SetColumn(onlineGroup, 2);

        grid.Children.Add(offlineGroup);
        Grid.SetColumn(offlineGroup, 3);

        grid.Children.Add(statusGroup);
        Grid.SetColumn(statusGroup, 4);

        return new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            Child = gridHost,
        };
    }

    private static Button MakeLectureButton(string text, string classes)
    {
        var button = new Button
        {
            Content = text,
        };
        foreach (var cls in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            button.Classes.Add(cls);
        }

        return button;
    }

    private async Task LoadLectureImageAsync(Image image, LectureInfo info)
    {
        var bitmap = await TryLoadRemoteImageAsync(info.Avatar, null);
        if (bitmap == null)
        {
            return;
        }

        _lectureImages.Add(bitmap);
        await Dispatcher.UIThread.InvokeAsync(() => image.Source = bitmap);
    }

    private static bool HasLectureCache(string? lectureId)
    {
        if (string.IsNullOrWhiteSpace(lectureId))
        {
            return false;
        }

        var path = CachePaths.GetLecturePath(lectureId);
        return File.Exists(path);
    }

    private async Task DownloadLectureAsync(
        LectureInfo info,
        Button downloadButton,
        IReadOnlyList<Button> onlineButtons,
        ProgressBar progressBar)
    {
        if (string.IsNullOrWhiteSpace(info.Id))
        {
            return;
        }

        downloadButton.IsEnabled = false;
        progressBar.IsVisible = true;
        progressBar.IsIndeterminate = false;
        progressBar.Value = 0;

        try
        {
            if (HasLectureCache(info.Id))
            {
                foreach (var button in onlineButtons)
                {
                    button.IsEnabled = false;
                }
                progressBar.IsVisible = false;
                return;
            }

            var path = CachePaths.GetLecturePath(info.Id);
            if (!string.IsNullOrWhiteSpace(info.Avatar))
            {
                var uri = BuildImageUri(info.Avatar);
                using var response = await ImageClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var total = response.Content.Headers.ContentLength;
                if (total.HasValue && total.Value > 0)
                {
                    progressBar.Maximum = total.Value;
                }
                else
                {
                    progressBar.IsIndeterminate = true;
                }

                await using var input = await response.Content.ReadAsStreamAsync();
                await using var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                var buffer = new byte[81920];
                long totalRead = 0;
                int read;
                while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read));
                    totalRead += read;

                    if (total.HasValue && total.Value > 0)
                    {
                        var value = totalRead;
                        await Dispatcher.UIThread.InvokeAsync(() => progressBar.Value = value);
                    }
                }

                if (!total.HasValue || total.Value <= 0)
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = progressBar.Maximum;
                }
            }
            else
            {
                await File.WriteAllTextAsync(path, string.Empty);
            }

            foreach (var button in onlineButtons)
            {
                button.IsEnabled = false;
            }

            progressBar.IsVisible = false;
        }
        catch
        {
            downloadButton.IsEnabled = true;
            progressBar.IsVisible = false;
            progressBar.IsIndeterminate = false;
        }
    }

    private void ShowLessonCourseView(string? className)
    {
        LessonClassView.IsVisible = false;
        LessonCourseView.IsVisible = true;
        LessonLectureView.IsVisible = false;
        LessonBackButton.IsVisible = true;
        LessonHeaderText.Text = string.IsNullOrWhiteSpace(className)
            ? "Gi\u00E1o \u00C1n"
            : $"Gi\u00E1o \u00C1n / {className}";
    }

    private void ShowLessonClassView()
    {
        LessonClassView.IsVisible = true;
        LessonCourseView.IsVisible = false;
        LessonLectureView.IsVisible = false;
        LessonBackButton.IsVisible = false;
        LessonHeaderText.Text = "Gi\u00E1o \u00C1n";
    }

    private void HandleLessonBack(object? sender, RoutedEventArgs e)
    {
        if (LessonLectureView.IsVisible)
        {
            ShowLessonCourseView(_coursesClassName);
        }
        else
        {
            ShowLessonClassView();
        }
    }

    private void ShowLessonLectureView(string? courseName)
    {
        LessonClassView.IsVisible = false;
        LessonCourseView.IsVisible = false;
        LessonLectureView.IsVisible = true;
        LessonBackButton.IsVisible = true;
        LessonHeaderText.Text = string.IsNullOrWhiteSpace(courseName)
            ? "Gi\u00E1o \u00C1n"
            : $"Gi\u00E1o \u00C1n / {_coursesClassName} / {courseName}";
    }
}
