using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using UnityProjectPlugin.Models;

namespace UnityProjectPlugin.ViewModels
{
    /// <summary>
    /// Unity 版本管理设置 ViewModel
    /// </summary>
    public class UnityVersionSettingsViewModel
    {
        private readonly UnityProjectPlugin _plugin;

        public UnityVersionSettingsViewModel(UnityProjectPlugin plugin)
        {
            _plugin = plugin;
            Versions = new ObservableCollection<UnityVersion>();

            // 初始化命令
            AddVersionCommand = new RelayCommand(AddVersion);
            DeleteVersionCommand = new RelayCommand<UnityVersion>(DeleteVersion);

            // 加载版本
            Refresh();
        }

        /// <summary>
        /// 版本列表
        /// </summary>
        public ObservableCollection<UnityVersion> Versions { get; }

        /// <summary>
        /// 添加版本命令
        /// </summary>
        public ICommand AddVersionCommand { get; }

        /// <summary>
        /// 删除版本命令
        /// </summary>
        public ICommand DeleteVersionCommand { get; }

        /// <summary>
        /// 刷新版本列表
        /// </summary>
        public void Refresh()
        {
            Versions.Clear();
            var versions = _plugin.GetVersions();
            
            foreach (var version in versions)
            {
                Versions.Add(version);
            }
        }

        /// <summary>
        /// 添加版本
        /// </summary>
        private void AddVersion()
        {
            // 这个方法需要在 View 层处理输入对话框
            // 通过事件或委托通知 View 层
            AddVersionRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 删除版本
        /// </summary>
        private void DeleteVersion(UnityVersion? version)
        {
            if (version == null) return;

            try
            {
                _plugin.RemoveVersion(version.Version);
                Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除版本失败: {version.Version} - {ex.Message}");
            }
        }

        /// <summary>
        /// 添加版本请求事件
        /// </summary>
        public event EventHandler? AddVersionRequested;

        /// <summary>
        /// 简单的命令实现
        /// </summary>
        private class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool>? _canExecute;

            public RelayCommand(Action execute, Func<bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

            public void Execute(object? parameter) => _execute();
        }

        private class RelayCommand<T> : ICommand
        {
            private readonly Action<T?> _execute;
            private readonly Func<T?, bool>? _canExecute;

            public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

            public void Execute(object? parameter) => _execute((T?)parameter);
        }
    }
}

