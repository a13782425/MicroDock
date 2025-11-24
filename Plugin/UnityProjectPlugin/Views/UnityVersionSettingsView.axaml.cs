using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityProjectPlugin.ViewModels;

namespace UnityProjectPlugin.Views
{
    /// <summary>
    /// Unity 版本管理设置视图
    /// </summary>
    public partial class UnityVersionSettingsView : UserControl
    {
        private readonly UnityProjectPlugin _plugin;
        private readonly UnityVersionSettingsViewModel _viewModel;

        private Button? _addVersionButton;
        private ItemsControl? _versionsItemsControl;

        public UnityVersionSettingsView(UnityProjectPlugin plugin)
        {
            _plugin = plugin;
            _viewModel = new UnityVersionSettingsViewModel(plugin);

            InitializeComponent();
            InitializeControls();
            AttachEventHandlers();

            _viewModel.AddVersionRequested += OnAddVersionRequested;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _addVersionButton = this.FindControl<Button>("AddVersionButton");
            _versionsItemsControl = this.FindControl<ItemsControl>("VersionsItemsControl");

            if (_versionsItemsControl != null)
            {
                _versionsItemsControl.ItemsSource = _viewModel.Versions;
            }
        }

        private void AttachEventHandlers()
        {
            if (_addVersionButton != null)
            {
                _addVersionButton.Click += OnAddVersionClick;
            }

            if (_versionsItemsControl != null)
            {
                // 为每个删除按钮附加事件处理器
                _versionsItemsControl.Loaded += (s, e) =>
                {
                    AttachDeleteButtonHandlers();
                };
            }
        }

        private void AttachDeleteButtonHandlers()
        {
            if (_versionsItemsControl == null) return;

            // 查找所有删除按钮并附加事件
            var buttons = _versionsItemsControl.GetVisualDescendants()
                .OfType<Button>()
                .Where(b => b.Content?.ToString() == "删除");

            foreach (var button in buttons)
            {
                button.Click -= OnDeleteVersionClick; // 避免重复附加
                button.Click += OnDeleteVersionClick;
            }
        }

        private async void OnAddVersionClick(object? sender, RoutedEventArgs e)
        {
            await AddVersionAsync();
        }

        private void OnAddVersionRequested(object? sender, EventArgs e)
        {
            _ = AddVersionAsync();
        }

        private async System.Threading.Tasks.Task AddVersionAsync()
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                // 选择 Unity.exe 文件
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "选择 Unity.exe",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Unity Editor")
                        {
                            Patterns = new[] { "Unity.exe" }
                        }
                    }
                });

                if (files.Count > 0)
                {
                    var editorPath = files[0].Path.LocalPath;

                    if (!File.Exists(editorPath))
                    {
                        _plugin.Context?.LogWarning($"所选文件不存在: {editorPath}");
                        return;
                    }

                    // 从 Unity.exe 的产品版本信息中提取版本号
                    var version = ExtractVersionFromFileInfo(editorPath);

                    if (string.IsNullOrEmpty(version))
                    {
                        // 如果无法从文件信息中获取，尝试从路径提取
                        version = ExtractVersionFromPath(editorPath);
                    }

                    if (string.IsNullOrEmpty(version))
                    {
                        version = $"Unity-{DateTime.Now:yyyyMMdd}";
                    }

                    await _plugin.AddVersionAsync(version, editorPath);
                    _viewModel.Refresh();

                    // 重新附加删除按钮事件
                    AttachDeleteButtonHandlers();

                    _plugin.Context?.LogInfo($"已添加 Unity 版本: {version}");
                }
            }
            catch (Exception ex)
            {
                _plugin.Context?.LogError($"添加 Unity 版本失败: {ex.Message}");
            }
        }

        private void OnDeleteVersionClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Models.UnityVersion version)
            {
                if (_viewModel.DeleteVersionCommand.CanExecute(version))
                {
                    _viewModel.DeleteVersionCommand.Execute(version);

                    // 刷新后重新附加事件
                    AttachDeleteButtonHandlers();
                }
            }
        }

        /// <summary>
        /// 从 Unity.exe 的产品版本信息中提取版本号
        /// 产品版本格式: 2021.3.39f1_fb3b7b32f191
        /// 提取结果: 2021.3.39f1
        /// </summary>
        private string? ExtractVersionFromFileInfo(string exePath)
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                var productVersion = versionInfo.ProductVersion;

                if (!string.IsNullOrEmpty(productVersion))
                {
                    // 产品版本格式: 2021.3.39f1_fb3b7b32f191
                    // 我们只需要下划线之前的部分: 2021.3.39f1
                    var underscoreIndex = productVersion.IndexOf('_');
                    if (underscoreIndex > 0)
                    {
                        return productVersion.Substring(0, underscoreIndex);
                    }
                    
                    // 如果没有下划线，直接返回产品版本
                    return productVersion;
                }
            }
            catch (Exception ex)
            {
                _plugin.Context?.LogWarning($"无法读取 Unity 版本信息: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 从路径中提取版本号（备用方法）
        /// </summary>
        private string? ExtractVersionFromPath(string path)
        {
            try
            {
                // 尝试从路径中提取版本号
                // 例如: C:\Program Files\Unity\Hub\Editor\2022.3.10f1\Editor\Unity.exe
                var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                foreach (var part in parts)
                {
                    // Unity 版本格式通常是: YYYY.X.XXfX 或 YYYY.X.XXaX 或 YYYY.X.XXbX
                    if (part.Length > 6 && char.IsDigit(part[0]) && part.Contains('.'))
                    {
                        // 简单验证是否看起来像版本号
                        if (part.Split('.').Length >= 2)
                        {
                            return part;
                        }
                    }
                }
            }
            catch
            {
                // 忽略错误
            }

            return null;
        }
    }
}

