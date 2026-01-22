using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SimpleLoginApp.Models;

namespace SimpleLoginApp.Services;

public sealed class UserService
{
    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri(ApiConfig.BaseUrl),
        Timeout = TimeSpan.FromSeconds(15),
    };

    public async Task<UserProfileResult> GetUserAsync(
        string userId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"users/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

        using var response = await Client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return UserProfileResult.Fail($"Tai thong tin that bai ({(int)response.StatusCode}).");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        var result = JsonSerializer.Deserialize<UserResponse>(body, options);

        if (result?.Success == true && result.Data != null)
        {
            return UserProfileResult.Ok(result.Data);
        }

        return UserProfileResult.Fail(result?.Message ?? "Tai thong tin that bai.");
    }
}
