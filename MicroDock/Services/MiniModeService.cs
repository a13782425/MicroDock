using MicroDock.Infrastructure;

namespace MicroDock.Services;

public interface IMiniModeService
{
    void Enable();
    void Disable();
    bool IsEnabled { get; }
}

/// <summary>
/// 迷你模式服务，管理悬浮球窗口的生命周期
/// 悬浮球窗口内部会管理自己的功能栏窗口
/// </summary>
public class MiniModeService : IMiniModeService
{
    private Views.MiniBallWindow? _miniBallWindow;
    
    public MiniModeService()
    {
    }

    public bool IsEnabled { get; private set; }

    public void Enable()
    {
        if (IsEnabled) return;
        
        // 通过事件通知隐藏主窗口
        EventAggregator.Instance.Publish(new WindowHideRequestMessage("MainWindow"));
        
        if (_miniBallWindow == null)
        {
            _miniBallWindow = new Views.MiniBallWindow();
            _miniBallWindow.Closed += (s, e) => 
            {
                // 悬浮球窗口关闭时会自动关闭功能栏窗口（在 MiniBallWindow 中处理）
                _miniBallWindow = null;
                IsEnabled = false;
                // 通知服务状态变更
                EventAggregator.Instance.Publish(new ServiceStateChangedMessage("MiniMode", false));
            };
        }
        
        _miniBallWindow.Show();
        IsEnabled = true;
        
        // 通知服务状态变更
        EventAggregator.Instance.Publish(new ServiceStateChangedMessage("MiniMode", true));
    }

    public void Disable()
    {
        if (!IsEnabled) return;
        
        IsEnabled = false;
        // 关闭悬浮球窗口（会自动关闭功能栏窗口）
        _miniBallWindow?.Close();
        _miniBallWindow = null;
        
        // 通知服务状态变更
        EventAggregator.Instance.Publish(new ServiceStateChangedMessage("MiniMode", false));

        // 通过事件通知显示主窗口
        EventAggregator.Instance.Publish(new WindowShowRequestMessage("MainWindow"));
        
    }
}
