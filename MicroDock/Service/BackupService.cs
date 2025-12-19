using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroDock.Service;

/// <summary>
/// 备份管理
/// </summary>
[AutoRegister(int.MinValue + 1)]
public class BackupService : IMicroService
{
    /// <summary>
    /// 待恢复数据库文件路径
    /// </summary>
    private static string PendingRestoreDbPath => Path.Combine(AppConfig.TEMP_BACKUP_FOLDER, "pending_restore_microdock");

    Task IMicroService.OnRegistered()
    {
        try
        {
            if (!File.Exists(PendingRestoreDbPath))
            {
                return Task.CompletedTask;
            }

            string dbPath = Path.Combine(AppConfig.CONFIG_FOLDER, "microdock");
            string backupPath = dbPath + "_backup_" + DateTime.Now.ToString("yyyyMMddHHmmss");

            LogInformation($"检测到待恢复的数据库，开始恢复", DEFAULT_LOG_TAG);

            // 备份当前数据库
            if (File.Exists(dbPath))
            {
                File.Copy(dbPath, backupPath, true);
            }

            try
            {
                // 用待恢复文件替换当前数据库
                File.Copy(PendingRestoreDbPath, dbPath, true);

                // 删除待恢复文件
                File.Delete(PendingRestoreDbPath);

                // 删除旧备份
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                LogInformation("主程序数据恢复成功", DEFAULT_LOG_TAG);
            }
            catch
            {
                // 恢复失败，还原原数据
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, dbPath, true);
                    File.Delete(backupPath);
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            LogError("应用待恢复数据失败", DEFAULT_LOG_TAG, ex);
        }
        return Task.CompletedTask;
    }
}
