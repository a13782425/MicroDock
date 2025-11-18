#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
安全服务模块
负责SHA256哈希计算和文件完整性验证
"""

import hashlib
import os
from pathlib import Path
from typing import Optional

class SecurityService:
    """安全服务类"""

    @staticmethod
    def calculate_sha256(text: str) -> str:
        """
        计算文本的SHA256哈希值

        Args:
            text: 要计算哈希的文本

        Returns:
            SHA256哈希字符串
        """
        return hashlib.sha256(text.encode('utf-8')).hexdigest()

    @staticmethod
    def calculate_file_sha256(file_path: Path) -> str:
        """
        计算文件的SHA256哈希值

        Args:
            file_path: 文件路径

        Returns:
            文件的SHA256哈希字符串
        """
        sha256_hash = hashlib.sha256()

        try:
            with open(file_path, "rb") as f:
                # 分块读取文件，避免大文件内存溢出
                for chunk in iter(lambda: f.read(4096), b""):
                    sha256_hash.update(chunk)
            return sha256_hash.hexdigest()
        except Exception as e:
            raise ValueError(f"计算文件哈希失败: {e}")

    @staticmethod
    def verify_file_integrity(file_path: Path, expected_hash: str) -> bool:
        """
        验证文件完整性

        Args:
            file_path: 文件路径
            expected_hash: 期望的哈希值

        Returns:
            文件完整性是否有效
        """
        if not file_path.exists():
            return False

        try:
            actual_hash = SecurityService.calculate_file_sha256(file_path)
            return actual_hash.lower() == expected_hash.lower()
        except Exception:
            return False

    @staticmethod
    def generate_backup_index(user_key: str, backup_name: str, timestamp: str) -> str:
        """
        生成备份索引

        Args:
            user_key: 用户自定义key
            backup_name: 备份名称
            timestamp: 时间戳

        Returns:
            备份索引哈希值
        """
        index_data = f"{user_key}:{backup_name}:{timestamp}"
        return SecurityService.calculate_sha256(index_data)

    @staticmethod
    def validate_access_key(access_key: str) -> bool:
        """
        验证访问密钥格式

        Args:
            access_key: 访问密钥

        Returns:
            密钥格式是否有效
        """
        if not access_key or len(access_key) < 8:
            return False

        # 可以添加更多验证规则，如特殊字符要求等
        return True

    @staticmethod
    def sanitize_filename(filename: str) -> str:
        """
        清理文件名，移除不安全字符

        Args:
            filename: 原始文件名

        Returns:
            清理后的安全文件名
        """
        # 移除危险字符
        dangerous_chars = ['<', '>', ':', '"', '|', '?', '*', '/', '\\']
        safe_name = filename

        for char in dangerous_chars:
            safe_name = safe_name.replace(char, '_')

        # 限制长度
        if len(safe_name) > 255:
            name, ext = os.path.splitext(safe_name)
            safe_name = name[:255-len(ext)] + ext

        return safe_name