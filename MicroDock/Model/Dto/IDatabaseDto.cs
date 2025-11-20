using MicroDock.Database;
using MicroDock.Service;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroDock.Model;

public interface IDatabaseDto
{
    void Save();
}

public abstract class BaseDatabaseDto<T> : ReactiveObject, IDatabaseDto where T : class, IDatabase
{
    private bool _isDirty;
    // 假设你有 ServiceLocator，否则请通过构造函数注入或单例访问
    protected static DelayStorageService DelayStorage => ServiceLocator.Get<DelayStorageService>();

    public T DBEntity { get; protected set; }

    public BaseDatabaseDto(T data)
    {
        DBEntity = data;
    }
    /// <summary>
    /// 标记对象为脏，并加入保存队列
    /// </summary>
    protected void MarkDirty()
    {
        if (!_isDirty)
        {
            _isDirty = true;
            DelayStorage.RegisterDirtyObject(this);
        }
    }
    public void Save()
    {
        if (!_isDirty)
            return;
        SaveToDatabase();
        _isDirty = false;
    }
    /// <summary>
    /// 子类实现具体的数据库更新逻辑
    /// </summary>
    protected abstract void SaveToDatabase();
}
