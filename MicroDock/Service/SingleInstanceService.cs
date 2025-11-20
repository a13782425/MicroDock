using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace MicroDock.Service
{
    /// <summary>
    /// 单实例管理服务
    /// 使用全局互斥锁和命名管道实现进程间通信
    /// </summary>
    public class SingleInstanceService : IDisposable
    {
        private const string MutexName = "Global\\MicroDock.SingleInstance";
        private const string PipeName = "MicroDock.IPC";
        private const string ShowWindowCommand = "SHOW_WINDOW";
        
        private static Mutex? _singleInstanceMutex;
        private static NamedPipeServerStream? _pipeServer;
        private static CancellationTokenSource? _cancellationTokenSource;
        private static Action? _showWindowCallback;
        private static bool _isDisposed = false;

        /// <summary>
        /// 尝试获取单实例互斥锁
        /// </summary>
        /// <returns>如果成功获取返回 true（表示这是第一个实例），否则返回 false</returns>
        public static bool TryAcquireMutex()
        {
            try
            {
                bool createdNew;
                _singleInstanceMutex = new Mutex(true, MutexName, out createdNew);
                
                if (createdNew)
                {
                    Log.Information("成功获取单实例互斥锁，这是第一个实例");
                    return true;
                }
                else
                {
                    Log.Information("检测到已有实例正在运行");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取单实例互斥锁失败");
                // 发生异常时允许启动，避免因异常导致无法启动应用
                return true;
            }
        }

        /// <summary>
        /// 通知已存在的实例显示窗口
        /// </summary>
        public static void NotifyExistingInstance()
        {
            Task.Run(async () =>
            {
                try
                {
                    Log.Information("尝试通知已存在的实例显示窗口");
                    
                    using (var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                    {
                        // 尝试连接到已存在实例的管道服务器，超时时间 2 秒
                        await pipeClient.ConnectAsync(2000);
                        
                        // 发送显示窗口命令
                        byte[] commandBytes = Encoding.UTF8.GetBytes(ShowWindowCommand);
                        await pipeClient.WriteAsync(commandBytes, 0, commandBytes.Length);
                        await pipeClient.FlushAsync();
                        
                        Log.Information("成功发送显示窗口命令到已存在的实例");
                    }
                }
                catch (TimeoutException)
                {
                    Log.Warning("连接已存在实例超时");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "通知已存在实例失败");
                }
            }).Wait(); // 同步等待完成
        }

        /// <summary>
        /// 启动命名管道服务器，监听显示窗口请求
        /// </summary>
        /// <param name="showWindowCallback">显示窗口的回调函数</param>
        public static void StartPipeServer(Action showWindowCallback)
        {
            if (_isDisposed)
            {
                Log.Warning("SingleInstanceService 已释放，无法启动管道服务器");
                return;
            }

            _showWindowCallback = showWindowCallback;
            _cancellationTokenSource = new CancellationTokenSource();
            
            // 在后台线程中运行管道服务器
            Task.Run(() => RunPipeServer(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            
            Log.Information("命名管道服务器已启动，等待连接");
        }

        /// <summary>
        /// 运行命名管道服务器循环
        /// </summary>
        private static async Task RunPipeServer(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !_isDisposed)
            {
                NamedPipeServerStream? pipeServer = null;
                
                try
                {
                    // 创建命名管道服务器
                    pipeServer = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        1, // 最多 1 个实例
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);
                    
                    _pipeServer = pipeServer;
                    
                    // 等待客户端连接
                    await pipeServer.WaitForConnectionAsync(cancellationToken);
                    
                    if (cancellationToken.IsCancellationRequested || _isDisposed)
                    {
                        break;
                    }
                    
                    Log.Information("收到客户端连接");
                    
                    // 读取客户端发送的命令
                    byte[] buffer = new byte[256];
                    int bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    
                    if (bytesRead > 0)
                    {
                        string command = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Log.Information("收到命令: {Command}", command);
                        
                        if (command == ShowWindowCommand)
                        {
                            // 调用显示窗口回调
                            _showWindowCallback?.Invoke();
                        }
                    }
                    
                    // 断开连接
                    pipeServer.Disconnect();
                }
                catch (OperationCanceledException)
                {
                    Log.Information("命名管道服务器被取消");
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested && !_isDisposed)
                    {
                        Log.Error(ex, "命名管道服务器发生错误");
                        // 短暂延迟后继续
                        await Task.Delay(1000, cancellationToken);
                    }
                }
                finally
                {
                    // 清理管道实例
                    pipeServer?.Dispose();
                }
            }
            
            Log.Information("命名管道服务器已停止");
        }

        /// <summary>
        /// 停止命名管道服务器
        /// </summary>
        public static void StopPipeServer()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _pipeServer?.Dispose();
                _pipeServer = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                
                Log.Information("命名管道服务器已停止");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "停止命名管道服务器时发生错误");
            }
        }

        /// <summary>
        /// 释放单实例互斥锁
        /// </summary>
        public static void ReleaseMutex()
        {
            try
            {
                if (_singleInstanceMutex != null)
                {
                    _singleInstanceMutex.ReleaseMutex();
                    _singleInstanceMutex.Dispose();
                    _singleInstanceMutex = null;
                    
                    Log.Information("单实例互斥锁已释放");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "释放单实例互斥锁时发生错误");
            }
        }

        /// <summary>
        /// 释放所有资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            
            StopPipeServer();
            ReleaseMutex();
            
            GC.SuppressFinalize(this);
        }
    }
}

