using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleLoginApp.Models;

public sealed class LectureListResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public LectureListPayload? Data { get; set; }
}

public sealed class LectureListPayload
{
    [JsonPropertyName("data")]
    public List<LectureInfo>? Items { get; set; }
}

public sealed class LectureInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("orderColumn")]
    public int? OrderColumn { get; set; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("courseId")]
    public string? CourseId { get; set; }
}

public sealed class LectureDetailResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public LectureDetail? Data { get; set; }
}

public sealed class LectureDetail
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("resources")]
    public List<LectureResource>? Resources { get; set; }
}

public sealed class LectureResource
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

public sealed class LectureListResult
{
    private LectureListResult(bool isSuccess, List<LectureInfo> lectures, string? error)
    {
        IsSuccess = isSuccess;
        Lectures = lectures;
        ErrorMessage = error;
    }

    public bool IsSuccess { get; }
    public List<LectureInfo> Lectures { get; }
    public string? ErrorMessage { get; }

    public static LectureListResult Ok(List<LectureInfo> lectures)
        => new(true, lectures, null);

    public static LectureListResult Fail(string error)
        => new(false, new List<LectureInfo>(), error);
}

public sealed class LectureResourceResult
{
    private LectureResourceResult(bool isSuccess, string? resourceUrl, string? error)
    {
        IsSuccess = isSuccess;
        ResourceUrl = resourceUrl;
        ErrorMessage = error;
    }

    public bool IsSuccess { get; }
    public string? ResourceUrl { get; }
    public string? ErrorMessage { get; }

    public static LectureResourceResult Ok(string resourceUrl)
        => new(true, resourceUrl, null);

    public static LectureResourceResult Fail(string error)
        => new(false, null, error);
}
