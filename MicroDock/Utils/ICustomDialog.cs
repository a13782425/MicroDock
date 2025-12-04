namespace MicroDock.Utils;

/// <summary>
/// 自定义对话框接口，用于与 ShowCustomDialogAsync 配合使用
/// </summary>
/// <typeparam name="TResult">对话框返回的结果类型</typeparam>
public interface ICustomDialog<TResult>
{
    /// <summary>
    /// 验证对话框输入是否有效
    /// </summary>
    bool Validate();

    /// <summary>
    /// 获取对话框结果(无论成功失败)
    /// </summary>
    TResult GetResult();
}
