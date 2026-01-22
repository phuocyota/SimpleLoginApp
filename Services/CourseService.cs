using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SimpleLoginApp.Models;

namespace SimpleLoginApp.Services;

public sealed class CourseService
{
    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri(ApiConfig.BaseUrl),
        Timeout = TimeSpan.FromSeconds(15),
    };

    public async Task<CourseListResult> GetCoursesAsync(
        string accessToken,
        string classId,
        CancellationToken cancellationToken = default)
    {
        var url = $"courses?classId={Uri.EscapeDataString(classId)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

        using var response = await Client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return CourseListResult.Fail($"Tai course that bai ({(int)response.StatusCode}).");
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var success = root.TryGetProperty("success", out var successElement) && successElement.GetBoolean();
            var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : null;

            List<CourseInfo>? items = null;
            if (root.TryGetProperty("data", out var dataElement))
            {
                if (dataElement.ValueKind == JsonValueKind.Array)
                {
                    items = JsonSerializer.Deserialize<List<CourseInfo>>(dataElement.GetRawText(), options);
                }
                else if (dataElement.ValueKind == JsonValueKind.Object
                         && dataElement.TryGetProperty("data", out var inner)
                         && inner.ValueKind == JsonValueKind.Array)
                {
                    items = JsonSerializer.Deserialize<List<CourseInfo>>(inner.GetRawText(), options);
                }
            }

            if (success && items is { })
            {
                return CourseListResult.Ok(items);
            }

            return CourseListResult.Fail(message ?? "Tai course that bai.");
        }
        catch
        {
            return CourseListResult.Fail("Tai course that bai.");
        }
    }
}
