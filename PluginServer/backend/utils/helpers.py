#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
辅助工具函数
提供文件操作、验证等通用功能
"""

import os
from pathlib import Path
from typing import Tuple

def validate_file_type(filename: str) -> bool:
    """
    验证文件类型是否为支持的插件格式

    Args:
        filename: 文件名

    Returns:
        是否为支持的格式
    """
    if not filename:
        return False

    supported_extensions = ['.zip', '.dll']
    file_extension = Path(filename).suffix.lower()
    return file_extension in supported_extensions

def get_safe_filename(filename: str) -> str:
    """
    获取安全的文件名，移除危险字符

    Args:
        filename: 原始文件名

    Returns:
        安全的文件名
    """
    # 移除危险字符
    dangerous_chars = ['<', '>', ':', '"', '|', '?', '*', '/', '\\']
    safe_name = filename

    for char in dangerous_chars:
        safe_name = safe_name.replace(char, '_')

    # 移除控制字符
    safe_name = ''.join(char for char in safe_name if ord(char) >= 32)

    return safe_name

def get_file_size(file_path: Path) -> int:
    """
    获取文件大小

    Args:
        file_path: 文件路径

    Returns:
        文件大小（字节）
    """
    try:
        return file_path.stat().st_size
    except (OSError, FileNotFoundError):
        return 0

def format_file_size(size_bytes: int) -> str:
    """
    格式化文件大小为人类可读格式

    Args:
        size_bytes: 文件大小（字节）

    Returns:
        格式化的文件大小字符串
    """
    if size_bytes == 0:
        return "0 B"

    size_names = ["B", "KB", "MB", "GB", "TB"]
    i = 0
    size = float(size_bytes)

    while size >= 1024.0 and i < len(size_names) - 1:
        size /= 1024.0
        i += 1

    return f"{size:.1f} {size_names[i]}"

def ensure_directory_exists(directory: Path) -> bool:
    """
    确保目录存在，如果不存在则创建

    Args:
        directory: 目录路径

    Returns:
        是否成功创建或已存在
    """
    try:
        directory.mkdir(parents=True, exist_ok=True)
        return True
    except OSError:
        return False

def is_safe_path(base_path: Path, target_path: Path) -> bool:
    """
    检查目标路径是否在基础路径之内（防止路径遍历攻击）

    Args:
        base_path: 基础路径
        target_path: 目标路径

    Returns:
        是否为安全路径
    """
    try:
        target_path.resolve().relative_to(base_path.resolve())
        return True
    except ValueError:
        return False

def extract_plugin_metadata(file_path: Path) -> dict:
    """
    从插件文件中提取元数据

    Args:
        file_path: 插件文件路径

    Returns:
        插件元数据字典
    """
    import json
    import zipfile

    metadata = {
        'name': file_path.stem,
        'version': '1.0.0',
        'description': '未知插件',
        'author': '未知',
        'plugin_type': 'storage',
        'dependencies': [],
        'config': {}
    }

    if file_path.suffix.lower() == '.zip':
        try:
            with zipfile.ZipFile(file_path, 'r') as zip_file:
                # 查找根目录的plugin.json
                for file_info in zip_file.filelist:
                    if (file_info.filename.endswith('plugin.json') and
                        '/' not in file_info.filename.replace('\\', '/')):
                        with zip_file.open(file_info) as json_file:
                            zip_metadata = json.loads(json_file.read().decode('utf-8'))
                            metadata.update(zip_metadata)
                        break
        except Exception as e:
            print(f"读取ZIP插件元数据失败: {e}")

    return metadata

def create_backup_filename(original_name: str, timestamp: str = None) -> str:
    """
    创建备份文件名

    Args:
        original_name: 原始文件名
        timestamp: 时间戳（可选）

    Returns:
        备份文件名
    """
    from datetime import datetime

    if not timestamp:
        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')

    name, ext = os.path.splitext(original_name)
    return f"{name}_{timestamp}{ext}"