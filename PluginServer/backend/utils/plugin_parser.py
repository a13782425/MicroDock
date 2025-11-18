#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
插件解析工具模块
用于从ZIP文件中提取plugin.json元数据
"""

import os
import json
import zipfile
import tempfile
from typing import Dict, Optional, Tuple, List
from pathlib import Path
import logging

logger = logging.getLogger(__name__)

class PluginParseError(Exception):
    """插件解析异常"""
    pass

class PluginValidationError(PluginParseError):
    """插件验证异常"""
    pass

class PluginParser:
    """插件解析器"""

    # 必需的plugin.json字段
    REQUIRED_FIELDS = ['name', 'displayName', 'version']

    # 可选的plugin.json字段
    OPTIONAL_FIELDS = [
        'description', 'author', 'license', 'homepage',
        'keywords', 'entry', 'dependencies', 'minAppVersion', 'category'
    ]

    # 允许的插件文件扩展名
    ALLOWED_EXTENSIONS = {'.dll', '.exe', '.so', '.dylib', '.json', '.md', '.txt'}

    # 最大文件大小 (50MB)
    MAX_FILE_SIZE = 50 * 1024 * 1024

    def __init__(self):
        """初始化插件解析器"""
        self.temp_dir = None

    def __enter__(self):
        """上下文管理器入口"""
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """上下文管理器出口，清理临时文件"""
        self.cleanup()

    def cleanup(self):
        """清理临时文件"""
        if self.temp_dir and os.path.exists(self.temp_dir):
            try:
                import shutil
                shutil.rmtree(self.temp_dir)
                logger.info(f"已清理临时目录: {self.temp_dir}")
            except Exception as e:
                logger.warning(f"清理临时目录失败: {e}")

    def validate_zip_file(self, file_path: str) -> bool:
        """验证ZIP文件格式和安全性"""
        try:
            # 检查文件大小
            file_size = os.path.getsize(file_path)
            if file_size > self.MAX_FILE_SIZE:
                raise PluginValidationError(f"文件过大: {file_size / (1024*1024):.1f}MB，最大允许: {self.MAX_FILE_SIZE / (1024*1024)}MB")

            # 检查ZIP文件格式
            with zipfile.ZipFile(file_path, 'r') as zip_ref:
                # 检查ZIP文件是否加密（密码保护）
                if zip_ref.comment:
                    logger.warning("ZIP文件包含注释，可能存在安全风险")

                # 检查文件列表
                file_list = zip_ref.namelist()
                if not file_list:
                    raise PluginValidationError("ZIP文件为空")

                # 检查危险文件路径
                for file_name in file_list:
                    # 防止路径遍历攻击
                    if '..' in file_name or file_name.startswith('/'):
                        raise PluginValidationError(f"检测到危险文件路径: {file_name}")

                    # 检查文件扩展名
                    file_ext = Path(file_name).suffix.lower()
                    if file_ext and file_ext not in self.ALLOWED_EXTENSIONS and file_ext != '.json':
                        logger.warning(f"文件扩展名可能不受支持: {file_ext}")

                # 尝试解压测试（检查文件是否损坏）
                zip_ref.testzip()

            return True

        except zipfile.BadZipFile:
            raise PluginValidationError("不是有效的ZIP文件格式")
        except Exception as e:
            raise PluginValidationError(f"ZIP文件验证失败: {str(e)}")

    def extract_plugin_metadata(self, file_path: str) -> Dict:
        """从ZIP文件中提取插件元数据"""
        try:
            # 首先验证ZIP文件
            self.validate_zip_file(file_path)

            # 创建临时目录
            self.temp_dir = tempfile.mkdtemp(prefix="plugin_extract_")

            # 解压ZIP文件并获取文件列表
            with zipfile.ZipFile(file_path, 'r') as zip_ref:
                zip_ref.extractall(self.temp_dir)
                file_count = len(zip_ref.namelist())

            # 查找plugin.json文件
            plugin_json_path = self._find_plugin_json(self.temp_dir)
            if not plugin_json_path:
                raise PluginParseError("未找到plugin.json配置文件")

            # 读取和解析plugin.json
            with open(plugin_json_path, 'r', encoding='utf-8') as f:
                try:
                    plugin_data = json.load(f)
                except json.JSONDecodeError as e:
                    raise PluginParseError(f"plugin.json格式错误: {str(e)}")

            # 验证插件数据
            self._validate_plugin_data(plugin_data)

            # 添加额外元数据
            plugin_data['file_size'] = os.path.getsize(file_path)
            plugin_data['file_count'] = file_count
            plugin_data['extracted_path'] = self.temp_dir

            logger.info(f"成功解析插件: {plugin_data.get('displayName', plugin_data.get('name'))}")
            return plugin_data

        except PluginParseError:
            raise
        except Exception as e:
            raise PluginParseError(f"提取插件元数据失败: {str(e)}")

    def _find_plugin_json(self, extract_dir: str) -> Optional[str]:
        """在解压目录中查找plugin.json文件"""
        # 常见的plugin.json位置
        possible_paths = [
            os.path.join(extract_dir, 'plugin.json'),
            os.path.join(extract_dir, 'config', 'plugin.json'),
            os.path.join(extract_dir, 'meta', 'plugin.json'),
        ]

        for path in possible_paths:
            if os.path.isfile(path):
                return path

        # 递归搜索（限制深度）
        for root, dirs, files in os.walk(extract_dir):
            # 限制搜索深度为3层
            level = root[len(extract_dir):].count(os.sep)
            if level >= 3:
                dirs[:] = []  # 不再深入子目录
                continue

            if 'plugin.json' in files:
                return os.path.join(root, 'plugin.json')

        return None

    def _validate_plugin_data(self, plugin_data: Dict) -> None:
        """验证插件数据的完整性"""
        if not isinstance(plugin_data, dict):
            raise PluginValidationError("plugin.json必须是一个JSON对象")

        # 检查必需字段
        missing_fields = []
        for field in self.REQUIRED_FIELDS:
            if field not in plugin_data or not plugin_data[field]:
                missing_fields.append(field)

        if missing_fields:
            raise PluginValidationError(f"缺少必需字段: {', '.join(missing_fields)}")

        # 验证字段格式
        self._validate_field_formats(plugin_data)

        # 检查版本号格式
        self._validate_version_format(plugin_data.get('version'))

        # 检查插件名称格式
        self._validate_plugin_name(plugin_data.get('name'))

    def _validate_field_formats(self, plugin_data: Dict) -> None:
        """验证字段格式"""
        # 名称格式检查
        name = plugin_data.get('name')
        if name and not isinstance(name, str):
            raise PluginValidationError("name字段必须是字符串")

        displayName = plugin_data.get('displayName')
        if displayName and not isinstance(displayName, str):
            raise PluginValidationError("displayName字段必须是字符串")

        # 版本号格式检查
        version = plugin_data.get('version')
        if version and not isinstance(version, str):
            raise PluginValidationError("version字段必须是字符串")

        # 描述长度检查
        description = plugin_data.get('description')
        if description and len(description) > 500:
            raise PluginValidationError("描述信息过长（最多500字符）")

        # 关键词数组检查
        keywords = plugin_data.get('keywords')
        if keywords and not isinstance(keywords, list):
            raise PluginValidationError("keywords字段必须是数组")

    def _validate_version_format(self, version: str) -> None:
        """验证版本号格式（简单的语义版本检查）"""
        if not version:
            return

        # 简单的版本号格式检查 (x.y.z 或 x.y 或 x)
        import re
        version_pattern = r'^\d+(\.\d+)*(\-[a-zA-Z0-9\-]+)?$'
        if not re.match(version_pattern, version):
            raise PluginValidationError(f"无效的版本号格式: {version}，建议使用语义版本号（如 1.0.0）")

    def _validate_plugin_name(self, name: str) -> None:
        """验证插件名称格式"""
        if not name:
            return

        # 插件名称格式检查（支持Java包名格式：字母、数字、点号、连字符、下划线）
        import re
        # 支持格式如：com.microdock.todolist, my-plugin, simple_plugin等
        name_pattern = r'^[a-zA-Z][a-zA-Z0-9\.\-\_]*$'
        # 额外检查：不能以点号、连字符或下划线结尾，且不能有连续的点号、连字符或下划线
        invalid_endings = r'[.\-\_]+$'
        invalid_sequences = r'[.\-\_]{2,}'

        if not re.match(name_pattern, name):
            raise PluginValidationError(
                f"无效的插件名称格式: {name}。"
                f"支持的格式：Java包名格式（如com.example.plugin）或简单名称（如my-plugin）"
                f"只允许字母、数字、点号、连字符和下划线，且必须以字母开头"
            )
        elif re.search(invalid_endings, name):
            raise PluginValidationError(
                f"插件名称不能以点号、连字符或下划线结尾: {name}"
            )
        elif re.search(invalid_sequences, name):
            raise PluginValidationError(
                f"插件名称不能包含连续的点号、连字符或下划线: {name}"
            )

    def get_plugin_file_list(self, file_path: str) -> List[Dict]:
        """获取ZIP文件中的文件列表（用于预览）"""
        try:
            with zipfile.ZipFile(file_path, 'r') as zip_ref:
                file_list = []
                for file_info in zip_ref.filelist:
                    file_list.append({
                        'name': file_info.filename,
                        'size': file_info.file_size,
                        'compressed_size': file_info.compress_size,
                        'is_dir': file_info.is_dir(),
                        'modify_time': f"{file_info.date_time[0]}-{file_info.date_time[1]:02d}-{file_info.date_time[2]:02d}"
                    })
                return file_list
        except Exception as e:
            raise PluginParseError(f"获取文件列表失败: {str(e)}")

# 便捷函数
def parse_plugin_from_zip(file_path: str) -> Dict:
    """便捷函数：解析ZIP文件中的插件信息"""
    with PluginParser() as parser:
        return parser.extract_plugin_metadata(file_path)

def validate_plugin_zip(file_path: str) -> Tuple[bool, str]:
    """便捷函数：验证ZIP插件文件"""
    try:
        with PluginParser() as parser:
            parser.validate_zip_file(file_path)
            return True, "验证通过"
    except PluginParseError as e:
        return False, str(e)