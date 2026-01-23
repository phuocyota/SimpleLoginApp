using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SimpleLoginApp.Models;

namespace SimpleLoginApp.Services;

public sealed class LectureService
{
    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri(ApiConfig.BaseUrl),
        Timeout = TimeSpan.FromSeconds(15),
    };

    public async Task<LectureListResult> GetLecturesAsync(
        string accessToken,
        string courseId,
        CancellationToken cancellationToken = default)
    {
        var url = $"lecture?page=1&size=100&courseId={Uri.EscapeDataString(courseId)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await Client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return LectureListResult.Fail($"Tai bai hoc that bai ({(int)response.StatusCode}).");
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

            List<LectureInfo>? items = null;
            if (root.TryGetProperty("data", out var dataElement)
                && dataElement.ValueKind == JsonValueKind.Object
                && dataElement.TryGetProperty("data", out var inner)
                && inner.ValueKind == JsonValueKind.Array)
            {
                items = JsonSerializer.Deserialize<List<LectureInfo>>(inner.GetRawText(), options);
            }

            if (success && items is { })
            {
                return LectureListResult.Ok(items);
            }

            return LectureListResult.Fail(message ?? "Tai bai hoc that bai.");
        }
        catch
        {
            return LectureListResult.Fail("Tai bai hoc that bai.");
        }
    }

    public async Task<LectureResourceResult> GetFirstResourceUrlAsync(
        string accessToken,
        string lectureId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"lecture/{Uri.EscapeDataString(lectureId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await Client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return LectureResourceResult.Fail($"Tai tai nguyen that bai ({(int)response.StatusCode}).");
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            var result = JsonSerializer.Deserialize<LectureDetailResponse>(body, options);
            var url = result?.Data?.Resources?
                .FirstOrDefault(resource => !string.IsNullOrWhiteSpace(resource.Url))
                ?.Url;

            if (result?.Success == true && !string.IsNullOrWhiteSpace(url))
            {
                return LectureResourceResult.Ok(url);
            }

            return LectureResourceResult.Fail(result?.Message ?? "Khong tim thay tai nguyen.");
        }
        catch
        {
            return LectureResourceResult.Fail("Khong the tai tai nguyen.");
        }
    }
}
