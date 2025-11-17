using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using MicroDock.Plugin;
using System;
using System.IO;
using System.Linq;
using UnityProjectPlugin.Models;
using UnityProjectPlugin.ViewModels;

namespace UnityProjectPlugin.Views
{
    /// <summary>
    /// Unity 项目列表标签页视图
    /// </summary>
    public partial class UnityProjectTabView : UserControl, IMicroTab
    {
        private readonly UnityProjectPlugin _plugin;
        private readonly UnityProjectTabViewModel _viewModel;

        /// <summary>
        /// 公开插件实例供子控件使用
        /// </summary>
        public UnityProjectPlugin Plugin => _plugin;

        // UI 控件引用
        private TextBox? _searchTextBox;
        private ToggleButton? _groupViewToggle;
        private Button? _addProjectButton;
        private Button? _emptyAddButton;
        private ItemsControl? _tileView;
        private ItemsControl? _groupView;
        private StackPanel? _emptyState;

        public UnityProjectTabView(UnityProjectPlugin plugin)
        {
            _plugin = plugin;
            _viewModel = new UnityProjectTabViewModel(plugin);

            InitializeComponent();
            InitializeControls();
            AttachEventHandlers();
            UpdateEmptyState();
        }

        public string TabName => "Unity项目";

        public IconSymbolEnum IconSymbol => IconSymbolEnum.GamesFilled;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _searchTextBox = this.FindControl<TextBox>("SearchTextBox");
            _groupViewToggle = this.FindControl<ToggleButton>("GroupViewToggle");
            _addProjectButton = this.FindControl<Button>("AddProjectButton");
            _emptyAddButton = this.FindControl<Button>("EmptyAddButton");
            _tileView = this.FindControl<ItemsControl>("TileView");
            _groupView = this.FindControl<ItemsControl>("GroupView");
            _emptyState = this.FindControl<StackPanel>("EmptyState");

            // 设置 DataContext
            DataContext = _viewModel;
        }

        private void AttachEventHandlers()
        {
            if (_addProjectButton != null)
            {
                _addProjectButton.Click += OnAddProjectClick;
            }

            if (_emptyAddButton != null)
            {
                _emptyAddButton.Click += OnAddProjectClick;
            }

            // 监听项目列表变化以更新空状态
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.FilteredProjects))
                {
                    UpdateEmptyState();
                }
            };
        }

        /// <summary>
        /// 刷新项目列表（供卡片调用）
        /// </summary>
        public void RefreshProjects()
        {
            _viewModel.LoadProjects();
        }

        private void UpdateEmptyState()
        {
            if (_emptyState != null)
            {
                _emptyState.IsVisible = _viewModel.FilteredProjects.Count == 0;
            }
        }

        private async void OnAddProjectClick(object? sender, RoutedEventArgs e)
        {
            await AddProjectAsync();
        }

        private async System.Threading.Tasks.Task AddProjectAsync()
        {
            try
            {
                TopLevel? topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                System.Collections.Generic.IReadOnlyList<IStorageFolder> folders = 
                    await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                    {
                        Title = "选择 Unity 项目文件夹",
                        AllowMultiple = false
                    });

                if (folders.Count > 0)
                {
                    string folderPath = folders[0].Path.LocalPath;

                    // 验证是否是 Unity 项目
                    string assetsPath = Path.Combine(folderPath, "Assets");
                    string projectSettingsPath = Path.Combine(folderPath, "ProjectSettings");

                    if (!Directory.Exists(assetsPath) || !Directory.Exists(projectSettingsPath))
                    {
                        // TODO: 显示错误消息对话框
                        return;
                    }

                    _viewModel.AddProject(folderPath);
                }
            }
            catch (Exception ex)
            {
                // TODO: 显示错误消息对话框
            }
        }
    }
}
