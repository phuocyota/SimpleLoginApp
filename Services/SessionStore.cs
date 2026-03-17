namespace SimpleLoginApp.Services;

public static class SessionStore
{
    public static string? UserId { get; set; }
    public static string? AccessToken { get; set; }
    public static string? UserType { get; set; }
    public static string? DeviceId { get; set; }

    public static void Set(string? userId, string? accessToken, string? userType, string? deviceId)
    {
        UserId = userId;
        AccessToken = accessToken;
        UserType = userType;
        DeviceId = deviceId;
    }

    public static void Clear()
    {
        Set(null, null, null, null);
    }
}
