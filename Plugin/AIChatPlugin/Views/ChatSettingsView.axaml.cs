using AIChatPlugin.Models;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MicroDock.Plugin;

namespace AIChatPlugin.Views
{
    /// <summary>
    /// AI 配置设置视图
    /// </summary>
    public partial class ChatSettingsView : UserControl
    {
        private readonly AIChatPlugin _plugin;
        private TextBox? _apiKeyTextBox;
        private TextBox? _baseUrlTextBox;
        private TextBox? _modelTextBox;
        private Avalonia.Controls.Slider? _temperatureSlider;
        private TextBox? _maxTokensTextBox;

        public ChatSettingsView(AIChatPlugin plugin)
        {
            _plugin = plugin;
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _apiKeyTextBox = this.FindControl<TextBox>("ApiKeyTextBox");
            _baseUrlTextBox = this.FindControl<TextBox>("BaseUrlTextBox");
            _modelTextBox = this.FindControl<TextBox>("ModelTextBox");
            _temperatureSlider = this.FindControl<Avalonia.Controls.Slider>("TemperatureSlider");
            _maxTokensTextBox = this.FindControl<TextBox>("MaxTokensTextBox");
        }

        private void LoadSettings()
        {
            ChatConfig config = _plugin.GetConfig();
            if (_apiKeyTextBox != null) _apiKeyTextBox.Text = config.ApiKey;
            if (_baseUrlTextBox != null) _baseUrlTextBox.Text = config.BaseUrl;
            if (_modelTextBox != null) _modelTextBox.Text = config.Model;
            if (_temperatureSlider != null) _temperatureSlider.Value = config.Temperature;
            if (_maxTokensTextBox != null) _maxTokensTextBox.Text = config.MaxTokens.ToString();
        }

        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            // 验证配置
            if (string.IsNullOrWhiteSpace(_apiKeyTextBox?.Text))
            {
                ShowError("API Key 不能为空");
                return;
            }

            if (string.IsNullOrWhiteSpace(_baseUrlTextBox?.Text))
            {
                ShowError("Base URL 不能为空");
                return;
            }

            if (string.IsNullOrWhiteSpace(_modelTextBox?.Text))
            {
                ShowError("模型名称不能为空");
                return;
            }

            // 解析 MaxTokens
            if (!int.TryParse(_maxTokensTextBox?.Text, out int maxTokens) || maxTokens <= 0)
            {
                ShowError("Max Tokens 必须是大于 0 的整数");
                return;
            }

            ChatConfig config = new ChatConfig
            {
                ApiKey = _apiKeyTextBox.Text,
                BaseUrl = _baseUrlTextBox.Text.TrimEnd('/'),
                Model = _modelTextBox.Text,
                Temperature = _temperatureSlider?.Value ?? 0.7,
                MaxTokens = maxTokens
            };

            _plugin.UpdateConfig(config);
            
            // 显示成功提示
            _plugin.Context!.ShowInAppNotification("配置已保存", "AI 配置已成功保存", 
                NotificationType.Success);
        }

        private void ShowError(string message)
        {
            _plugin.Context!.ShowInAppNotification("配置错误", message, NotificationType.Error);
        }
    }
}


