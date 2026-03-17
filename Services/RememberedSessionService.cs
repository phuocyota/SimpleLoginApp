using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SimpleLoginApp.Services;

public sealed class RememberedSessionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    public void SaveCurrentSession()
    {
        var token = SessionStore.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var expiresAtUtc = GetTokenExpiryUtc(token);
        if (expiresAtUtc == null)
        {
            return;
        }

        var session = new SavedSession
        {
            UserId = SessionStore.UserId,
            AccessToken = token,
            UserType = SessionStore.UserType,
            DeviceId = SessionStore.DeviceId,
            ExpiresAtUtc = expiresAtUtc.Value,
        };

        var json = JsonSerializer.Serialize(session, JsonOptions);
        File.WriteAllText(CachePaths.GetTokenFilePath(), json, Encoding.UTF8);
    }

    public bool TryRestoreValidSession()
    {
        try
        {
            var path = CachePaths.GetTokenFilePath();
            if (!File.Exists(path))
            {
                return false;
            }

            var json = File.ReadAllText(path, Encoding.UTF8);
            var session = JsonSerializer.Deserialize<SavedSession>(json);
            if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
            {
                ClearSavedSession();
                return false;
            }

            var expiresAtUtc = session.ExpiresAtUtc;
            if (expiresAtUtc == default)
            {
                expiresAtUtc = GetTokenExpiryUtc(session.AccessToken) ?? default;
            }

            if (expiresAtUtc == default || expiresAtUtc <= DateTimeOffset.UtcNow)
            {
                ClearSavedSession();
                return false;
            }

            SessionStore.Set(session.UserId, session.AccessToken, session.UserType, session.DeviceId);
            return true;
        }
        catch
        {
            ClearSavedSession();
            return false;
        }
    }

    public void ClearSavedSession()
    {
        SessionStore.Clear();

        var path = CachePaths.GetTokenFilePath();
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static DateTimeOffset? GetTokenExpiryUtc(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payloadBytes = DecodeBase64Url(parts[1]);
            using var document = JsonDocument.Parse(payloadBytes);
            if (!document.RootElement.TryGetProperty("exp", out var expElement))
            {
                return null;
            }

            long expUnix;
            if (expElement.ValueKind == JsonValueKind.Number && expElement.TryGetInt64(out var numberValue))
            {
                expUnix = numberValue;
            }
            else if (expElement.ValueKind == JsonValueKind.String
                     && long.TryParse(expElement.GetString(), out var stringValue))
            {
                expUnix = stringValue;
            }
            else
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(expUnix);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        var padding = normalized.Length % 4;
        if (padding > 0)
        {
            normalized = normalized.PadRight(normalized.Length + (4 - padding), '=');
        }

        return Convert.FromBase64String(normalized);
    }

    private sealed class SavedSession
    {
        public string? UserId { get; set; }
        public string? AccessToken { get; set; }
        public string? UserType { get; set; }
        public string? DeviceId { get; set; }
        public DateTimeOffset ExpiresAtUtc { get; set; }
    }
}
