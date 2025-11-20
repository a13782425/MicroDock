using Avalonia;
using System;

namespace MicroDock.Service;

/// <summary>
/// 窗口位置计算工具类，提供DPI安全的位置和尺寸计算
/// </summary>
public static class WindowPositionCalculator
{
    /// <summary>
    /// 计算窗口的屏幕像素中心点
    /// </summary>
    /// <param name="position">窗口位置（像素坐标）</param>
    /// <param name="widthDip">窗口宽度（DIP）</param>
    /// <param name="heightDip">窗口高度（DIP）</param>
    /// <param name="dpiScale">DPI 缩放比例</param>
    /// <returns>中心点的像素坐标</returns>
    public static PixelPoint CalculateCenter(PixelPoint position, double widthDip, double heightDip, double dpiScale)
    {
        if (dpiScale <= 0) dpiScale = 1;

        // 使用浮点数计算避免中间舍入误差
        double widthPx = widthDip * dpiScale;
        double heightPx = heightDip * dpiScale;

        // 计算中心点（使用浮点数确保精度）
        double centerX = position.X + widthPx * 0.5;
        double centerY = position.Y + heightPx * 0.5;

        // 最后再舍入到最接近的整数像素
        return new PixelPoint(
            (int)Math.Round(centerX),
            (int)Math.Round(centerY)
        );
    }

    /// <summary>
    /// 计算围绕指定中心点的新窗口位置
    /// </summary>
    /// <param name="centerPx">中心点（像素坐标）</param>
    /// <param name="newWidthDip">新窗口宽度（DIP）</param>
    /// <param name="newHeightDip">新窗口高度（DIP）</param>
    /// <param name="dpiScale">DPI 缩放比例</param>
    /// <returns>新窗口的左上角位置（像素坐标）</returns>
    public static PixelPoint CalculatePositionAroundCenter(
        PixelPoint centerPx,
        double newWidthDip,
        double newHeightDip,
        double dpiScale)
    {
        if (dpiScale <= 0) dpiScale = 1;

        // 使用浮点数计算避免舍入误差
        double newWidthPx = newWidthDip * dpiScale;
        double newHeightPx = newHeightDip * dpiScale;

        // 计算新位置（中心点 - 半尺寸）
        double newX = centerPx.X - newWidthPx / 2.0;
        double newY = centerPx.Y - newHeightPx / 2.0;

        // 舍入到整数像素
        return new PixelPoint(
            (int)Math.Round(newX),
            (int)Math.Round(newY)
        );
    }

    /// <summary>
    /// 计算保持中心不变的新窗口位置
    /// </summary>
    /// <param name="currentPosition">当前窗口位置（像素坐标）</param>
    /// <param name="currentWidthDip">当前窗口宽度（DIP）</param>
    /// <param name="currentHeightDip">当前窗口高度（DIP）</param>
    /// <param name="newWidthDip">新窗口宽度（DIP）</param>
    /// <param name="newHeightDip">新窗口高度（DIP）</param>
    /// <param name="dpiScale">DPI 缩放比例</param>
    /// <returns>新窗口的位置（像素坐标）</returns>
    public static PixelPoint CalculateNewPosition(
        PixelPoint currentPosition,
        double currentWidthDip,
        double currentHeightDip,
        double newWidthDip,
        double newHeightDip,
        double dpiScale)
    {
        // 先计算当前中心点
        PixelPoint center = CalculateCenter(currentPosition, currentWidthDip, currentHeightDip, dpiScale);

        // 然后基于中心点计算新位置
        return CalculatePositionAroundCenter(center, newWidthDip, newHeightDip, dpiScale);
    }

    /// <summary>
    /// 判断窗口是否靠近屏幕边缘
    /// </summary>
    /// <param name="windowPosition">窗口位置（像素坐标）</param>
    /// <param name="windowWidthDip">窗口宽度（DIP）</param>
    /// <param name="windowHeightDip">窗口高度（DIP）</param>
    /// <param name="workingArea">工作区矩形（像素坐标）</param>
    /// <param name="marginPx">边缘阈值（像素）</param>
    /// <param name="dpiScale">DPI 缩放比例</param>
    /// <returns>边缘方向标志 (nearLeft, nearRight, nearTop, nearBottom)</returns>
    public static (bool nearLeft, bool nearRight, bool nearTop, bool nearBottom) CheckEdgeProximity(
        PixelPoint windowPosition,
        double windowWidthDip,
        double windowHeightDip,
        PixelRect workingArea,
        int marginPx,
        double dpiScale)
    {
        if (dpiScale <= 0) dpiScale = 1;

        int windowWidthPx = (int)Math.Round(windowWidthDip * dpiScale);
        int windowHeightPx = (int)Math.Round(windowHeightDip * dpiScale);

        bool nearLeft = windowPosition.X <= workingArea.X + marginPx;
        bool nearRight = windowPosition.X + windowWidthPx >= workingArea.Right - marginPx;
        bool nearTop = windowPosition.Y <= workingArea.Y + marginPx;
        bool nearBottom = windowPosition.Y + windowHeightPx >= workingArea.Bottom - marginPx;

        return (nearLeft, nearRight, nearTop, nearBottom);
    }

    /// <summary>
    /// 根据边缘位置计算最佳弧线方向
    /// </summary>
    /// <param name="nearLeft">是否靠近左边缘</param>
    /// <param name="nearRight">是否靠近右边缘</param>
    /// <param name="nearTop">是否靠近顶部边缘</param>
    /// <param name="nearBottom">是否靠近底部边缘</param>
    /// <param name="defaultStartAngle">默认起始角度</param>
    /// <param name="defaultSweepAngle">默认扫描角度</param>
    /// <returns>调整后的起始角度和扫描角度</returns>
    public static (double startAngle, double sweepAngle) CalculateOptimalArc(
        bool nearLeft,
        bool nearRight,
        bool nearTop,
        bool nearBottom,
        double defaultStartAngle,
        double defaultSweepAngle)
    {
        // 根据边缘位置选择最佳方向
        if (nearLeft && !nearRight)
        {
            // 右半环（-90..90度）
            return (-90, 180);
        }
        else if (nearRight && !nearLeft)
        {
            // 左半环（90..270度）
            return (90, 180);
        }
        else if (nearTop && !nearBottom)
        {
            // 下半环（0..180度）
            return (0, 180);
        }
        else if (nearBottom && !nearTop)
        {
            // 上半环（180..360度）
            return (180, 180);
        }
        else
        {
            // 中间或角落位置，使用默认值
            return (defaultStartAngle, defaultSweepAngle);
        }
    }
}

