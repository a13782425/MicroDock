using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClaudeSwitchPlugin.Models;
using FluentAvalonia.UI.Controls;
using MicroDock.Plugin;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ClaudeSwitchPlugin.Views
{
    /// <summary>
    /// Claude 配置切换主标签页视图
    /// </summary>
    public partial class ClaudeSwitchTabView : UserControl, IMicroTab
    {
        private readonly ClaudeSwitchPlugin _plugin;
        private AIProvider _currentProvider = AIProvider.Claude;
        private string _searchText = "";

        // UI 控件引用
        private TextBox? _searchTextBox;
        private ComboBox? _providerComboBox;
        private Button? _addConfigButton;
        private Button? _emptyAddButton;
        private ItemsControl? _configurationsView;
        private StackPanel? _emptyState;

        public ObservableCollection<AIConfiguration> FilteredConfigurations { get; } = new();

        public ClaudeSwitchTabView(ClaudeSwitchPlugin plugin)
        {
            _plugin = plugin;
            InitializeComponent();
            InitializeControls();
            AttachEventHandlers();
            LoadConfigurations();
        }

        public string TabName => "Claude 配置";
        public IconSymbolEnum IconSymbol => IconSymbolEnum.Settings;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _searchTextBox = this.FindControl<TextBox>("SearchTextBox");
            _providerComboBox = this.FindControl<ComboBox>("ProviderComboBox");
            _addConfigButton = this.FindControl<Button>("AddConfigButton");
            _emptyAddButton = this.FindControl<Button>("EmptyAddButton");
            _configurationsView = this.FindControl<ItemsControl>("ConfigurationsView");
            _emptyState = this.FindControl<StackPanel>("EmptyState");

            // 设置 ItemsSource
            if (_configurationsView != null)
            {
                _configurationsView.ItemsSource = FilteredConfigurations;
            }
        }

        private void AttachEventHandlers()
        {
            if (_searchTextBox != null)
            {
                _searchTextBox.TextChanged += OnSearchTextChanged;
            }

            if (_providerComboBox != null)
            {
                _providerComboBox.SelectionChanged += OnProviderChanged;
            }

            if (_addConfigButton != null)
            {
                _addConfigButton.Click += OnAddConfigClick;
            }

            if (_emptyAddButton != null)
            {
                _emptyAddButton.Click += OnAddConfigClick;
            }
        }

        /// <summary>
        /// 加载配置列表
        /// </summary>
        public void LoadConfigurations()
        {
            FilteredConfigurations.Clear();

            var configs = string.IsNullOrWhiteSpace(_searchText)
                ? _plugin.GetConfigurationsByProvider(_currentProvider)
                : _plugin.SearchConfigurations(_currentProvider, _searchText);

            foreach (var config in configs)
            {
                FilteredConfigurations.Add(config);
            }

            UpdateEmptyState();
        }

        /// <summary>
        /// 更新空状态显示
        /// </summary>
        private void UpdateEmptyState()
        {
            if (_emptyState != null && _configurationsView != null)
            {
                _emptyState.IsVisible = FilteredConfigurations.Count == 0;
            }
        }

        /// <summary>
        /// 搜索文本变化处理
        /// </summary>
        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            _searchText = _searchTextBox?.Text ?? "";
            LoadConfigurations();
        }

        /// <summary>
        /// 提供商切换处理
        /// </summary>
        private void OnProviderChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_providerComboBox?.SelectedIndex >= 0)
            {
                _currentProvider = (AIProvider)_providerComboBox.SelectedIndex;
                LoadConfigurations();
            }
        }

        /// <summary>
        /// 添加配置按钮点击
        /// </summary>
        private async void OnAddConfigClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "添加 AI 配置",
                    PrimaryButtonText = "保存",
                    CloseButtonText = "取消"
                };

                var editForm = new EditConfigForm(_currentProvider);
                dialog.Content = editForm;

                dialog.PrimaryButtonClick += (s, args) =>
                {
                    args.Cancel = !editForm.Validate();
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    var config = editForm.GetConfiguration();
                    _plugin.SaveConfiguration(config);
                    LoadConfigurations();
                }
            }
            catch (Exception ex)
            {
                // 记录错误（LogError 是 protected 方法，需要通过插件实例内部调用）
                System.Diagnostics.Debug.WriteLine($"添加配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 编辑配置
        /// </summary>
        public async void EditConfiguration(AIConfiguration config)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "编辑 AI 配置",
                    PrimaryButtonText = "保存",
                    CloseButtonText = "取消"
                };

                var editForm = new EditConfigForm(config);
                dialog.Content = editForm;

                dialog.PrimaryButtonClick += (s, args) =>
                {
                    args.Cancel = !editForm.Validate();
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    var updatedConfig = editForm.GetConfiguration();
                    _plugin.SaveConfiguration(updatedConfig);
                    LoadConfigurations();
                }
            }
            catch (Exception ex)
            {
                // 记录错误
                System.Diagnostics.Debug.WriteLine($"编辑配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除配置
        /// </summary>
        public async void DeleteConfiguration(AIConfiguration config)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "确认删除",
                    Content = $"确定要删除配置 \"{config.Name}\" 吗？",
                    PrimaryButtonText = "删除",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Close
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    _plugin.DeleteConfiguration(config.Id);
                    LoadConfigurations();
                }
            }
            catch (Exception ex)
            {
                // 记录错误
                System.Diagnostics.Debug.WriteLine($"删除配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用配置
        /// </summary>
        public void ApplyConfiguration(AIConfiguration config)
        {
            try
            {
                _plugin.ApplyConfiguration(config);
                LoadConfigurations();
            }
            catch (Exception ex)
            {
                // 记录错误
                System.Diagnostics.Debug.WriteLine($"应用配置失败: {ex.Message}");
            }
        }
    }
}