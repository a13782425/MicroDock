using System.Diagnostics;

namespace MicroDock.Restarter;

/// <summary>
/// MicroDock 重启助手程序
/// 用法: Restarter.exe <程序路径> <进程ID> [重启原因]
/// </summary>
internal class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("用法: Restarter.exe <程序路径> <进程ID> [重启原因]");
            return 1;
        }

        string targetPath = args[0];
        if (!int.TryParse(args[1], out int processId))
        {
            Console.WriteLine("无效的进程ID");
            return 1;
        }

        string? reason = args.Length > 2 ? args[2] : null;

        try
        {
            // 等待原进程退出
            // 等待原进程退出（每秒检查一次，最多等待 10 秒）
            try
            {
                var process = Process.GetProcessById(processId);
                Console.WriteLine($"等待进程 {processId} 退出...");

                int waitedSeconds = 0;
                const int maxWaitSeconds = 10;

                while (!process.HasExited && waitedSeconds < maxWaitSeconds)
                {
                    Thread.Sleep(1000);
                    waitedSeconds++;
                    Console.WriteLine($"已等待 {waitedSeconds} 秒...");
                }

                // 超时仍未退出，强制终止
                if (!process.HasExited)
                {
                    Console.WriteLine("进程未能正常退出，强制终止...");
                    process.Kill();
                    process.WaitForExit(3000); // 等待 Kill 生效
                }
                if (!process.HasExited)
                {
                    Console.WriteLine("进程未能正常退出, 重启失败");
                    return 1;
                }

                Console.WriteLine("原进程已退出");
            }
            catch (ArgumentException)
            {
                // 进程 ID 不存在，说明已退出
                Console.WriteLine("原进程已退出");
            }
            catch (InvalidOperationException)
            {
                // 进程在检查过程中退出了
                Console.WriteLine("原进程已退出");
            }

            // 短暂延迟确保资源完全释放（如文件句柄、互斥锁等）
            Thread.Sleep(500);

            // 构建启动参数
            var startInfo = new ProcessStartInfo
            {
                FileName = targetPath,
                UseShellExecute = true
            };

            // 如果有重启原因，传递给新实例
            if (!string.IsNullOrEmpty(reason))
            {
                startInfo.Arguments = $"--restart-reason={reason}";
            }

            // 启动新实例
            Console.WriteLine($"启动: {targetPath}");
            Process.Start(startInfo);

            Console.WriteLine("重启完成");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"重启失败: {ex.Message}");
            return 1;
        }
    }
}
