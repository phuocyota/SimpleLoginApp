using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SimpleLoginApp.Models;

namespace SimpleLoginApp.Services;

public sealed class ClassService
{
    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri(ApiConfig.BaseUrl),
        Timeout = TimeSpan.FromSeconds(15),
    };

    public async Task<ClassListResult> GetClassesAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "classes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

        using var response = await Client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return ClassListResult.Fail($"Tai giao an that bai ({(int)response.StatusCode}).");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        var result = JsonSerializer.Deserialize<ClassListResponse>(body, options);

        if (result?.Success == true && result.Data != null)
        {
            return ClassListResult.Ok(result.Data);
        }

        return ClassListResult.Fail(result?.Message ?? "Tai giao an that bai.");
    }
}
