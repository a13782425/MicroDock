using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AIChatPlugin.Services
{
    /// <summary>
    /// Mermaid 图表转图片服务
    /// 使用 Mermaid.Ink API 将 Mermaid 代码转换为 SVG/PNG 图片
    /// </summary>
    public class MermaidToImageService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        /// <summary>
        /// 将 Mermaid 代码转换为图片
        /// </summary>
        /// <param name="mermaidCode">Mermaid 代码</param>
        /// <returns>图片的 Bitmap 对象，失败返回 null</returns>
        public async Task<Bitmap?> ConvertToImageAsync(string mermaidCode)
        {
            if (string.IsNullOrWhiteSpace(mermaidCode))
            {
                return null;
            }

            try
            {
                // 使用 Mermaid.Ink API
                // API 格式: https://mermaid.ink/img/{base64_encoded_mermaid_code}
                string base64Code = Convert.ToBase64String(Encoding.UTF8.GetBytes(mermaidCode));
                string apiUrl = $"https://mermaid.ink/img/{base64Code}";

                // 下载图片
                byte[] imageBytes = await _httpClient.GetByteArrayAsync(apiUrl);

                // 转换为 Avalonia Bitmap
                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    return new Bitmap(ms);
                }
            }
            catch (Exception ex)
            {
                // 记录错误（可以通过插件上下文记录）
                System.Diagnostics.Debug.WriteLine($"Mermaid 转换失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 将 Mermaid 代码转换为 SVG URL
        /// </summary>
        /// <param name="mermaidCode">Mermaid 代码</param>
        /// <returns>SVG 图片的 URL</returns>
        public string ConvertToSvgUrl(string mermaidCode)
        {
            if (string.IsNullOrWhiteSpace(mermaidCode))
            {
                return string.Empty;
            }

            try
            {
                // 使用 Mermaid.Ink API
                string base64Code = Convert.ToBase64String(Encoding.UTF8.GetBytes(mermaidCode));
                return $"https://mermaid.ink/svg/{base64Code}";
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 验证 Mermaid 代码是否有效（简单检查）
        /// </summary>
        public bool IsValidMermaidCode(string mermaidCode)
        {
            if (string.IsNullOrWhiteSpace(mermaidCode))
            {
                return false;
            }

            // 简单检查是否包含 Mermaid 关键字
            string[] keywords = { "graph", "flowchart", "sequenceDiagram", "classDiagram", "stateDiagram", "erDiagram", "gantt", "pie", "journey" };
            string lowerCode = mermaidCode.ToLower();

            foreach (string keyword in keywords)
            {
                if (lowerCode.Contains(keyword.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

