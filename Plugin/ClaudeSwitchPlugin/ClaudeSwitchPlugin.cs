using ClaudeSwitchPlugin.Models;
using ClaudeSwitchPlugin.Views;
using MicroDock.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ClaudeSwitchPlugin
{
    /// <summary>
    /// Claude 配置切换插件
    /// </summary>
    public class ClaudeSwitchPlugin : BaseMicroDockPlugin
    {
        private string _dataFolder = string.Empty;
        private List<AIConfiguration> _configurations = new();
        private ClaudeSwitchTabView? _mainTabView;

        /// <summary>
        /// JSON 序列化选项
        /// </summary>
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public override IMicroTab[] Tabs
        {
            get
            {
                if (_mainTabView == null)
                {
                    _mainTabView = new ClaudeSwitchTabView(this);
                }
                return new IMicroTab[] { _mainTabView };
            }
        }

        public override void OnInit()
        {
            base.OnInit();
            LogInfo("Claude 配置切换插件初始化中...");

            // 初始化数据文件夹路径
            _dataFolder = Context?.DataPath ?? string.Empty;
            if (string.IsNullOrEmpty(_dataFolder))
            {
                LogError("无法获取插件数据文件夹路径");
                return;
            }

            // 确保数据文件夹存在
            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
                LogInfo($"创建数据文件夹: {_dataFolder}");
            }

            // 加载配置数据
            LoadConfigurationsFromFile();
            LogInfo($"已加载 {_configurations.Count} 个 AI 配置");
        }

        public override void OnEnable()
        {
            base.OnEnable();
            LogInfo("Claude 配置切换插件已启用");
        }

        public override void OnDisable()
        {
            base.OnDisable();
            LogInfo("Claude 配置切换插件已禁用");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogInfo("Claude 配置切换插件已销毁");
        }

        #region 数据管理

        /// <summary>
        /// 从文件加载配置列表
        /// </summary>
        private void LoadConfigurationsFromFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "ai_configurations.json");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    _configurations = JsonSerializer.Deserialize<List<AIConfiguration>>(json, _jsonOptions) ?? new List<AIConfiguration>();
                    LogInfo($"从文件加载了 {_configurations.Count} 个配置");
                }
                else
                {
                    _configurations = new List<AIConfiguration>();
                    LogInfo("配置文件不存在，使用空列表");
                }
            }
            catch (Exception ex)
            {
                LogError("从文件加载配置列表失败", ex);
                _configurations = new List<AIConfiguration>();
            }
        }

        /// <summary>
        /// 保存配置列表到文件
        /// </summary>
        private void SaveConfigurationsToFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "ai_configurations.json");
                string json = JsonSerializer.Serialize(_configurations, _jsonOptions);
                File.WriteAllText(filePath, json);
                LogInfo("配置列表已保存到文件");
            }
            catch (Exception ex)
            {
                LogError("保存配置列表到文件失败", ex);
            }
        }

        /// <summary>
        /// 获取所有配置
        /// </summary>
        public List<AIConfiguration> GetConfigurations()
        {
            return new List<AIConfiguration>(_configurations);
        }

        /// <summary>
        /// 根据提供商获取配置
        /// </summary>
        public List<AIConfiguration> GetConfigurationsByProvider(AIProvider provider)
        {
            return _configurations.Where(c => c.Provider == provider).ToList();
        }

        /// <summary>
        /// 根据ID获取配置
        /// </summary>
        public AIConfiguration? GetConfiguration(string id)
        {
            return _configurations.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// 获取当前活跃的配置
        /// </summary>
        public AIConfiguration? GetActiveConfiguration(AIProvider provider)
        {
            return _configurations.FirstOrDefault(c => c.Provider == provider && c.IsActive);
        }

        /// <summary>
        /// 添加或更新配置
        /// </summary>
        public void SaveConfiguration(AIConfiguration configuration)
        {
            try
            {
                // 检查是否已存在相同ID的配置
                var existingConfig = _configurations.FirstOrDefault(c => c.Id == configuration.Id);
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
                    _configurations.Add(configuration);
                }

                // 保存到文件
                SaveConfigurationsToFile();
                LogInfo($"已保存配置: {configuration.Name}");
            }
            catch (Exception ex)
            {
                LogError("保存配置失败", ex);
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
                var configToRemove = _configurations.FirstOrDefault(c => c.Id == id);
                if (configToRemove != null)
                {
                    _configurations.Remove(configToRemove);
                    SaveConfigurationsToFile();
                    LogInfo($"已删除配置: {configToRemove.Name}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogError("删除配置失败", ex);
                return false;
            }
        }

        /// <summary>
        /// 应用配置
        /// </summary>
        public void ApplyConfiguration(AIConfiguration configuration)
        {
            try
            {
                // 清除该提供商的所有活跃状态
                foreach (var config in _configurations.Where(c => c.Provider == configuration.Provider))
                {
                    config.IsActive = false;
                }

                // 设置新的活跃配置
                var activeConfig = _configurations.FirstOrDefault(c => c.Id == configuration.Id);
                if (activeConfig != null)
                {
                    activeConfig.IsActive = true;
                    activeConfig.LastUsed = DateTime.Now;
                }

                SaveConfigurationsToFile();

                // 根据提供商应用到对应的配置文件
                ApplyConfigurationToSystem(configuration);

                LogInfo($"已应用配置: {configuration.Name} ({configuration.ProviderDisplayName})");
            }
            catch (Exception ex)
            {
                LogError("应用配置失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 应用配置到系统（根据提供商分发到对应的处理方法）
        /// </summary>
        private void ApplyConfigurationToSystem(AIConfiguration configuration)
        {
            try
            {
                switch (configuration.Provider)
                {
                    case Models.AIProvider.Claude:
                        ApplyClaudeConfiguration(configuration);
                        break;

                    case Models.AIProvider.OpenAI:
                        ApplyOpenAIConfiguration(configuration);
                        break;

                    default:
                        LogWarning($"未知的 AI 提供商: {configuration.Provider}，跳过系统配置应用");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogError($"应用配置到系统失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 应用 Claude 配置到 settings.json
        /// </summary>
        private void ApplyClaudeConfiguration(AIConfiguration configuration)
        {
            try
            {
                // Claude 配置文件路径
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string claudeSettingsPath = Path.Combine(userProfile, ".claude", "settings.json");

                // 确保目录存在
                string? claudeDir = Path.GetDirectoryName(claudeSettingsPath);
                if (!string.IsNullOrEmpty(claudeDir) && !Directory.Exists(claudeDir))
                {
                    Directory.CreateDirectory(claudeDir);
                    LogInfo($"创建 Claude 配置目录: {claudeDir}");
                }

                // 读取现有配置以保留其他设置
                JsonDocument? existingSettings = null;
                if (File.Exists(claudeSettingsPath))
                {
                    try
                    {
                        string existingJson = File.ReadAllText(claudeSettingsPath);
                        existingSettings = JsonDocument.Parse(existingJson);
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"读取现有 Claude 配置失败，将创建新配置: {ex.Message}");
                    }
                }

                // 构建新的配置
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

                writer.WriteStartObject();

                // 写入 env 配置
                writer.WritePropertyName("env");
                writer.WriteStartObject();

                // Claude API 配置
                writer.WriteString("ANTHROPIC_BASE_URL", configuration.BaseURL);
                writer.WriteString("ANTHROPIC_AUTH_TOKEN", configuration.ApiKey);
                writer.WriteString("API_TIMEOUT_MS", "3000000");
                writer.WriteNumber("CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC", 1);

                // 如果指定了模型，设置模型相关配置
                if (!string.IsNullOrWhiteSpace(configuration.Model))
                {
                    writer.WriteString("ANTHROPIC_MODEL", configuration.Model);
                    writer.WriteString("ANTHROPIC_SMALL_FAST_MODEL", configuration.Model);
                    writer.WriteString("ANTHROPIC_DEFAULT_SONNET_MODEL", configuration.Model);
                    writer.WriteString("ANTHROPIC_DEFAULT_OPUS_MODEL", configuration.Model);
                    writer.WriteString("ANTHROPIC_DEFAULT_HAIKU_MODEL", configuration.Model);
                }

                writer.WriteEndObject(); // env

                // 保留其他配置（如果存在）
                if (existingSettings != null && existingSettings.RootElement.TryGetProperty("outputStyle", out var outputStyle))
                {
                    writer.WriteString("outputStyle", outputStyle.GetString());
                }
                else
                {
                    writer.WriteString("outputStyle", "engineer-professional");
                }

                if (existingSettings != null && existingSettings.RootElement.TryGetProperty("alwaysThinkingEnabled", out var alwaysThinking))
                {
                    writer.WriteBoolean("alwaysThinkingEnabled", alwaysThinking.GetBoolean());
                }
                else
                {
                    writer.WriteBoolean("alwaysThinkingEnabled", true);
                }

                writer.WriteEndObject(); // root

                writer.Flush();

                // 写入文件
                string newJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
                File.WriteAllText(claudeSettingsPath, newJson);

                LogInfo($"已更新 Claude 配置文件: {claudeSettingsPath}");

                existingSettings?.Dispose();
            }
            catch (Exception ex)
            {
                LogError($"应用 Claude 配置失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 应用 OpenAI 配置（预留，根据实际需求实现）
        /// </summary>
        private void ApplyOpenAIConfiguration(AIConfiguration configuration)
        {
            try
            {
                // 方案 1: 如果 OpenAI 也使用 Claude settings.json
                // 可以将 OpenAI 配置写入到同一个文件的不同字段

                // 方案 2: 如果 OpenAI 需要单独的配置文件
                // string openaiConfigPath = Path.Combine(userProfile, ".openai", "config.json");

                // 方案 3: 如果 OpenAI 通过环境变量配置
                // Environment.SetEnvironmentVariable("OPENAI_API_KEY", configuration.ApiKey, EnvironmentVariableTarget.User);

                // 这里提供一个示例实现：写入环境变量
                LogInfo($"应用 OpenAI 配置: {configuration.Name}");
                
                // 示例：设置环境变量（需要管理员权限或用户级别）
                // Environment.SetEnvironmentVariable("OPENAI_API_KEY", configuration.ApiKey, EnvironmentVariableTarget.User);
                // Environment.SetEnvironmentVariable("OPENAI_API_BASE", configuration.BaseURL, EnvironmentVariableTarget.User);
                
                // 或者写入到特定配置文件
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string openaiConfigPath = Path.Combine(userProfile, ".openai", "config.json");
                
                string? openaiDir = Path.GetDirectoryName(openaiConfigPath);
                if (!string.IsNullOrEmpty(openaiDir) && !Directory.Exists(openaiDir))
                {
                    Directory.CreateDirectory(openaiDir);
                }

                // 构建 OpenAI 配置
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

                writer.WriteStartObject();
                writer.WriteString("api_key", configuration.ApiKey);
                writer.WriteString("api_base", configuration.BaseURL);
                if (!string.IsNullOrWhiteSpace(configuration.Model))
                {
                    writer.WriteString("model", configuration.Model);
                }
                writer.WriteEndObject();

                writer.Flush();

                string newJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
                File.WriteAllText(openaiConfigPath, newJson);

                LogInfo($"已更新 OpenAI 配置文件: {openaiConfigPath}");
            }
            catch (Exception ex)
            {
                LogError($"应用 OpenAI 配置失败: {ex.Message}", ex);
                throw;
            }
        }

        // 未来扩展示例：
        // /// <summary>
        // /// 应用 Google Gemini 配置
        // /// </summary>
        // private void ApplyGeminiConfiguration(AIConfiguration configuration)
        // {
        //     // 实现 Gemini 特定的配置应用逻辑
        // }
        //
        // /// <summary>
        // /// 应用 Azure OpenAI 配置
        // /// </summary>
        // private void ApplyAzureOpenAIConfiguration(AIConfiguration configuration)
        // {
        //     // 实现 Azure OpenAI 特定的配置应用逻辑
        // }

        /// <summary>
        /// 搜索配置
        /// </summary>
        public List<AIConfiguration> SearchConfigurations(AIProvider provider, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return GetConfigurationsByProvider(provider);
            }

            return _configurations
                .Where(c => c.Provider == provider &&
                           (c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            c.BaseURL.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            c.Model.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        #endregion
    }
}