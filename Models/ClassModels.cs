using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleLoginApp.Models;

public sealed class ClassListResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public List<ClassInfo>? Data { get; set; }
}

public sealed class ClassInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("orderNumber")]
    public int? OrderNumber { get; set; }

    [JsonPropertyName("currentImage")]
    public string? CurrentImage { get; set; }
}

public sealed class ClassListResult
{
    private ClassListResult(bool isSuccess, List<ClassInfo> classes, string? error)
    {
        IsSuccess = isSuccess;
        Classes = classes;
        ErrorMessage = error;
    }

    public bool IsSuccess { get; }
    public List<ClassInfo> Classes { get; }
    public string? ErrorMessage { get; }

    public static ClassListResult Ok(List<ClassInfo> classes)
        => new(true, classes, null);

    public static ClassListResult Fail(string error)
        => new(false, new List<ClassInfo>(), error);
}
