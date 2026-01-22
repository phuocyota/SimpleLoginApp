using System.Text.Json.Serialization;

namespace SimpleLoginApp.Models;

public sealed class LoginRequest
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }
}

public sealed class LoginResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public LoginData? Data { get; set; }
}

public sealed class LoginData
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("userType")]
    public string? UserType { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }
}

public sealed class LoginResult
{
    private LoginResult(bool isSuccess, string? userId, string? accessToken, string? userType, string? deviceId, string? error)
    {
        IsSuccess = isSuccess;
        UserId = userId;
        AccessToken = accessToken;
        UserType = userType;
        DeviceId = deviceId;
        ErrorMessage = error;
    }

    public bool IsSuccess { get; }
    public string? UserId { get; }
    public string? AccessToken { get; }
    public string? UserType { get; }
    public string? DeviceId { get; }
    public string? ErrorMessage { get; }

    public static LoginResult Ok(string userId, string? accessToken, string? userType, string? deviceId)
        => new(true, userId, accessToken, userType, deviceId, null);

    public static LoginResult Fail(string error)
        => new(false, null, null, null, null, error);
}
