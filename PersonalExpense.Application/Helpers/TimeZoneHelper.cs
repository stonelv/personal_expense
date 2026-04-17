using System.Collections.ObjectModel;

namespace PersonalExpense.Application.Helpers;

public static class TimeZoneHelper
{
    private static readonly ReadOnlyDictionary<string, string> IanaToWindowsMap = new ReadOnlyDictionary<string, string>(
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Asia/Shanghai", "China Standard Time" },
            { "Asia/Chongqing", "China Standard Time" },
            { "Asia/Harbin", "China Standard Time" },
            { "Asia/Kashgar", "China Standard Time" },
            { "Asia/Urumqi", "China Standard Time" },
            { "Asia/Hong_Kong", "Hong Kong Standard Time" },
            { "Asia/Macau", "China Standard Time" },
            { "Asia/Taipei", "Taipei Standard Time" },
            { "Asia/Tokyo", "Tokyo Standard Time" },
            { "Asia/Seoul", "Korea Standard Time" },
            { "Asia/Singapore", "Singapore Standard Time" },
            { "America/New_York", "Eastern Standard Time" },
            { "America/Chicago", "Central Standard Time" },
            { "America/Denver", "Mountain Standard Time" },
            { "America/Los_Angeles", "Pacific Standard Time" },
            { "America/Phoenix", "US Mountain Standard Time" },
            { "America/Anchorage", "Alaskan Standard Time" },
            { "Pacific/Honolulu", "Hawaiian Standard Time" },
            { "Europe/London", "GMT Standard Time" },
            { "Europe/Paris", "Romance Standard Time" },
            { "Europe/Berlin", "W. Europe Standard Time" },
            { "Europe/Madrid", "Romance Standard Time" },
            { "Europe/Rome", "W. Europe Standard Time" },
            { "Europe/Amsterdam", "W. Europe Standard Time" },
            { "Europe/Brussels", "Romance Standard Time" },
            { "Europe/Vienna", "W. Europe Standard Time" },
            { "Europe/Warsaw", "Central European Standard Time" },
            { "Europe/Prague", "Central Europe Standard Time" },
            { "Europe/Budapest", "Central Europe Standard Time" },
            { "Europe/Moscow", "Russian Standard Time" },
            { "Europe/Istanbul", "Turkey Standard Time" },
            { "Africa/Cairo", "Egypt Standard Time" },
            { "Africa/Johannesburg", "South Africa Standard Time" },
            { "Asia/Dubai", "Arabian Standard Time" },
            { "Asia/Kolkata", "India Standard Time" },
            { "Asia/Bangkok", "SE Asia Standard Time" },
            { "Asia/Jakarta", "SE Asia Standard Time" },
            { "Australia/Sydney", "AUS Eastern Standard Time" },
            { "Australia/Melbourne", "AUS Eastern Standard Time" },
            { "Australia/Perth", "W. Australia Standard Time" },
            { "Australia/Brisbane", "E. Australia Standard Time" },
            { "Australia/Adelaide", "Cen. Australia Standard Time" },
            { "Pacific/Auckland", "New Zealand Standard Time" },
            { "America/Sao_Paulo", "E. South America Standard Time" },
            { "America/Buenos_Aires", "Argentina Standard Time" },
            { "America/Mexico_City", "Central Standard Time (Mexico)" },
            { "Pacific/Apia", "Samoa Standard Time" }
        }
    );

    private static readonly Lazy<ReadOnlyDictionary<string, string>> WindowsToIanaMapLazy = new(() =>
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in IanaToWindowsMap)
        {
            if (!dict.ContainsKey(kvp.Value))
            {
                dict[kvp.Value] = kvp.Key;
            }
        }
        return new ReadOnlyDictionary<string, string>(dict);
    });

    private static ReadOnlyDictionary<string, string> WindowsToIanaMap => WindowsToIanaMapLazy.Value;

    public static TimeZoneInfo? FindTimeZoneById(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            if (IanaToWindowsMap.TryGetValue(timeZoneId, out var windowsId))
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                }
                catch (TimeZoneNotFoundException)
                {
                }
            }

            if (WindowsToIanaMap.TryGetValue(timeZoneId, out var ianaId))
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(ianaId);
                }
                catch (TimeZoneNotFoundException)
                {
                }
            }

            return null;
        }
    }

    public static bool TryFindTimeZoneById(string timeZoneId, out TimeZoneInfo timeZone)
    {
        timeZone = FindTimeZoneById(timeZoneId);
        return timeZone != null;
    }

    public static string? ConvertToWindowsTimeZoneId(string ianaTimeZoneId)
    {
        if (string.IsNullOrWhiteSpace(ianaTimeZoneId))
        {
            return null;
        }

        return IanaToWindowsMap.TryGetValue(ianaTimeZoneId, out var windowsId) ? windowsId : null;
    }

    public static string? ConvertToIanaTimeZoneId(string windowsTimeZoneId)
    {
        if (string.IsNullOrWhiteSpace(windowsTimeZoneId))
        {
            return null;
        }

        return WindowsToIanaMap.TryGetValue(windowsTimeZoneId, out var ianaId) ? ianaId : null;
    }

    public static TimeZoneInfo GetUtcTimeZone()
    {
        return TimeZoneInfo.Utc;
    }

    public static TimeZoneInfo GetSafeTimeZone(string? timeZoneId, TimeZoneInfo? defaultTimeZone = null)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return defaultTimeZone ?? TimeZoneInfo.Utc;
        }

        var timeZone = FindTimeZoneById(timeZoneId);
        return timeZone ?? defaultTimeZone ?? TimeZoneInfo.Utc;
    }

    public static DateTime ConvertToUtc(DateTime dateTime, TimeZoneInfo sourceTimeZone)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
        {
            return dateTime;
        }

        var unspecified = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, sourceTimeZone);
    }

    public static DateTime ConvertFromUtc(DateTime dateTime, TimeZoneInfo targetTimeZone)
    {
        if (dateTime.Kind != DateTimeKind.Utc)
        {
            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeFromUtc(dateTime, targetTimeZone);
    }

    public static DateTime GetMonthStartInUtc(int year, int month, TimeZoneInfo timeZone)
    {
        var localStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return ConvertToUtc(localStart, timeZone);
    }

    public static DateTime GetMonthEndInUtc(int year, int month, TimeZoneInfo timeZone)
    {
        var localEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Unspecified);
        return ConvertToUtc(localEnd, timeZone);
    }
}
