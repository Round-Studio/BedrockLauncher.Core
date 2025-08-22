using System;

public static class TimeBasedVersion
{
    /// <summary>
    /// 生成基于当前时间的版本号，格式如：2025.4.5.123
    /// 最后一部分是当天已过去的分钟数（00:00 起）
    /// </summary>
    public static string GetVersion()
    {
        DateTime now = DateTime.Now;

        int year = now.Year;
        int month = now.Month;      // 1-12，不补零
        int day = now.Day;          // 1-31，不补零
        int totalMinutes = now.Hour * 60 + now.Minute;  // 从 00:00 起的分钟数

        return $"{year}.{month}.{day}.{totalMinutes}";
    }

    // 可选：返回 .NET 的 Version 对象（注意范围限制）
    public static Version GetVersionObject()
    {
        DateTime now = DateTime.Now;
        int year = now.Year;
        int month = now.Month;
        int day = now.Day;
        int totalMinutes = now.Hour * 60 + now.Minute;

        // Version 要求每个部分在 0-65535 之间，这里完全安全
        return new Version(year, month, day, totalMinutes);
    }
}