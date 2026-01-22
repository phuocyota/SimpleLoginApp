using System;

namespace SimpleLoginApp.Services;

public static class DeviceIdProvider
{
    private static readonly string DeviceId = $"device-{Environment.MachineName}";

    public static string Get() => DeviceId;
}
