using System;

namespace RemoteDesktopPlugin.Models
{
    /// <summary>
    /// 远程桌面连接模型
    /// </summary>
    public class RemoteConnection
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 连接名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 远程主机地址
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// 端口号（默认3389）
        /// </summary>
        public int Port { get; set; } = 3389;

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码（加密存储）
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 域名（可选）
        /// </summary>
        public string? Domain { get; set; }

        /// <summary>
        /// 分组名称
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// 描述信息
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 最后连接时间
        /// </summary>
        public DateTime LastConnected { get; set; } = DateTime.Now;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 获取连接字符串（用于日志显示）
        /// </summary>
        public string ConnectionString
        {
            get
            {
                if (Port == 3389)
                    return Host;
                else
                    return $"{Host}:{Port}";
            }
        }

        /// <summary>
        /// 获取完整的RDP连接地址
        /// </summary>
        public string FullAddress => Port == 3389 ? Host : $"{Host}:{Port}";
    }
}