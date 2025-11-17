using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ClaudeSwitchPlugin.Models;
using System;

namespace ClaudeSwitchPlugin.Views
{
    /// <summary>
    /// 编辑配置表单
    /// </summary>
    public partial class EditConfigForm : UserControl
    {
        private AIConfiguration? _originalConfig;

        // UI 控件引用
        private TextBox? _nameTextBox;
        private ComboBox? _providerComboBox;
        private TextBox? _baseURLTextBox;
        private TextBox? _apiKeyTextBox;
        private TextBox? _modelTextBox;
        private TextBlock? _errorText;

        /// <summary>
        /// 创建添加配置表单
        /// </summary>
        public EditConfigForm(AIProvider defaultProvider)
        {
            InitializeComponent();
            InitializeControls();
            
            // 设置默认提供商
            if (_providerComboBox != null)
            {
                _providerComboBox.SelectedIndex = (int)defaultProvider;
            }

            // 设置默认 URL
            SetDefaultBaseURL(defaultProvider);
        }

        /// <summary>
        /// 创建编辑配置表单
        /// </summary>
        public EditConfigForm(AIConfiguration config)
        {
            _originalConfig = config;
            InitializeComponent();
            InitializeControls();
            LoadConfiguration(config);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _nameTextBox = this.FindControl<TextBox>("NameTextBox");
            _providerComboBox = this.FindControl<ComboBox>("ProviderComboBox");
            _baseURLTextBox = this.FindControl<TextBox>("BaseURLTextBox");
            _apiKeyTextBox = this.FindControl<TextBox>("ApiKeyTextBox");
            _modelTextBox = this.FindControl<TextBox>("ModelTextBox");
            _errorText = this.FindControl<TextBlock>("ErrorText");

            // 监听提供商变化
            if (_providerComboBox != null)
            {
                _providerComboBox.SelectionChanged += (s, e) =>
                {
                    if (_providerComboBox.SelectedIndex >= 0)
                    {
                        var provider = (AIProvider)_providerComboBox.SelectedIndex;
                        if (string.IsNullOrWhiteSpace(_baseURLTextBox?.Text))
                        {
                            SetDefaultBaseURL(provider);
                        }
                    }
                };
            }
        }

        /// <summary>
        /// 加载配置数据
        /// </summary>
        private void LoadConfiguration(AIConfiguration config)
        {
            if (_nameTextBox != null) _nameTextBox.Text = config.Name;
            if (_providerComboBox != null) _providerComboBox.SelectedIndex = (int)config.Provider;
            if (_baseURLTextBox != null) _baseURLTextBox.Text = config.BaseURL;
            if (_apiKeyTextBox != null) _apiKeyTextBox.Text = config.ApiKey;
            if (_modelTextBox != null) _modelTextBox.Text = config.Model;
        }

        /// <summary>
        /// 设置默认基础 URL
        /// </summary>
        private void SetDefaultBaseURL(AIProvider provider)
        {
            if (_baseURLTextBox != null)
            {
                _baseURLTextBox.Text = provider switch
                {
                    AIProvider.Claude => "https://api.anthropic.com",
                    AIProvider.OpenAI => "https://api.openai.com",
                    _ => ""
                };
            }
        }

        /// <summary>
        /// 验证表单
        /// </summary>
        public bool Validate()
        {
            // 清除之前的错误信息
            if (_errorText != null)
            {
                _errorText.IsVisible = false;
                _errorText.Text = "";
            }

            // 验证配置名称
            if (string.IsNullOrWhiteSpace(_nameTextBox?.Text))
            {
                ShowError("请输入配置名称");
                return false;
            }

            // 验证基础 URL
            if (string.IsNullOrWhiteSpace(_baseURLTextBox?.Text))
            {
                ShowError("请输入 API 基础 URL");
                return false;
            }

            // 验证 API 密钥
            if (string.IsNullOrWhiteSpace(_apiKeyTextBox?.Text))
            {
                ShowError("请输入 API 密钥");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取配置对象
        /// </summary>
        public AIConfiguration GetConfiguration()
        {
            var config = _originalConfig ?? new AIConfiguration();

            config.Name = _nameTextBox?.Text?.Trim() ?? "";
            config.Provider = (AIProvider)(_providerComboBox?.SelectedIndex ?? 0);
            config.BaseURL = _baseURLTextBox?.Text?.Trim() ?? "";
            config.ApiKey = _apiKeyTextBox?.Text?.Trim() ?? "";
            config.Model = _modelTextBox?.Text?.Trim() ?? "";
            config.LastUsed = DateTime.Now;

            return config;
        }

        /// <summary>
        /// 显示错误信息
        /// </summary>
        private void ShowError(string message)
        {
            if (_errorText != null)
            {
                _errorText.Text = message;
                _errorText.IsVisible = true;
            }
        }
    }
}

