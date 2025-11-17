using ClaudeSwitchPlugin.Models;
using ClaudeSwitchPlugin.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ClaudeSwitchPlugin.ViewModels
{
    /// <summary>
    /// OpenAI 配置页签 ViewModel
    /// </summary>
    public class OpenAITabViewModel : ReactiveObject
    {
        private readonly ConfigurationService _configService;
        private string _searchText = "";
        private ConfigurationCardViewModel? _activeConfiguration;

        public string SearchText
        {
            get => _searchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
                RefreshConfigurations();
            }
        }

        public ObservableCollection<ConfigurationCardViewModel> Configurations { get; } = new();

        public ConfigurationCardViewModel? ActiveConfiguration
        {
            get => _activeConfiguration;
            set => this.RaiseAndSetIfChanged(ref _activeConfiguration, value);
        }

        public event Action? AddConfigurationRequested;

        public OpenAITabViewModel(ConfigurationService configService)
        {
            _configService = configService;
            RefreshConfigurations();
        }

        /// <summary>
        /// 刷新配置列表
        /// </summary>
        public void RefreshConfigurations()
        {
            try
            {
                Configurations.Clear();

                var configs = string.IsNullOrWhiteSpace(SearchText)
                    ? _configService.GetConfigurationsByProvider(AIProvider.OpenAI)
                    : _configService.SearchConfigurations(AIProvider.OpenAI, SearchText);

                foreach (var config in configs)
                {
                    var cardViewModel = new ConfigurationCardViewModel(_configService, config);

                    // 订阅事件
                    cardViewModel.ApplyRequested += OnConfigurationApplied;
                    cardViewModel.EditRequested += OnConfigurationEditRequested;
                    cardViewModel.DeleteRequested += OnConfigurationDeleteRequested;

                    Configurations.Add(cardViewModel);

                    if (config.IsActive)
                    {
                        ActiveConfiguration = cardViewModel;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"刷新OpenAI配置列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加新配置
        /// </summary>
        public void AddConfiguration()
        {
            AddConfigurationRequested?.Invoke();
        }

        /// <summary>
        /// 配置应用事件处理
        /// </summary>
        private void OnConfigurationApplied(ConfigurationCardViewModel appliedCard)
        {
            try
            {
                // 更新所有卡片的状态
                foreach (var card in Configurations)
                {
                    card.IsActive = (card.Configuration.Id == appliedCard.Configuration.Id);
                }

                ActiveConfiguration = appliedCard;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理配置应用事件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 配置编辑事件处理
        /// </summary>
        private void OnConfigurationEditRequested(ConfigurationCardViewModel editCard)
        {
            // 这里可以打开编辑对话框
            Console.WriteLine($"编辑配置: {editCard.Configuration.Name}");
            AddConfigurationRequested?.Invoke(); // 临时处理，应该传递编辑的配置
        }

        /// <summary>
        /// 配置删除事件处理
        /// </summary>
        private void OnConfigurationDeleteRequested(ConfigurationCardViewModel deleteCard)
        {
            try
            {
                if (deleteCard.IsActive)
                {
                    ActiveConfiguration = null;
                }

                Configurations.Remove(deleteCard);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理配置删除事件失败: {ex.Message}");
            }
        }
    }
}