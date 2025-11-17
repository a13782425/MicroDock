using Avalonia.Controls;
using ClaudeSwitchPlugin.Models;
using ClaudeSwitchPlugin.Services;
using ReactiveUI;
using System;
using System.Reactive;
using System.Windows.Input;

namespace ClaudeSwitchPlugin.ViewModels
{
    /// <summary>
    /// 配置卡片 ViewModel
    /// </summary>
    public class ConfigurationCardViewModel : ReactiveObject
    {
        private readonly ConfigurationService _configService;
        private AIConfiguration _configuration;
        private bool _isActive;

        public AIConfiguration Configuration
        {
            get => _configuration;
            set => this.RaiseAndSetIfChanged(ref _configuration, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => this.RaiseAndSetIfChanged(ref _isActive, value);
        }

        public string Name => Configuration.Name;
        public string ProviderIcon => Configuration.ProviderIcon;
        public string BaseURL => Configuration.BaseURL;
        public string MaskedApiKey => Configuration.MaskedApiKey;
        public string Model => Configuration.Model;
        public string LastUsedString => Configuration.LastUsed.ToString("yyyy-MM-dd HH:mm");

        public bool CanApply => !IsActive;
        public bool CanEdit => true;
        public bool CanDelete => !IsActive;

        public event Action<ConfigurationCardViewModel>? ApplyRequested;
        public event Action<ConfigurationCardViewModel>? EditRequested;
        public event Action<ConfigurationCardViewModel>? DeleteRequested;

        public ICommand ApplyConfiguration { get; }
        public ICommand EditConfiguration { get; }
        public ICommand DeleteConfiguration { get; }

        public ConfigurationCardViewModel(ConfigurationService configService, AIConfiguration configuration)
        {
            _configService = configService;
            Configuration = configuration;
            IsActive = configuration.IsActive;

            ApplyConfiguration = ReactiveCommand.Create(ApplyConfigurationAction);
            EditConfiguration = ReactiveCommand.Create(EditConfigurationAction);
            DeleteConfiguration = ReactiveCommand.Create(DeleteConfigurationAction);
        }

        /// <summary>
        /// 应用配置命令动作
        /// </summary>
        private void ApplyConfigurationAction()
        {
            try
            {
                _configService.ApplyConfiguration(Configuration);
                IsActive = true;
                ApplyRequested?.Invoke(this);
            }
            catch (Exception ex)
            {
                // 这里可以显示错误提示
                Console.WriteLine($"应用配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 编辑配置命令动作
        /// </summary>
        private void EditConfigurationAction()
        {
            EditRequested?.Invoke(this);
        }

        /// <summary>
        /// 删除配置命令动作
        /// </summary>
        private void DeleteConfigurationAction()
        {
            try
            {
                if (_configService.DeleteConfiguration(Configuration.Id))
                {
                    DeleteRequested?.Invoke(this);
                }
            }
            catch (Exception ex)
            {
                // 这里可以显示错误提示
                Console.WriteLine($"删除配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        public void UpdateStatus()
        {
            this.RaisePropertyChanged(nameof(Name));
            this.RaisePropertyChanged(nameof(ProviderIcon));
            this.RaisePropertyChanged(nameof(BaseURL));
            this.RaisePropertyChanged(nameof(MaskedApiKey));
            this.RaisePropertyChanged(nameof(Model));
            this.RaisePropertyChanged(nameof(LastUsedString));
            this.RaisePropertyChanged(nameof(CanApply));
            this.RaisePropertyChanged(nameof(CanDelete));
        }
    }
}