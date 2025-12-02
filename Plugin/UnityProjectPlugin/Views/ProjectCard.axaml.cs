using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using UnityProjectPlugin.Models;

namespace UnityProjectPlugin.Views
{
    /// <summary>
    /// 项目卡片控件
    /// </summary>
    public partial class ProjectCard : UserControl
    {
        //private SplitButton? _openSplitButton;
        //private MenuItem? _deleteMenuItem;
        //private MenuItem? _openDirectoryMenuItem;
        //private TextBlock? ProjectNameDisplay;
        //private TextBox? ProjectNameEditor;
        //private TextBlock? _projectPathText;
        //private Button? _groupButton;
        //private TextBlock? GroupButtonText;
        //private ComboBox? GroupComboBox;
        //private ItemsControl? GroupsListControl;
        //private TextBox? NewGroupNameTextBox;
        //private Button? _addGroupButton;
        //private Image? ProjectIcon;
        private UnityProjectPlugin? _plugin;
        private bool _isEditingName = false;
        private string _originalName = string.Empty;

        public ProjectCard()
        {
            InitializeComponent(true);
            AttachEventHandlers();
        }

        private void AttachEventHandlers()
        {
            if (OpenDirectoryMenuItem != null)
            {
                OpenDirectoryMenuItem.Click += OnOpenDirectoryClick;
            }

            // 项目路径点击打开目录
            if (ProjectPathText != null)
            {
                ProjectPathText.PointerPressed += OnPathPointerPressed;
            }

            // 项目名双击编辑
            if (ProjectNameDisplay != null)
            {
                ProjectNameDisplay.DoubleTapped += OnNameDoubleTapped;
            }

            // 项目名编辑器事件
            if (ProjectNameEditor != null)
            {
                ProjectNameEditor.LostFocus += OnNameEditorLostFocus;
                ProjectNameEditor.KeyDown += OnNameEditorKeyDown;
            }

            // 分组按钮事件
            if (GroupButton != null)
            {
                GroupButton.Click += OnGroupButtonClick;
            }

            // 分组选择器事件
            if (GroupComboBox != null)
            {
                GroupComboBox.SelectionChanged += OnGroupSelectionChanged;
            }

            // 添加分组按钮事件
            if (AddGroupButton != null)
            {
                AddGroupButton.Click += OnAddGroupButtonClick;
            }

            // 新分组输入框回车事件
            if (NewGroupNameTextBox != null)
            {
                NewGroupNameTextBox.KeyDown += OnNewGroupNameKeyDown;
            }
            // 删除菜单项点击
            if (DeleteMenuItem != null)
            {
                DeleteMenuItem.Click += OnDeleteMenuItemClick;
            }
            // 监听 DataContext 变化
            this.DataContextChanged += OnDataContextChanged;
            this.AttachedToVisualTree += OnAttachedToVisualTree;
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            // 查找父级 UnityProjectTabView 获取插件实例
            UnityProjectTabView? tabView = this.FindAncestorOfType<UnityProjectTabView>();
            if (tabView != null)
            {
                _plugin = tabView.Plugin;
            }
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is UnityProject project)
            {
                LoadProjectIcon(project);
                UpdateGroupDisplay(project);
            }
        }

        /// <summary>
        /// 加载项目图标
        /// </summary>
        private void LoadProjectIcon(UnityProject project)
        {
            if (ProjectIcon == null) return;

            try
            {

                // 如果没有自定义图标，使用插件内嵌的默认 unity.png 资源
                try
                {
                    var uri = new Uri("avares://UnityProjectPlugin/Assets/unity.png");
                    var asset = Avalonia.Platform.AssetLoader.Open(uri);
                    ProjectIcon.Source = new Bitmap(asset);
                }
                catch (Exception ex)
                {
                    // 如果 avares 加载失败，尝试使用文件系统路径作为后备方案

                    // 记录错误信息以便调试
                    _plugin.Context.LogError($"Failed to load default icon: {ex.Message}");

                }
            }
            catch
            {
                // 加载失败，图标为空
                ProjectIcon.Source = null;
            }
        }

