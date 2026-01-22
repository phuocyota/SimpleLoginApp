using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleLoginApp.Models;

public sealed class CourseListResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public CourseListPayload? Data { get; set; }
}

public sealed class CourseListPayload
{
    [JsonPropertyName("data")]
    public List<CourseInfo>? Items { get; set; }
}

public sealed class CourseInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("orderNumber")]
    public int? OrderNumber { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("currentImage")]
    public string? CurrentImage { get; set; }
}

public sealed class CourseListResult
{
    private CourseListResult(bool isSuccess, List<CourseInfo> courses, string? error)
    {
        IsSuccess = isSuccess;
        Courses = courses;
        ErrorMessage = error;
    }

    public bool IsSuccess { get; }
    public List<CourseInfo> Courses { get; }
    public string? ErrorMessage { get; }

    public static CourseListResult Ok(List<CourseInfo> courses)
        => new(true, courses, null);

    public static CourseListResult Fail(string error)
        => new(false, new List<CourseInfo>(), error);
}
