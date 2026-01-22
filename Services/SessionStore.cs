namespace SimpleLoginApp.Services;

public static class SessionStore
{
    public static string? UserId { get; set; }
    public static string? AccessToken { get; set; }
    public static string? UserType { get; set; }
    public static string? DeviceId { get; set; }
}