        /// <summary>
        /// 更新分组显示
        /// </summary>
        private void UpdateGroupDisplay(UnityProject project)
        {
            if (GroupButtonText == null) return;

            if (!string.IsNullOrEmpty(project.GroupName))
            {
                GroupButtonText.Text = project.GroupName;
            }
            else
            {
                GroupButtonText.Text = "未分组";
            }
        }
        /// <summary>
        /// 删除菜单项点击事件
        /// </summary>
        private async void OnDeleteMenuItemClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not UnityProject project)
                return;

            // 获取父级 UnityProjectTabView 的 ViewModel
            var tabView = this.FindAncestorOfType<UnityProjectTabView>();
            var viewModel = tabView?.ViewModel;

            if (viewModel?.DeleteProjectCommand?.CanExecute(project) == true)
            {
                viewModel.DeleteProjectCommand.Execute(project);
            }
        }
        /// <summary>
        /// 分组按钮点击 - 打开分组管理 Flyout
        /// </summary>
        private void OnGroupButtonClick(object? sender, RoutedEventArgs e)
        {
            LoadGroupsData();
        }

        /// <summary>
        /// 加载分组数据
        /// </summary>
        private void LoadGroupsData()
        {
            if (_plugin == null || GroupComboBox == null || GroupsListControl == null) return;

            List<ProjectGroup> groups = _plugin.GetGroups();

            // 设置 ComboBox 数据源
            List<string> groupNames = groups.Select(g => g.Name).ToList();
            groupNames.Insert(0, string.Empty); // 添加"无分组"选项
            GroupComboBox.ItemsSource = groupNames;

            // 设置当前选择
            if (DataContext is UnityProject project)
            {
                if (!string.IsNullOrEmpty(project.GroupName))
                {
                    GroupComboBox.SelectedItem = project.GroupName;
                }
                else
                {
                    GroupComboBox.SelectedIndex = 0;
                }
            }

            // 设置分组列表
            GroupsListControl.ItemsSource = groups;
        }

        /// <summary>
        /// 项目名双击事件 - 进入编辑模式
        /// </summary>
        private void OnNameDoubleTapped(object? sender, TappedEventArgs e)
        {
            EnterNameEditMode();
        }

        private void EnterNameEditMode()
        {
            if (ProjectNameDisplay == null || ProjectNameEditor == null || _isEditingName) return;
            if (DataContext is not UnityProject project) return;

            _isEditingName = true;
            _originalName = project.Name;

            ProjectNameDisplay.IsVisible = false;
            ProjectNameEditor.IsVisible = true;
            ProjectNameEditor.Text = project.Name;
            ProjectNameEditor.Focus();
            ProjectNameEditor.SelectAll();
        }

        private async Task ExitNameEditModeAsync(bool save)
        {
            if (ProjectNameDisplay == null || ProjectNameEditor == null || !_isEditingName) return;
            if (DataContext is not UnityProject project) return;

            _isEditingName = false;

            if (save)
            {
                string newName = ProjectNameEditor.Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(newName) && newName != _originalName && _plugin != null)
                {
                    // 保存新名称
                    await _plugin.UpdateProjectAsync(project.Path, newName, project.GroupName);
                    project.Name = newName;

                    // 刷新列表
                    RefreshParentList();
                }
            }
            else
            {
                // 恢复原名称
                ProjectNameEditor.Text = _originalName;
            }

            ProjectNameDisplay.IsVisible = true;
            ProjectNameEditor.IsVisible = false;
        }

        private async void OnNameEditorLostFocus(object? sender, RoutedEventArgs e)
        {
            // 支持失去焦点时保存
            await ExitNameEditModeAsync(true);
        }

        private async void OnNameEditorKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await ExitNameEditModeAsync(true);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                await ExitNameEditModeAsync(false);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 分组选择变化事件
        /// </summary>
        private async void OnGroupSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (GroupComboBox == null || _plugin == null) return;
            if (DataContext is not UnityProject project) return;

            string? selectedGroup = GroupComboBox.SelectedItem as string;
            string? newGroupName = string.IsNullOrEmpty(selectedGroup) ? null : selectedGroup;

            // 如果分组变化了，保存
            if (newGroupName != project.GroupName)
            {
                await _plugin.UpdateProjectAsync(project.Path, project.Name, newGroupName);
                project.GroupName = newGroupName;
                UpdateGroupDisplay(project);

                // 刷新列表
                RefreshParentList();
            }
        }

        /// <summary>
        /// 添加新分组
        /// </summary>
        private async void OnAddGroupButtonClick(object? sender, RoutedEventArgs e)
        {
            await AddNewGroupAsync();
        }
  
        private async void OnNewGroupNameKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await AddNewGroupAsync();
                e.Handled = true;
            }
        }

        private async Task AddNewGroupAsync()
        {
            if (NewGroupNameTextBox == null || _plugin == null) return;

            string? newGroupName = NewGroupNameTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(newGroupName)) return;

            // 检查是否已存在
            if (_plugin.GetGroups().Any(g => g.Name == newGroupName))
            {
                // TODO: 显示提示
                return;
            }

            // 添加新分组
            await _plugin.AddGroupAsync(newGroupName);
            NewGroupNameTextBox.Text = string.Empty;

            // 重新加载分组列表
            LoadGroupsData();
        }

        /// <summary>
        /// 删除分组按钮点击
        /// </summary>
        public async void DeleteGroupButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ProjectGroup group)
            {
                // 通过 ViewModel 删除分组
                UnityProjectTabView? tabView = this.FindAncestorOfType<UnityProjectTabView>();
                if (tabView != null)
                {
                    await tabView.ViewModel.DeleteGroupAsync(group);
                    
                    // 重新加载分组列表（用于分组下拉框）
                    LoadGroupsData();
                }
            }
        }



        /// <summary>
        /// 打开目录菜单项点击事件
        /// </summary>
        private void OnOpenDirectoryClick(object? sender, RoutedEventArgs e)
        {
            OpenProjectDirectory();
        }

        /// <summary>
        /// 项目路径点击事件
        /// </summary>
        private void OnPathPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            OpenProjectDirectory();
        }

        /// <summary>
        /// 打开项目目录
        /// </summary>
        private void OpenProjectDirectory()
        {
            if (DataContext is UnityProject project)
            {
                try
                {
                    if (Directory.Exists(project.Path))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = project.Path,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        _plugin?.Context.LogWarning($"项目目录不存在: {project.Path}");
                    }
                }
                catch (Exception ex)
                {
                    _plugin?.Context.LogError($"打开目录失败: {project.Path}", ex);
                }
            }
        }

        private void RefreshParentList()
        {
            UnityProjectTabView? tabView = this.FindAncestorOfType<UnityProjectTabView>();
            tabView?.RefreshProjects();
        }
    }

    /// <summary>
    /// 查找可视树祖先的扩展方法
    /// </summary>
    public static class VisualTreeHelper
    {
        public static T? FindAncestorOfType<T>(this Control control) where T : class
        {
            Avalonia.Visual? current = control.GetVisualParent();
            while (current != null)
            {
                if (current is T result)
                    return result;
                current = current.GetVisualParent();
            }
            return null;
        }
    }
}

