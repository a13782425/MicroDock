using System;

namespace ClaudeSwitchPlugin.Models
{
    /// <summary>
    /// AI 配置数据模型
    /// </summary>
    public class AIConfiguration
    {
        /// <summary>
        /// 配置唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 配置显示名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// AI 提供商
        /// </summary>
        public AIProvider Provider { get; set; }

        /// <summary>
        /// API 基础 URL
        /// </summary>
        public string BaseURL { get; set; } = "";

        /// <summary>
        /// API 密钥
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// 模型名称
        /// </summary>
        public string Model { get; set; } = "";

        /// <summary>
        /// 最后使用时间
        /// </summary>
        public DateTime LastUsed { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否为当前活跃配置
        /// </summary>
        public bool IsActive { get; set; } = false;

        /// <summary>
        /// 获取脱敏显示的 API Key
        /// </summary>
        public string MaskedApiKey => string.IsNullOrEmpty(ApiKey) ? "" :
            ApiKey.Length > 8 ? $"{ApiKey.Substring(0, 8)}***" : "***";

        /// <summary>
        /// 获取提供商显示名称
        /// </summary>
        public string ProviderDisplayName => Provider switch
        {
            AIProvider.Claude => "Claude",
            AIProvider.OpenAI => "OpenAI",
            _ => "未知"
        };

        /// <summary>
        /// 获取提供商标识
        /// </summary>
        public string ProviderIcon => Provider switch
        {
            AIProvider.Claude => "C",
            AIProvider.OpenAI => "O",
            _ => "?"
        };
    }
}