using MicroDock.Database;
using ReactiveUI;

namespace MicroDock.Model;

/// <summary>
/// 导航页签配置表中间类
/// </summary>
public class NavigationTabDto : BaseDatabaseDto<NavigationTabDB>
{
    private string _id;
    private int _orderIndex;
    private bool _isVisible;
    private bool _isLocked;
    private string? _passwordHash;

    public NavigationTabDto(NavigationTabDB data) : base(data)
    {
        // 初始化时加载数据
        _id = DBEntity.Id;
        _orderIndex = DBEntity.OrderIndex;
        _isVisible = DBEntity.IsVisible;
        _isLocked = DBEntity.IsLocked;
        _passwordHash = DBEntity.PasswordHash;
    }

    /// <summary>
    /// 唯一ID (主键) - 只读，不允许修改
    /// </summary>
    public string Id
    {
        get => _id;
        private set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    /// <summary>
    /// 排序索引
    /// </summary>
    public int OrderIndex
    {
        get => _orderIndex;
        set
        {
            if (_orderIndex != value)
            {
                this.RaiseAndSetIfChanged(ref _orderIndex, value);
                MarkDirty(); // 标记为脏，1秒后自动写入数据库
            }
        }
    }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                this.RaiseAndSetIfChanged(ref _isVisible, value);
                MarkDirty();
            }
        }
    }

    /// <summary>
    /// 是否启用密码锁定
    /// </summary>
    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (_isLocked != value)
            {
                this.RaiseAndSetIfChanged(ref _isLocked, value);
                MarkDirty();
            }
        }
    }

    /// <summary>
    /// 密码哈希值 (SHA256)
    /// </summary>
    public string? PasswordHash
    {
        get => _passwordHash;
        set
        {
            if (_passwordHash != value)
            {
                this.RaiseAndSetIfChanged(ref _passwordHash, value);
                MarkDirty();
            }
        }
    }

    protected override void SaveToDatabase()
    {
        DBEntity.Id = this.Id;
        DBEntity.OrderIndex = this.OrderIndex;
        DBEntity.IsVisible = this.IsVisible;
        DBEntity.IsLocked = this.IsLocked;
        DBEntity.PasswordHash = this.PasswordHash;
        DBContext.UpdateNavigationTab(DBEntity);
    }
}
