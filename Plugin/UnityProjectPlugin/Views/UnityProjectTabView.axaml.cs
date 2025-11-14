using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MicroDock.Plugin;
using System;
using System.IO;
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

        // UI 控件引用
        private TextBox? _searchTextBox;
        private Button? _addProjectButton;
        private DataGrid? _projectsDataGrid;

        public UnityProjectTabView(UnityProjectPlugin plugin)
        {
            _plugin = plugin;
            _viewModel = new UnityProjectTabViewModel(plugin);

            InitializeComponent();
            InitializeControls();
            AttachEventHandlers();
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
            _addProjectButton = this.FindControl<Button>("AddProjectButton");
            _projectsDataGrid = this.FindControl<DataGrid>("ProjectsDataGrid");

            // 设置 DataContext
            DataContext = _viewModel;
        }

        private void AttachEventHandlers()
        {
            if (_searchTextBox != null)
            {
                _searchTextBox.TextChanged += (s, e) =>
                {
                    _viewModel.SearchText = _searchTextBox.Text ?? string.Empty;
                };
            }

            if (_addProjectButton != null)
            {
                _addProjectButton.Click += OnAddProjectClick;
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

        /// <summary>
        /// 打开按钮点击事件（DataGrid 单元格中的按钮）
        /// </summary>
        public void OpenButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is UnityProject project)
            {
                try
                {
                    _viewModel.OpenProject(project);
                }
                catch (Exception ex)
                {
                    // TODO: 显示错误消息对话框
                }
            }
        }

        /// <summary>
        /// 编辑按钮点击事件（菜单项）
        /// </summary>
        public void EditButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is UnityProject project)
            {
                try
                {
                    // 创建编辑 Flyout 内容
                    ProjectEditFlyout editFlyout = new ProjectEditFlyout(_plugin, project, () =>
                    {
                        // 保存后刷新列表
                        _viewModel.LoadProjects();
                    });

                    // 创建并显示 Flyout
                    Flyout flyout = new Flyout
                    {
                        Content = editFlyout,
                        Placement = PlacementMode.Pointer
                    };

                    flyout.ShowAt(menuItem);
                }
                catch (Exception ex)
                {
                    // TODO: 显示错误消息对话框
                }
            }
        }

        /// <summary>
        /// 删除按钮点击事件（菜单项）
        /// </summary>
        public async void DeleteButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is UnityProject project)
            {
                try
                {
                    // TODO: 显示确认对话框
                    // 暂时直接删除
                    _viewModel.DeleteProject(project);
                }
                catch (Exception ex)
                {
                    // TODO: 显示错误消息对话框
                }
            }
        }
    }
}

