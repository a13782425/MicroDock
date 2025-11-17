using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ClaudeSwitchPlugin.Models;
using System;

namespace ClaudeSwitchPlugin.Views
{
    /// <summary>
    /// 配置卡片视图
    /// </summary>
    public partial class ConfigurationCard : UserControl
    {
        private AIConfiguration? _configuration;

        // UI 控件引用
        private TextBlock? _providerIcon;
        private TextBlock? _configName;
        private TextBlock? _baseURL;
        private TextBlock? _maskedApiKey;
        private TextBlock? _modelName;
        private Border? _modelBorder;
        private TextBlock? _lastUsedText;
        private Border? _activeTag;
        private Button? _applyButton;
        private MenuItem? _editMenuItem;
        private MenuItem? _deleteMenuItem;

        public ConfigurationCard()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _providerIcon = this.FindControl<TextBlock>("ProviderIcon");
            _configName = this.FindControl<TextBlock>("ConfigName");
            _baseURL = this.FindControl<TextBlock>("BaseURL");
            _maskedApiKey = this.FindControl<TextBlock>("MaskedApiKey");
            _modelName = this.FindControl<TextBlock>("ModelName");
            _modelBorder = this.FindControl<Border>("ModelBorder");
            _lastUsedText = this.FindControl<TextBlock>("LastUsedText");
            _activeTag = this.FindControl<Border>("ActiveTag");
            _applyButton = this.FindControl<Button>("ApplyButton");
            _editMenuItem = this.FindControl<MenuItem>("EditMenuItem");
            _deleteMenuItem = this.FindControl<MenuItem>("DeleteMenuItem");

            // 绑定事件
            if (_applyButton != null)
            {
                _applyButton.Click += OnApplyClick;
            }

            if (_editMenuItem != null)
            {
                _editMenuItem.Click += OnEditClick;
            }

            if (_deleteMenuItem != null)
            {
                _deleteMenuItem.Click += OnDeleteClick;
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is AIConfiguration config)
            {
                _configuration = config;
                UpdateUI();
            }
        }

        /// <summary>
        /// 更新 UI 显示
        /// </summary>
        private void UpdateUI()
        {
            if (_configuration == null) return;

            // 提供商图标
            if (_providerIcon != null)
            {
                _providerIcon.Text = _configuration.ProviderIcon;
            }

            // 配置名称
            if (_configName != null)
            {
                _configName.Text = _configuration.Name;
            }

            // 基础 URL
            if (_baseURL != null)
            {
                _baseURL.Text = _configuration.BaseURL;
            }

            // 脱敏的 API Key
            if (_maskedApiKey != null)
            {
                _maskedApiKey.Text = _configuration.MaskedApiKey;
            }

            // 模型名称
            if (_modelName != null && _modelBorder != null)
            {
                bool hasModel = !string.IsNullOrWhiteSpace(_configuration.Model);
                _modelBorder.IsVisible = hasModel;
                if (hasModel)
                {
                    _modelName.Text = _configuration.Model;
                }
            }

            // 最后使用时间
            if (_lastUsedText != null)
            {
                _lastUsedText.Text = $"最后使用: {_configuration.LastUsed:yyyy-MM-dd HH:mm}";
            }

            // 活跃状态
            if (_activeTag != null)
            {
                _activeTag.IsVisible = _configuration.IsActive;
            }

            // 应用按钮状态
            if (_applyButton != null)
            {
                _applyButton.IsEnabled = !_configuration.IsActive;
            }

            // 删除按钮状态
            if (_deleteMenuItem != null)
            {
                _deleteMenuItem.IsEnabled = !_configuration.IsActive;
            }
        }

        /// <summary>
        /// 应用配置
        /// </summary>
        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            if (_configuration == null) return;

            var parent = this.FindAncestorOfType<ClaudeSwitchTabView>();
            parent?.ApplyConfiguration(_configuration);
        }

        /// <summary>
        /// 编辑配置
        /// </summary>
        private void OnEditClick(object? sender, RoutedEventArgs e)
        {
            if (_configuration == null) return;

            var parent = this.FindAncestorOfType<ClaudeSwitchTabView>();
            parent?.EditConfiguration(_configuration);
        }

        /// <summary>
        /// 删除配置
        /// </summary>
        private void OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            if (_configuration == null) return;

            var parent = this.FindAncestorOfType<ClaudeSwitchTabView>();
            parent?.DeleteConfiguration(_configuration);
        }
    }
}