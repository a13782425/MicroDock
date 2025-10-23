using Avalonia;
using MicroDock.Services;
using Xunit;

namespace MicroDock.Tests;

public class WindowPositionCalculatorTests
{
    [Fact]
    public void CalculateCenter_ReturnsCorrectCenter_ForStandardDPI()
    {
        // Arrange
        PixelPoint position = new PixelPoint(100, 100);
        double widthDip = 64;
        double heightDip = 64;
        double dpiScale = 1.0;

        // Act
        PixelPoint center = WindowPositionCalculator.CalculateCenter(position, widthDip, heightDip, dpiScale);

        // Assert
        Assert.Equal(132, center.X); // 100 + 64/2
        Assert.Equal(132, center.Y); // 100 + 64/2
    }

    [Fact]
    public void CalculateCenter_ReturnsCorrectCenter_ForHighDPI()
    {
        // Arrange
        PixelPoint position = new PixelPoint(200, 200);
        double widthDip = 64;
        double heightDip = 64;
        double dpiScale = 1.5; // 150% DPI

        // Act
        PixelPoint center = WindowPositionCalculator.CalculateCenter(position, widthDip, heightDip, dpiScale);

        // Assert
        // 64 * 1.5 = 96 像素
        // 中心: 200 + 96/2 = 200 + 48 = 248
        Assert.Equal(248, center.X);
        Assert.Equal(248, center.Y);
    }

    [Fact]
    public void CalculateCenter_HandlesOddSizes()
    {
        // Arrange
        PixelPoint position = new PixelPoint(0, 0);
        double widthDip = 65; // 奇数
        double heightDip = 65;
        double dpiScale = 1.0;

        // Act
        PixelPoint center = WindowPositionCalculator.CalculateCenter(position, widthDip, heightDip, dpiScale);

        // Assert
        // Math.Round(32.5) = 32 (Banker's Rounding: 四舍六入五取偶)
        Assert.Equal(32, center.X);
        Assert.Equal(32, center.Y);
    }

    [Fact]
    public void CalculatePositionAroundCenter_ReturnsCorrectPosition()
    {
        // Arrange
        PixelPoint center = new PixelPoint(300, 300);
        double newWidthDip = 100;
        double newHeightDip = 100;
        double dpiScale = 1.0;

        // Act
        PixelPoint newPosition = WindowPositionCalculator.CalculatePositionAroundCenter(
            center, newWidthDip, newHeightDip, dpiScale);

        // Assert
        // 300 - 100/2 = 250
        Assert.Equal(250, newPosition.X);
        Assert.Equal(250, newPosition.Y);
    }

    [Fact]
    public void CalculateNewPosition_MaintainsCenter()
    {
        // Arrange
        PixelPoint currentPosition = new PixelPoint(100, 100);
        double currentWidthDip = 64;
        double currentHeightDip = 64;
        double newWidthDip = 200;
        double newHeightDip = 200;
        double dpiScale = 1.0;

        // Act
        PixelPoint originalCenter = WindowPositionCalculator.CalculateCenter(
            currentPosition, currentWidthDip, currentHeightDip, dpiScale);
        
        PixelPoint newPosition = WindowPositionCalculator.CalculateNewPosition(
            currentPosition, currentWidthDip, currentHeightDip,
            newWidthDip, newHeightDip, dpiScale);
        
        PixelPoint newCenter = WindowPositionCalculator.CalculateCenter(
            newPosition, newWidthDip, newHeightDip, dpiScale);

        // Assert: 中心点应该保持不变
        Assert.Equal(originalCenter.X, newCenter.X);
        Assert.Equal(originalCenter.Y, newCenter.Y);
    }

    [Fact]
    public void CalculateNewPosition_MaintainsCenter_WithHighDPI()
    {
        // Arrange
        PixelPoint currentPosition = new PixelPoint(200, 200);
        double currentWidthDip = 64;
        double currentHeightDip = 64;
        double newWidthDip = 180;
        double newHeightDip = 180;
        double dpiScale = 1.25; // 125% DPI

        // Act
        PixelPoint originalCenter = WindowPositionCalculator.CalculateCenter(
            currentPosition, currentWidthDip, currentHeightDip, dpiScale);
        
        PixelPoint newPosition = WindowPositionCalculator.CalculateNewPosition(
            currentPosition, currentWidthDip, currentHeightDip,
            newWidthDip, newHeightDip, dpiScale);
        
        PixelPoint newCenter = WindowPositionCalculator.CalculateCenter(
            newPosition, newWidthDip, newHeightDip, dpiScale);

        // Assert: 中心点应该保持不变（允许1像素误差，因为舍入）
        Assert.True(Math.Abs(originalCenter.X - newCenter.X) <= 1);
        Assert.True(Math.Abs(originalCenter.Y - newCenter.Y) <= 1);
    }

