using ClaudeSwitchPlugin.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ClaudeSwitchPlugin.Services
{
    /// <summary>
    /// 配置管理服务
    /// </summary>
    public class ConfigurationService
    {
        private readonly string _dataFolder;
        private readonly string _configFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public ConfigurationService(string dataFolder)
        {
            _dataFolder = dataFolder;
            _configFilePath = Path.Combine(dataFolder, "ai_configurations.json");
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            // 确保数据文件夹存在
            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }
        }

        /// <summary>
        /// 获取所有配置
        /// </summary>
        public List<AIConfiguration> GetConfigurations()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    return new List<AIConfiguration>();
                }

                string json = File.ReadAllText(_configFilePath);
                var configurations = JsonSerializer.Deserialize<List<AIConfiguration>>(json, _jsonOptions);
                return configurations ?? new List<AIConfiguration>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取配置文件失败: {ex.Message}");
                return new List<AIConfiguration>();
            }
        }

        /// <summary>
        /// 根据提供商获取配置
        /// </summary>
        public List<AIConfiguration> GetConfigurationsByProvider(AIProvider provider)
        {
            return GetConfigurations().Where(c => c.Provider == provider).ToList();
        }

        /// <summary>
        /// 根据ID获取配置
        /// </summary>
        public AIConfiguration? GetConfiguration(string id)
        {
            return GetConfigurations().FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// 获取当前活跃的配置
        /// </summary>
        public AIConfiguration? GetActiveConfiguration(AIProvider provider)
        {
            return GetConfigurationsByProvider(provider).FirstOrDefault(c => c.IsActive);
        }

        /// <summary>
        /// 添加或更新配置
        /// </summary>
        public void SaveConfiguration(AIConfiguration configuration)
        {
            try
            {
                var configurations = GetConfigurations();

                // 检查是否已存在相同ID的配置
                var existingConfig = configurations.FirstOrDefault(c => c.Id == configuration.Id);
                if (existingConfig != null)
                {
                    // 更新现有配置
                    existingConfig.Name = configuration.Name;
                    existingConfig.Provider = configuration.Provider;
                    existingConfig.BaseURL = configuration.BaseURL;
                    existingConfig.ApiKey = configuration.ApiKey;
                    existingConfig.Model = configuration.Model;
                    existingConfig.LastUsed = configuration.LastUsed;
                    existingConfig.IsActive = configuration.IsActive;
                }
                else
                {
                    // 添加新配置
                    configurations.Add(configuration);
                }

                // 保存到文件
                SaveToFile(configurations);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存配置失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 删除配置
        /// </summary>
        public bool DeleteConfiguration(string id)
        {
            try
            {
                var configurations = GetConfigurations();
                var configToRemove = configurations.FirstOrDefault(c => c.Id == id);

                if (configToRemove != null)
                {
                    configurations.Remove(configToRemove);
                    SaveToFile(configurations);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除配置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置活跃配置
        /// </summary>
        public void SetActiveConfiguration(AIProvider provider, string configId)
        {
            try
            {
                var configurations = GetConfigurations();

                // 清除该提供商的所有活跃状态
                foreach (var config in configurations.Where(c => c.Provider == provider))
                {
                    config.IsActive = false;
                }

                // 设置新的活跃配置
                var activeConfig = configurations.FirstOrDefault(c => c.Id == configId && c.Provider == provider);
                if (activeConfig != null)
                {
                    activeConfig.IsActive = true;
                    activeConfig.LastUsed = DateTime.Now;
                }

                SaveToFile(configurations);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置活跃配置失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 应用配置（模拟应用操作）
        /// </summary>
        public void ApplyConfiguration(AIConfiguration configuration)
        {
            try
            {
                // 设置为活跃配置
                SetActiveConfiguration(configuration.Provider, configuration.Id);

                // 这里可以添加实际的应用配置逻辑，比如：
                // - 设置环境变量
                // - 更新配置文件
                // - 发送通知给其他应用

                Console.WriteLine($"已应用配置: {configuration.Name} ({configuration.ProviderDisplayName})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用配置失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 搜索配置
        /// </summary>
        public List<AIConfiguration> SearchConfigurations(AIProvider provider, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return GetConfigurationsByProvider(provider);
            }

            return GetConfigurationsByProvider(provider)
                .Where(c => c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           c.BaseURL.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           c.Model.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        private void SaveToFile(List<AIConfiguration> configurations)
        {
            try
            {
                string json = JsonSerializer.Serialize(configurations, _jsonOptions);
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存配置文件失败: {ex.Message}");
                throw;
            }
        }
    }
}