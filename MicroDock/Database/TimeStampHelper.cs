using System;

namespace MicroDock.Database;

/// <summary>
/// 时间戳辅助类，用于处理从2025年1月1日开始的时间戳转换
/// </summary>
internal static class TimeStampHelper
{
    /// <summary>
    /// 基准时间：2025年1月1日 00:00:00（本地时间）
    /// </summary>
    private static readonly DateTime BaseTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);

    /// <summary>
    /// 获取当前时间戳（从基准时间到现在的毫秒数）
    /// </summary>
    /// <returns>毫秒时间戳</returns>
    public static long GetCurrentTimestamp()
    {
        return ToTimestamp(DateTime.Now);
    }

    /// <summary>
    /// 将时间戳转换为 DateTime
    /// </summary>
    /// <param name="timestamp">毫秒时间戳</param>
    /// <returns>DateTime 对象</returns>
    public static DateTime ToDateTime(long timestamp)
    {
        return BaseTime.AddMilliseconds(timestamp);
    }

    /// <summary>
    /// 将 DateTime 转换为时间戳（从基准时间开始的毫秒数）
    /// </summary>
    /// <param name="dateTime">DateTime 对象</param>
    /// <returns>毫秒时间戳</returns>
    public static long ToTimestamp(DateTime dateTime)
    {
        return (long)(dateTime - BaseTime).TotalMilliseconds;
    }
}

