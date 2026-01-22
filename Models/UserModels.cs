using System.Text.Json.Serialization;

namespace SimpleLoginApp.Models;

public sealed class UserResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public UserProfile? Data { get; set; }
}

public sealed class UserProfile
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("userType")]
    public string? UserType { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("birthday")]
    public string? Birthday { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("citizenId")]
    public string? CitizenId { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

public sealed class UserProfileResult
{
    private UserProfileResult(bool isSuccess, UserProfile? profile, string? error)
    {
        IsSuccess = isSuccess;
        Profile = profile;
        ErrorMessage = error;
    }

    public bool IsSuccess { get; }
    public UserProfile? Profile { get; }
    public string? ErrorMessage { get; }

    public static UserProfileResult Ok(UserProfile profile)
        => new(true, profile, null);

    public static UserProfileResult Fail(string error)
        => new(false, null, error);
}
