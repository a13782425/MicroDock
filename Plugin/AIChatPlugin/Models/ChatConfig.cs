namespace AIChatPlugin.Models
{
    /// <summary>
    /// AI 配置模型
    /// </summary>
    public class ChatConfig
    {
        /// <summary>
        /// API 密钥
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// API 基础地址
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";

        /// <summary>
        /// 模型名称
        /// </summary>
        public string Model { get; set; } = "gpt-3.5-turbo";

        /// <summary>
        /// 温度参数（0-2）
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// 最大 token 数
        /// </summary>
        public int MaxTokens { get; set; } = 2000;

        /// <summary>
        /// 获取完整的 API URL
        /// </summary>
        public string GetApiUrl()
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                return "https://api.openai.com/v1/chat/completions";
            }

            // 确保 BaseUrl 不以 / 结尾
            string baseUrl = BaseUrl.TrimEnd('/');
            
            // 如果 BaseUrl 已经包含 /chat/completions，直接返回
            if (baseUrl.EndsWith("/chat/completions"))
            {
                return baseUrl;
            }

            // 否则添加 /chat/completions
            return $"{baseUrl}/chat/completions";
        }
    }
}

