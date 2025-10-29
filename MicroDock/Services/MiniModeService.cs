using MicroDock.Infrastructure;

namespace MicroDock.Services;

public interface IMiniModeService
{
    void Enable();
    void Disable();
    bool IsEnabled { get; }
}

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
        
        _miniBallWindow?.Close();
        _miniBallWindow = null;
        
        // 通过事件通知显示主窗口
        EventAggregator.Instance.Publish(new WindowShowRequestMessage("MainWindow"));
        
        IsEnabled = false;
        
        // 通知服务状态变更
        EventAggregator.Instance.Publish(new ServiceStateChangedMessage("MiniMode", false));
    }
}
