using System;
using System.Security.Cryptography;

namespace MicroDock.Services;

public static class IconHashService
{
    /// <summary>
    /// 计算数据的 SHA256 哈希值
    /// </summary>
    public static string ComputeSha256Hash(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return string.Empty;
        }

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(data);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }

    /// <summary>
    /// 验证数据是否匹配给定的哈希值
    /// </summary>
    public static bool VerifyHash(byte[] data, string hash)
    {
        string computedHash = ComputeSha256Hash(data);
        return string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase);
    }
}

