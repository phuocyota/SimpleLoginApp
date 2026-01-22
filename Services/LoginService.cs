using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SimpleLoginApp.Models;

namespace SimpleLoginApp.Services;

public sealed class LoginService
{
    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri(ApiConfig.BaseUrl),
        Timeout = TimeSpan.FromSeconds(15),
    };

    public async Task<LoginResult> LoginAsync(
        string username,
        string password,
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        var payload = new LoginRequest
        {
            Username = username,
            Password = password,
            DeviceId = deviceId,
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await Client.PostAsync("auth/login/teacher", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return LoginResult.Fail($"Dang nhap that bai ({(int)response.StatusCode}).");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        var result = JsonSerializer.Deserialize<LoginResponse>(body, options);

        if (result?.Success == true && !string.IsNullOrWhiteSpace(result.Data?.UserId))
        {
            return LoginResult.Ok(
                result.Data!.UserId!,
                result.Data.AccessToken,
                result.Data.UserType,
                result.Data.DeviceId);
        }

        return LoginResult.Fail(result?.Message ?? "Dang nhap that bai.");
    }
}
