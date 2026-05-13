namespace RinhaApi.Services;

/// <summary>
/// Caches DateTime.UtcNow to avoid repeated system calls under high concurrency.
/// Accuracy is within 1 second, which is sufficient for minutesSinceLastTx.
/// </summary>
public static class CachedClock
{
    private static DateTime _utcNow = DateTime.UtcNow;

    private static readonly Timer _timer = new(
        _ => _utcNow = DateTime.UtcNow,
        null,
        TimeSpan.Zero,
        TimeSpan.FromSeconds(1));

    public static DateTime UtcNow => _utcNow;
}