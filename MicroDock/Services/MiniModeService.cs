namespace MicroDock.Services;

public interface IMiniModeService
{
    void Enable();
    void Disable();
    bool IsEnabled { get; }
}

public class MiniModeService : IMiniModeService
{
    private readonly Views.MainWindow _mainWindow;
    private Views.MiniBallWindow? _miniBallWindow;
    
    public MiniModeService(Views.MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public bool IsEnabled { get; private set; }

    public void Enable()
    {
        if (IsEnabled) return;
        
        _mainWindow.Hide();
        
        if (_miniBallWindow == null)
        {
            _miniBallWindow = new Views.MiniBallWindow(this);
            _miniBallWindow.Closed += (s, e) => _miniBallWindow = null;
        }
        
        _miniBallWindow.Show();
        IsEnabled = true;
    }

    public void Disable()
    {
        if (!IsEnabled) return;
        
        _miniBallWindow?.Close();
        _mainWindow.Show();
        IsEnabled = false;
    }
}
