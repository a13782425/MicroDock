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
using System.Threading.Tasks;
using UnityProjectPlugin.Models;

namespace UnityProjectPlugin.Views
{
    /// <summary>
    /// 项目卡片控件
    /// </summary>
    public partial class ProjectCard : UserControl
    {
        private SplitButton? _openSplitButton;
        private MenuItem? _deleteMenuItem;
        private MenuItem? _openDirectoryMenuItem;
        private TextBlock? _projectNameDisplay;
        private TextBox? _projectNameEditor;
        private TextBlock? _projectPathText;
        private Button? _groupButton;
        private TextBlock? _groupButtonText;
        private ComboBox? _groupComboBox;
        private ItemsControl? _groupsListControl;
        private TextBox? _newGroupNameTextBox;
        private Button? _addGroupButton;
        private Image? _projectIcon;
        private UnityProjectPlugin? _plugin;
        private bool _isEditingName = false;
        private string _originalName = string.Empty;

        public ProjectCard()
        {
            InitializeComponent();
            InitializeControls();
            AttachEventHandlers();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _openSplitButton = this.FindControl<SplitButton>("OpenSplitButton");
            _deleteMenuItem = this.FindControl<MenuItem>("DeleteMenuItem");
            _openDirectoryMenuItem = this.FindControl<MenuItem>("OpenDirectoryMenuItem");
            _projectNameDisplay = this.FindControl<TextBlock>("ProjectNameDisplay");
            _projectNameEditor = this.FindControl<TextBox>("ProjectNameEditor");
            _projectPathText = this.FindControl<TextBlock>("ProjectPathText");
            _groupButton = this.FindControl<Button>("GroupButton");
            _groupButtonText = this.FindControl<TextBlock>("GroupButtonText");
            _groupComboBox = this.FindControl<ComboBox>("GroupComboBox");
            _groupsListControl = this.FindControl<ItemsControl>("GroupsListControl");
            _newGroupNameTextBox = this.FindControl<TextBox>("NewGroupNameTextBox");
            _addGroupButton = this.FindControl<Button>("AddGroupButton");
            _projectIcon = this.FindControl<Image>("ProjectIcon");
        }

        private void AttachEventHandlers()
        {
            if (_openDirectoryMenuItem != null)
            {
                _openDirectoryMenuItem.Click += OnOpenDirectoryClick;
            }

            // 项目路径点击打开目录
            if (_projectPathText != null)
            {
                _projectPathText.PointerPressed += OnPathPointerPressed;
            }

            // 项目名双击编辑
            if (_projectNameDisplay != null)
            {
                _projectNameDisplay.DoubleTapped += OnNameDoubleTapped;
            }

            // 项目名编辑器事件
            if (_projectNameEditor != null)
            {
                _projectNameEditor.LostFocus += OnNameEditorLostFocus;
                _projectNameEditor.KeyDown += OnNameEditorKeyDown;
            }

            // 分组按钮事件
            if (_groupButton != null)
            {
                _groupButton.Click += OnGroupButtonClick;
            }

            // 分组选择器事件
            if (_groupComboBox != null)
            {
                _groupComboBox.SelectionChanged += OnGroupSelectionChanged;
            }

            // 添加分组按钮事件
            if (_addGroupButton != null)
            {
                _addGroupButton.Click += OnAddGroupButtonClick;
            }

            // 新分组输入框回车事件
            if (_newGroupNameTextBox != null)
            {
                _newGroupNameTextBox.KeyDown += OnNewGroupNameKeyDown;
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
            if (_projectIcon == null) return;

            try
            {

                // 如果没有自定义图标，使用插件内嵌的默认 unity.png 资源
                try
                {
                    var uri = new Uri("avares://UnityProjectPlugin/Assets/unity.png");
                    var asset = Avalonia.Platform.AssetLoader.Open(uri);
                    _projectIcon.Source = new Bitmap(asset);
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
                _projectIcon.Source = null;
            }
        }

        /// <summary>
        /// 更新分组显示
        /// </summary>
        private void UpdateGroupDisplay(UnityProject project)
        {
            if (_groupButtonText == null) return;

            if (!string.IsNullOrEmpty(project.GroupName))
            {
                _groupButtonText.Text = project.GroupName;
            }
            else
            {
                _groupButtonText.Text = "未分组";
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
            if (_plugin == null || _groupComboBox == null || _groupsListControl == null) return;

            List<ProjectGroup> groups = _plugin.GetGroups();

            // 设置 ComboBox 数据源
            List<string> groupNames = groups.Select(g => g.Name).ToList();
            groupNames.Insert(0, string.Empty); // 添加"无分组"选项
            _groupComboBox.ItemsSource = groupNames;

            // 设置当前选择
            if (DataContext is UnityProject project)
            {
                if (!string.IsNullOrEmpty(project.GroupName))
                {
                    _groupComboBox.SelectedItem = project.GroupName;
                }
                else
                {
                    _groupComboBox.SelectedIndex = 0;
                }
            }

            // 设置分组列表
            _groupsListControl.ItemsSource = groups;
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
            if (_projectNameDisplay == null || _projectNameEditor == null || _isEditingName) return;
            if (DataContext is not UnityProject project) return;

            _isEditingName = true;
            _originalName = project.Name;

            _projectNameDisplay.IsVisible = false;
            _projectNameEditor.IsVisible = true;
            _projectNameEditor.Text = project.Name;
            _projectNameEditor.Focus();
            _projectNameEditor.SelectAll();
        }

        private async Task ExitNameEditModeAsync(bool save)
        {
            if (_projectNameDisplay == null || _projectNameEditor == null || !_isEditingName) return;
            if (DataContext is not UnityProject project) return;

            _isEditingName = false;

            if (save)
            {
                string newName = _projectNameEditor.Text?.Trim() ?? string.Empty;
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
                _projectNameEditor.Text = _originalName;
            }

            _projectNameDisplay.IsVisible = true;
            _projectNameEditor.IsVisible = false;
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
            if (_groupComboBox == null || _plugin == null) return;
            if (DataContext is not UnityProject project) return;

            string? selectedGroup = _groupComboBox.SelectedItem as string;
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
            if (_newGroupNameTextBox == null || _plugin == null) return;

            string? newGroupName = _newGroupNameTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(newGroupName)) return;

            // 检查是否已存在
            if (_plugin.GetGroups().Any(g => g.Name == newGroupName))
            {
                // TODO: 显示提示
                return;
            }

            // 添加新分组
            await _plugin.AddGroupAsync(newGroupName);
            _newGroupNameTextBox.Text = string.Empty;

            // 重新加载分组列表
            LoadGroupsData();
        }

        /// <summary>
        /// 删除分组按钮点击
        /// </summary>
        public async void DeleteGroupButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ProjectGroup group && _plugin != null)
            {
                // 检查是否有项目使用该分组
                int usageCount = _plugin.GetGroupUsageCount(group.Name);
                if (usageCount > 0)
                {
                    // TODO: 显示提示消息
                    return;
                }

                // 删除分组
                await _plugin.DeleteGroupAsync(group.Id);

                // 重新加载分组列表
                LoadGroupsData();

                // 刷新父列表
                RefreshParentList();
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

