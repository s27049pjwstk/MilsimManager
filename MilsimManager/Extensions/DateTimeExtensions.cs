namespace MilsimManager.Extensions;

public static class DateTimeExtensions {
    public static string TimeRelative(this DateTime date) {
        var span = DateTime.Now - date;
        if (span.TotalSeconds < 0) {
            var future = date - DateTime.Now;
            if (future.TotalHours < 1)
                return $"in {(int)future.TotalMinutes} min";
            return future.TotalDays switch {
                < 2 => $"in {(int)future.TotalHours} h",
                < 60 => $"in {(int)future.TotalDays} days",
                < 730 => $"in {(int)(future.TotalDays / 30)} months",
                _ => $"in {(int)(future.TotalDays / 365)} years"
            };
        }

        if (span.TotalHours < 1)
            return $"{(int)span.TotalMinutes} min ago";
        return span.TotalDays switch {
            < 2 => $"{(int)span.TotalHours} h ago",
            < 60 => $"{(int)span.TotalDays} days ago",
            < 730 => $"{(int)(span.TotalDays / 30)} months ago",
            _ => $"{(int)(span.TotalDays / 365)} years ago"
        };
    }
    private static long ToUnixTimestampSeconds(this DateTime date) => new DateTimeOffset(date).ToUnixTimeSeconds();
    public static string ToDiscordTimestamp(this DateTime date) => $"<t:{date.ToUnixTimestampSeconds().ToString()}>";
}
