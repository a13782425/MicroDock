namespace MicroDock.Plugin;
/// <summary>
/// 插件工具类
/// </summary>
// public static class MicroPluginUtils
// {
//     public static string GetConfigPath(IMicroDockPlugin plugin)
//     {
//         if (plugin == null)
//             throw new ArgumentNullException("插件不能为空");
//         if (string.IsNullOrWhiteSpace(plugin.UniqueName))
//             throw new ArgumentException("插件名为空");
//         return "";
//     }
// }
interface IMicroPluginUtils
{
    string GetConfigPath();
}