    [Fact]
    public void CheckEdgeProximity_DetectsLeftEdge()
    {
        // Arrange
        PixelPoint windowPosition = new PixelPoint(10, 300);
        double windowWidthDip = 64;
        double windowHeightDip = 64;
        PixelRect workingArea = new PixelRect(0, 0, 1920, 1080);
        int marginPx = 48;
        double dpiScale = 1.0;

        // Act
        (bool nearLeft, bool nearRight, bool nearTop, bool nearBottom) = 
            WindowPositionCalculator.CheckEdgeProximity(
                windowPosition, windowWidthDip, windowHeightDip,
                workingArea, marginPx, dpiScale);

        // Assert
        Assert.True(nearLeft);
        Assert.False(nearRight);
        Assert.False(nearTop);
        Assert.False(nearBottom);
    }

    [Fact]
    public void CheckEdgeProximity_DetectsRightEdge()
    {
        // Arrange
        PixelPoint windowPosition = new PixelPoint(1850, 300);
        double windowWidthDip = 64;
        double windowHeightDip = 64;
        PixelRect workingArea = new PixelRect(0, 0, 1920, 1080);
        int marginPx = 48;
        double dpiScale = 1.0;

        // Act
        (bool nearLeft, bool nearRight, bool nearTop, bool nearBottom) = 
            WindowPositionCalculator.CheckEdgeProximity(
                windowPosition, windowWidthDip, windowHeightDip,
                workingArea, marginPx, dpiScale);

        // Assert
        Assert.False(nearLeft);
        Assert.True(nearRight);
        Assert.False(nearTop);
        Assert.False(nearBottom);
    }

    [Fact]
    public void CalculateOptimalArc_ReturnsRightArcForLeftEdge()
    {
        // Act
        (double startAngle, double sweepAngle) = 
            WindowPositionCalculator.CalculateOptimalArc(
                nearLeft: true, nearRight: false, nearTop: false, nearBottom: false,
                defaultStartAngle: -90, defaultSweepAngle: 360);

        // Assert
        Assert.Equal(-90, startAngle);
        Assert.Equal(180, sweepAngle);
    }

    [Fact]
    public void CalculateOptimalArc_ReturnsLeftArcForRightEdge()
    {
        // Act
        (double startAngle, double sweepAngle) = 
            WindowPositionCalculator.CalculateOptimalArc(
                nearLeft: false, nearRight: true, nearTop: false, nearBottom: false,
                defaultStartAngle: -90, defaultSweepAngle: 360);

        // Assert
        Assert.Equal(90, startAngle);
        Assert.Equal(180, sweepAngle);
    }

    [Fact]
    public void CalculateOptimalArc_ReturnsDefaultForCenter()
    {
        // Act
        (double startAngle, double sweepAngle) = 
            WindowPositionCalculator.CalculateOptimalArc(
                nearLeft: false, nearRight: false, nearTop: false, nearBottom: false,
                defaultStartAngle: -90, defaultSweepAngle: 360);

        // Assert
        Assert.Equal(-90, startAngle);
        Assert.Equal(360, sweepAngle);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public void MultipleResizeOperations_DoNotAccumulateError(double dpiScale)
    {
        // Arrange: 模拟多次展开收起操作
        PixelPoint initialPosition = new PixelPoint(500, 500);
        double ballSize = 64;
        double expandedSize = 200;

        PixelPoint currentPosition = initialPosition;

        // Act: 执行10次展开-收起循环
        for (int i = 0; i < 10; i++)
        {
            // 展开
            currentPosition = WindowPositionCalculator.CalculateNewPosition(
                currentPosition, ballSize, ballSize,
                expandedSize, expandedSize, dpiScale);

            // 收起
            currentPosition = WindowPositionCalculator.CalculateNewPosition(
                currentPosition, expandedSize, expandedSize,
                ballSize, ballSize, dpiScale);
        }

        // Assert: 最终位置应该与初始位置非常接近（允许1像素误差）
        Assert.True(Math.Abs(currentPosition.X - initialPosition.X) <= 1,
            $"X position drifted: expected ~{initialPosition.X}, got {currentPosition.X}");
        Assert.True(Math.Abs(currentPosition.Y - initialPosition.Y) <= 1,
            $"Y position drifted: expected ~{initialPosition.Y}, got {currentPosition.Y}");
    }
}

