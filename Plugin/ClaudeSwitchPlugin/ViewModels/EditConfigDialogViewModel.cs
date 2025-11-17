using Avalonia.Controls;
using ClaudeSwitchPlugin.Models;
using ClaudeSwitchPlugin.Services;
using ClaudeSwitchPlugin.Views;
using ReactiveUI;
using System;
using System.Reactive;
using System.Windows.Input;

namespace ClaudeSwitchPlugin.ViewModels
{
    /// <summary>
    /// 编辑配置对话框 ViewModel
    /// </summary>
    public class EditConfigDialogViewModel : ReactiveObject
    {
        private readonly ConfigurationService _configService;
        private readonly EditConfigDialog _dialog;
        private readonly AIConfiguration? _originalConfig;
        private bool _isSaved = false;

        private string _name = "";
        private int _selectedProviderIndex = 0;
        private string _baseURL = "";
        private string _apiKey = "";
        private string _model = "";

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public int SelectedProviderIndex
        {
            get => _selectedProviderIndex;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedProviderIndex, value);
                // 根据提供商设置默认 URL
                if (string.IsNullOrEmpty(BaseURL))
                {
                    BaseURL = GetDefaultBaseURL((AIProvider)value);
                }
            }
        }

        public string BaseURL
        {
            get => _baseURL;
            set => this.RaiseAndSetIfChanged(ref _baseURL, value);
        }

        public string ApiKey
        {
            get => _apiKey;
            set => this.RaiseAndSetIfChanged(ref _apiKey, value);
        }

        public string Model
        {
            get => _model;
            set => this.RaiseAndSetIfChanged(ref _model, value);
        }

        public string Subtitle => _originalConfig != null ? "编辑现有配置" : "添加新配置";

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditConfigDialogViewModel(ConfigurationService configService, EditConfigDialog dialog, AIConfiguration? originalConfig = null)
        {
            _configService = configService;
            _dialog = dialog;
            _originalConfig = originalConfig;

            // 如果是编辑模式，填充现有数据
            if (_originalConfig != null)
            {
                Name = _originalConfig.Name;
                SelectedProviderIndex = (int)_originalConfig.Provider;
                BaseURL = _originalConfig.BaseURL;
                ApiKey = _originalConfig.ApiKey;
                Model = _originalConfig.Model;
            }

            SaveCommand = ReactiveCommand.Create(SaveConfiguration);
            CancelCommand = ReactiveCommand.Create(CancelAction);
        }

        /// <summary>
        /// 获取默认基础URL
        /// </summary>
        private string GetDefaultBaseURL(AIProvider provider)
        {
            return provider switch
            {
                AIProvider.Claude => "https://api.anthropic.com",
                AIProvider.OpenAI => "https://api.openai.com",
                _ => ""
            };
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private void SaveConfiguration()
        {
            try
            {
                // 验证必填字段
                if (string.IsNullOrWhiteSpace(Name))
                {
                    ShowError("请输入配置名称");
                    return;
                }

                if (string.IsNullOrWhiteSpace(BaseURL))
                {
                    ShowError("请输入 API 基础 URL");
                    return;
                }

                if (string.IsNullOrWhiteSpace(ApiKey))
                {
                    ShowError("请输入 API 密钥");
                    return;
                }

                // 创建或更新配置
                var config = _originalConfig ?? new AIConfiguration();
                config.Name = Name.Trim();
                config.Provider = (AIProvider)SelectedProviderIndex;
                config.BaseURL = BaseURL.Trim();
                config.ApiKey = ApiKey.Trim();
                config.Model = Model.Trim();
                config.LastUsed = DateTime.Now;

                // 保存配置
                _configService.SaveConfiguration(config);
                _isSaved = true;
            }
            catch (Exception ex)
            {
                ShowError($"保存配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取保存状态
        /// </summary>
        public bool IsSaved => _isSaved;

        /// <summary>
        /// 取消动作
        /// </summary>
        private void CancelAction()
        {
            // 取消操作，不保存
            _isSaved = false;
        }

        /// <summary>
        /// 显示错误信息
        /// </summary>
        private void ShowError(string message)
        {
            // 这里可以显示更好的错误提示，比如 Toast 或 MessageBox
            Console.WriteLine($"错误: {message}");
            // TODO: 实现更好的错误显示方式
        }
    }
}