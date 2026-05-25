using System;

public class TimeManager
{
    public static DateTime GetCurrentTime()
    {
        return DateTime.Now;
    }
    public static double GetSecondsBetween(DateTime oldTime, DateTime newTime)
    {
        return (newTime - oldTime).TotalSeconds;
    }
    public static string Format(TimeSpan time)
    {
        if (time.TotalSeconds <= 0) return "00m00s";

        if (time.TotalDays >= 1)
            return $"{(int)time.TotalDays}d{time.Hours:D2}h";

        if (time.TotalHours >= 1)
            return $"{time.Hours:D2}h{time.Minutes:D2}m";

        return $"{time.Minutes:D2}m{time.Seconds:D2}s";
    }
}