#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
插件管理服务
提供插件的增删改查等业务逻辑
"""

import json
import shutil
import zipfile
from datetime import datetime
from pathlib import Path
from typing import List, Optional, Dict, Any
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select, delete, and_
from sqlalchemy.orm import selectinload
from sqlalchemy.exc import IntegrityError

from ..models.plugin import Plugin, PluginVersion, PluginCreate, PluginUpdate, PluginWithVersions
from ..utils.database import get_session
from ..services.security_service import SecurityService
from ..utils.helpers import get_file_size, format_file_size
from sqlalchemy import or_

class PluginService:
    """插件管理服务类"""

    def __init__(self):
        self.base_dir = Path(__file__).parent.parent.parent
        self.plugins_dir = self.base_dir / "data" / "plugins"
        self.temp_dir = self.base_dir / "data" / "uploads"

    async def get_plugins(
        self,
        skip: int = 0,
        limit: int = 100,
        plugin_type: Optional[str] = None,
        is_active: Optional[bool] = None,
        search: Optional[str] = None
    ) -> List[Plugin]:
        """
        获取插件列表

        Args:
            skip: 跳过的记录数
            limit: 返回的记录数
            plugin_type: 插件类型过滤
            is_active: 是否激活过滤
            search: 搜索关键词

        Returns:
            插件列表
        """
        async with get_session() as session:
            query = select(Plugin)

            # 应用过滤条件
            if plugin_type:
                query = query.where(Plugin.plugin_type == plugin_type)
            if is_active is not None:
                query = query.where(Plugin.is_active == is_active)
            if search:
                query = query.where(
                    or_(
                        Plugin.name.ilike(f"%{search}%"),
                        Plugin.display_name.ilike(f"%{search}%"),
                        Plugin.description.ilike(f"%{search}%")
                    )
                )

            # 应用分页和排序
            query = query.offset(skip).limit(limit).order_by(Plugin.updated_at.desc())

            result = await session.execute(query)
            return result.scalars().all()

    async def get_plugin_by_id(self, plugin_id: int) -> Optional[Plugin]:
        """根据ID获取插件"""
        async with get_session() as session:
            query = select(Plugin).where(Plugin.id == plugin_id)
            result = await session.execute(query)
            return result.scalar_one_or_none()

    async def get_plugin_by_name(self, plugin_name: str) -> Optional[Plugin]:
        """根据名称获取插件"""
        async with get_session() as session:
            query = select(Plugin).where(Plugin.name == plugin_name)
            result = await session.execute(query)
            return result.scalar_one_or_none()

    async def get_plugin_with_versions(self, plugin_id: int) -> Optional[PluginWithVersions]:
        """获取包含版本信息的插件"""
        async with get_session() as session:
            query = select(Plugin).options(
                selectinload(Plugin.versions)
            ).where(Plugin.id == plugin_id)

            result = await session.execute(query)
            plugin = result.scalar_one_or_none()

            if plugin:
                # 转换为响应模型
                plugin_dict = {
                    "id": plugin.id,
                    "name": plugin.name,
                    "display_name": plugin.display_name,
                    "description": plugin.description,
                    "author": plugin.author,
                    "version": plugin.version,
                    "plugin_type": plugin.plugin_type,
                    "dependencies": json.loads(plugin.dependencies) if plugin.dependencies else [],
                    "config": json.loads(plugin.config) if plugin.config else {},
                    "file_path": plugin.file_path,
                    "file_size": plugin.file_size,
                    "file_hash": plugin.file_hash,
                    "is_active": plugin.is_active,
                    "is_outdated": plugin.is_outdated,
                    "created_at": plugin.created_at,
                    "updated_at": plugin.updated_at,
                    "versions": [
                        {
                            "id": v.id,
                            "version": v.version,
                            "description": v.description,
                            "changelog": v.changelog,
                            "file_path": v.file_path,
                            "file_size": v.file_size,
                            "file_hash": v.file_hash,
                            "is_outdated": v.is_outdated,
                            "created_at": v.created_at
                        } for v in plugin.versions
                    ]
                }
                return PluginWithVersions(**plugin_dict)

            return None

    async def create_plugin(self, plugin_data: PluginCreate) -> Plugin:
        """创建新插件"""
        async with get_session() as session:
            # 检查插件是否已存在（包括名称和版本的组合检查）
            existing = await self.get_plugin_by_name(plugin_data.name)
            if existing:
                # 检查版本是否也相同
                if existing.version == plugin_data.version:
                    raise ValueError(f"插件 '{plugin_data.name}' 版本 '{plugin_data.version}' 已存在，请勿重复上传相同版本的插件")
                else:
                    raise ValueError(f"插件 '{plugin_data.name}' 已存在（当前版本：{existing.version}），如需上传新版本请先在插件管理中升级版本")

            # 验证文件存在
            plugin_file = Path(plugin_data.file_path)
            if not plugin_file.exists():
                raise ValueError(f"插件文件不存在: {plugin_file}")

            # 计算文件哈希
            if not plugin_data.file_hash:
                plugin_data.file_hash = SecurityService.calculate_file_sha256(plugin_file)

            # 创建插件记录
            db_plugin = Plugin(
                name=plugin_data.name,
                display_name=plugin_data.display_name,
                description=plugin_data.description,
                author=plugin_data.author,
                version=plugin_data.version,
                plugin_type=plugin_data.plugin_type,
                file_path=plugin_data.file_path,
                file_size=plugin_data.file_size,
                file_hash=plugin_data.file_hash,
                dependencies=json.dumps(plugin_data.dependencies) if plugin_data.dependencies else None,
                config=json.dumps(plugin_data.config) if plugin_data.config else None
            )

            try:
                session.add(db_plugin)
                await session.commit()
                await session.refresh(db_plugin)
                return db_plugin
            except IntegrityError as e:
                await session.rollback()
                # 数据库唯一性约束违反
                if "UNIQUE constraint failed" in str(e) and "name" in str(e):
                    raise ValueError(f"插件名称 '{plugin_data.name}' 已存在，无法创建重复的插件")
                else:
                    raise ValueError(f"数据库约束错误: {str(e)}")

    async def update_plugin(self, plugin_id: int, plugin_data: PluginUpdate) -> Optional[Plugin]:
        """更新插件"""
        async with get_session() as session:
            query = select(Plugin).where(Plugin.id == plugin_id)
            result = await session.execute(query)
            plugin = result.scalar_one_or_none()

            if not plugin:
                return None

            # 更新字段
            update_data = plugin_data.dict(exclude_unset=True)

            # 处理特殊字段
            if 'dependencies' in update_data:
                update_data['dependencies'] = json.dumps(update_data['dependencies'])
            if 'config' in update_data:
                update_data['config'] = json.dumps(update_data['config'])

            # 应用更新
            for field, value in update_data.items():
                setattr(plugin, field, value)

            plugin.updated_at = datetime.utcnow()

            await session.commit()
            await session.refresh(plugin)

            return plugin

    async def delete_plugin(self, plugin_id: int) -> bool:
        """删除插件"""
        async with get_session() as session:
            query = select(Plugin).where(Plugin.id == plugin_id)
            result = await session.execute(query)
            plugin = result.scalar_one_or_none()

            if not plugin:
                return False

            # 备份文件（如果需要）
            plugin_file = Path(plugin.file_path)
            if plugin_file.exists():
                backup_dir = self.base_dir / "data" / "backups"
                backup_name = f"{plugin.name}_{datetime.now().strftime('%Y%m%d_%H%M%S')}{plugin_file.suffix}"
                backup_path = backup_dir / backup_name

                try:
                    shutil.copy2(plugin_file, backup_path)
                except Exception as e:
                    print(f"备份插件文件失败: {e}")

            # 删除数据库记录
            await session.delete(plugin)
            await session.commit()

            # 删除文件
            try:
                if plugin_file.exists():
                    plugin_file.unlink()
            except Exception as e:
                print(f"删除插件文件失败: {e}")

            return True

    async def scan_plugins(self) -> Dict[str, Any]:
        """扫描插件目录，自动发现插件"""
        discovered_plugins = []
        errors = []

        if not self.plugins_dir.exists():
            self.plugins_dir.mkdir(parents=True, exist_ok=True)
            return {"discovered": 0, "errors": 0, "plugins": []}

        # 扫描ZIP和DLL文件
        plugin_files = list(self.plugins_dir.glob("*.zip")) + list(self.plugins_dir.glob("*.dll"))

        for plugin_file in plugin_files:
            try:
                plugin_info = await self._extract_plugin_info(plugin_file)

                # 检查是否已存在
                existing = await self.get_plugin_by_name(plugin_info['name'])
                if existing:
                    # 更新现有插件信息
                    update_data = PluginUpdate(
                        display_name=plugin_info.get('display_name'),
                        description=plugin_info.get('description'),
                        author=plugin_info.get('author'),
                        version=plugin_info.get('version'),
                        file_size=plugin_info.get('file_size', 0),
                        file_hash=plugin_info.get('file_hash')
                    )
                    await self.update_plugin(existing.id, update_data)
                else:
                    # 创建新插件
                    plugin_create = PluginCreate(
                        name=plugin_info['name'],
                        display_name=plugin_info.get('display_name'),
                        description=plugin_info.get('description'),
                        author=plugin_info.get('author'),
                        version=plugin_info.get('version', '1.0.0'),
                        plugin_type=plugin_info.get('plugin_type', 'storage'),
                        file_path=str(plugin_file),
                        file_size=plugin_info.get('file_size', 0),
                        file_hash=plugin_info.get('file_hash'),
                        dependencies=plugin_info.get('dependencies', []),
                        config=plugin_info.get('config', {})
                    )
                    await self.create_plugin(plugin_create)

                discovered_plugins.append(plugin_info['name'])

            except Exception as e:
                errors.append(f"处理插件文件 {plugin_file} 失败: {str(e)}")

        return {
            "discovered": len(discovered_plugins),
            "errors": len(errors),
            "plugins": discovered_plugins,
            "error_details": errors
        }

    async def _extract_plugin_info(self, plugin_file: Path) -> Dict[str, Any]:
        """从插件文件中提取信息"""
        file_stat = plugin_file.stat()
        file_size = file_stat.st_size
        file_hash = SecurityService.calculate_file_sha256(plugin_file)

        # 默认配置
        plugin_config = {
            'name': plugin_file.stem,
            'display_name': plugin_file.stem,
            'version': '1.0.0',
            'description': '插件',
            'author': '未知',
            'plugin_type': 'storage',
            'dependencies': [],
            'config': {}
        }

        # 如果是ZIP文件，尝试读取plugin.json
        if plugin_file.suffix.lower() == '.zip':
            try:
                with zipfile.ZipFile(plugin_file, 'r') as zip_file:
                    # 查找根目录的plugin.json
                    for file_info in zip_file.filelist:
                        if (file_info.filename.endswith('plugin.json') and
                            '/' not in file_info.filename.replace('\\', '/')):
                            with zip_file.open(file_info) as json_file:
                                zip_config = json.loads(json_file.read().decode('utf-8'))
                                plugin_config.update(zip_config)
                            break
            except Exception as e:
                print(f"读取ZIP插件配置失败: {e}")

        return {
            **plugin_config,
            'file_size': file_size,
            'file_hash': file_hash
        }

