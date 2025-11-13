using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MicroDock.Plugin;
using System;
using System.Collections.Generic;
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

        // UI 控件引用
        private TextBox? _searchTextBox;
        private Button? _addProjectButton;
        private ListBox? _projectsListBox;
        private TextBlock? _projectNameText;
        private TextBlock? _projectPathText;
        private TextBlock? _projectVersionText;
        private TextBlock? _lastOpenedText;
        private Button? _openProjectButton;
        private Button? _deleteProjectButton;
        private TextBlock? _emptyStateText;

        public UnityProjectTabView(UnityProjectPlugin plugin)
        {
            _plugin = plugin;
            _viewModel = new UnityProjectTabViewModel(plugin);

            InitializeComponent();
            InitializeControls();
            AttachEventHandlers();

            _viewModel.AddProjectRequested += OnAddProjectRequested;
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
            _projectsListBox = this.FindControl<ListBox>("ProjectsListBox");
            _projectNameText = this.FindControl<TextBlock>("ProjectNameText");
            _projectPathText = this.FindControl<TextBlock>("ProjectPathText");
            _projectVersionText = this.FindControl<TextBlock>("ProjectVersionText");
            _lastOpenedText = this.FindControl<TextBlock>("LastOpenedText");
            _openProjectButton = this.FindControl<Button>("OpenProjectButton");
            _deleteProjectButton = this.FindControl<Button>("DeleteProjectButton");
            _emptyStateText = this.FindControl<TextBlock>("EmptyStateText");

            if (_projectsListBox != null)
            {
                _projectsListBox.ItemsSource = _viewModel.Projects;
            }
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

            if (_projectsListBox != null)
            {
                _projectsListBox.SelectionChanged += OnProjectSelectionChanged;
                _projectsListBox.DoubleTapped += OnProjectDoubleClick;
            }

            if (_openProjectButton != null)
            {
                _openProjectButton.Click += OnOpenProjectClick;
            }

            if (_deleteProjectButton != null)
            {
                _deleteProjectButton.Click += OnDeleteProjectClick;
            }
        }

        private void OnProjectSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var selected = _projectsListBox?.SelectedItem as UnityProject;
            _viewModel.SelectedProject = selected;

            UpdateDetailPanel(selected);
        }

        private void UpdateDetailPanel(UnityProject? project)
        {
            bool hasProject = project != null;

            if (_projectNameText != null)
                _projectNameText.Text = project?.Name ?? string.Empty;

            if (_projectPathText != null)
                _projectPathText.Text = project?.Path ?? string.Empty;

            if (_projectVersionText != null)
                _projectVersionText.Text = project?.UnityVersion ?? "未知";

            if (_lastOpenedText != null)
                _lastOpenedText.Text = project?.LastOpened.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;

            if (_openProjectButton != null)
                _openProjectButton.IsEnabled = hasProject;

            if (_deleteProjectButton != null)
                _deleteProjectButton.IsEnabled = hasProject;

            if (_emptyStateText != null)
                _emptyStateText.IsVisible = !hasProject;
        }

        private async void OnAddProjectClick(object? sender, RoutedEventArgs e)
        {
            await AddProjectAsync();
        }

        private void OnAddProjectRequested(object? sender, EventArgs e)
        {
            _ = AddProjectAsync();
        }

        private async System.Threading.Tasks.Task AddProjectAsync()
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "选择 Unity 项目文件夹",
                    AllowMultiple = false
                });

                if (folders.Count > 0)
                {
                    var folderPath = folders[0].Path.LocalPath;

                    // 验证是否是 Unity 项目
                    var assetsPath = Path.Combine(folderPath, "Assets");
                    var projectSettingsPath = Path.Combine(folderPath, "ProjectSettings");

                    if (!Directory.Exists(assetsPath) || !Directory.Exists(projectSettingsPath))
                    {
                        _plugin.Context?.LogWarning($"所选文件夹不是有效的 Unity 项目: {folderPath}");
                        return;
                    }

                    _plugin.AddProject(folderPath);
                    _viewModel.Refresh();

                    _plugin.Context?.LogInfo($"已添加项目: {folderPath}");
                }
            }
            catch (Exception ex)
            {
                _plugin.Context?.LogError($"添加项目失败: {ex.Message}");
            }
        }

        private void OnOpenProjectClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel.OpenProjectCommand.CanExecute(null))
            {
                _viewModel.OpenProjectCommand.Execute(null);
            }
        }

        private void OnProjectDoubleClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedProject != null)
            {
                _viewModel.OpenProjectCommand.Execute(null);
            }
        }

        private void OnDeleteProjectClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel.DeleteProjectCommand.CanExecute(null))
            {
                _viewModel.DeleteProjectCommand.Execute(null);
            }
        }
    }
}

