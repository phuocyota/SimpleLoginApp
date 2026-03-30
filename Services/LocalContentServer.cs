using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleLoginApp.Services;

public sealed class LocalContentServer : IDisposable
{
    private static readonly Lazy<LocalContentServer> LazyInstance = new(() => new LocalContentServer());
    private readonly ConcurrentDictionary<string, string> _roots = new(StringComparer.Ordinal);
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _serverTask;
    private readonly int _port;

    public static LocalContentServer Instance => LazyInstance.Value;

    private LocalContentServer()
    {
        _port = GetAvailablePort();
        _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
        _listener.Start();
        _serverTask = Task.Run(() => RunAsync(_cts.Token));
    }

    public Uri RegisterFile(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        var rootDirectory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new InvalidOperationException("Unable to determine content root.");
        }

        var rootId = GetOrAddRoot(rootDirectory);
        var fileName = Path.GetFileName(fullPath);
        return new Uri($"http://127.0.0.1:{_port}/content/{Uri.EscapeDataString(rootId)}/{Uri.EscapeDataString(fileName)}");
    }

    public void Dispose()
    {
        _cts.Cancel();
        if (_listener.IsListening)
        {
            _listener.Stop();
        }

        _listener.Close();
        _cts.Dispose();
    }

    private string GetOrAddRoot(string rootDirectory)
    {
        foreach (var pair in _roots)
        {
            if (string.Equals(pair.Value, rootDirectory, StringComparison.Ordinal))
            {
                return pair.Key;
            }
        }

        var id = Guid.NewGuid().ToString("N");
        _roots[id] = rootDirectory;
        return id;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext? context = null;
            try
            {
                context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context), cancellationToken);
            }
            catch when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                if (context != null)
                {
                    TryClose(context.Response);
                }
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            if (!TryResolvePath(context.Request.Url, out var filePath))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                TryClose(context.Response);
                return;
            }

            if (!File.Exists(filePath))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                TryClose(context.Response);
                return;
            }

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = GetContentType(filePath);
            await using var input = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            context.Response.ContentLength64 = input.Length;
            await input.CopyToAsync(context.Response.OutputStream);
            context.Response.OutputStream.Close();
        }
        catch
        {
            try
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                TryClose(context.Response);
            }
            catch
            {
                // Ignore secondary response failures.
            }
        }
    }

    private bool TryResolvePath(Uri? requestUri, out string filePath)
    {
        filePath = string.Empty;
        if (requestUri == null)
        {
            return false;
        }

        var segments = requestUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 3 || !string.Equals(segments[0], "content", StringComparison.Ordinal))
        {
            return false;
        }

        var rootId = Uri.UnescapeDataString(segments[1]);
        if (!_roots.TryGetValue(rootId, out var rootDirectory))
        {
            return false;
        }

        var relativeSegments = new string[segments.Length - 2];
        for (var i = 2; i < segments.Length; i++)
        {
            relativeSegments[i - 2] = Uri.UnescapeDataString(segments[i]);
        }

        var combinedRelativePath = Path.Combine(relativeSegments);
        var fullPath = Path.GetFullPath(Path.Combine(rootDirectory, combinedRelativePath));
        var fullRoot = Path.GetFullPath(rootDirectory) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(fullRoot, StringComparison.Ordinal))
        {
            return false;
        }

        filePath = fullPath;
        return true;
    }

    private static int GetAvailablePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string GetContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".html" => "text/html; charset=utf-8",
            ".htm" => "text/html; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".xml" => "application/xml; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".otf" => "font/otf",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream",
        };
    }

    private static void TryClose(HttpListenerResponse response)
    {
        try
        {
            response.OutputStream.Close();
        }
        catch
        {
            // Ignore response close failures.
        }
    }
}
