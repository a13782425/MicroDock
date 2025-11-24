using MicroDock.Plugin;
using System.Diagnostics;
using System.Text;

namespace FileToolPlugin
{
    public class FileToolPlugin : BaseMicroDockPlugin
    {
        public override IMicroTab[] Tabs => Array.Empty<IMicroTab>();

        public override object? GetSettingsControl() => null;

        private string GetConsoleAppPath()
        {
            // The console app is copied to the same directory as the plugin dll
            string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string directory = Path.GetDirectoryName(assemblyLocation)!;
            return Path.Combine(directory, "Tools.exe");
        }

        [MicroTool("file.read", Description = "读取一个文件并将其内容以 Base64 字符串形式返回", ReturnDescription = "Base64 编码的文件内容")]
        public async Task<string> ReadFile(
            [ToolParameter("path", Description = "文件的绝对路径")] string path)
        {
            return await RunConsoleCommand("read", path);
        }

        [MicroTool("file.write", Description = "将 Base64 格式的内容写入文件中", ReturnDescription = "成功消息或错误信息")]
        public async Task<string> WriteFile(
            [ToolParameter("path", Description = "文件的绝对路径")] string path,
            [ToolParameter("content", Description = "Base64 编码的文件内容")] string content)
        {
            return await RunConsoleCommand("write", path, content);
        }

        private async Task<string> RunConsoleCommand(string command, params string[] args)
        {
            string exePath = GetConsoleAppPath();
            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException($"Console app not found at {exePath}");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            // Add command
            startInfo.ArgumentList.Add(command);
            // Add args
            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
            {
                // If there is error output, prefer returning that.
                if (!string.IsNullOrEmpty(error))
                {
                     throw new Exception($"Tool execution failed: {error}");
                }
                 throw new Exception($"Tool execution failed with exit code {process.ExitCode}");
            }

            return output;
        }
    }
}
