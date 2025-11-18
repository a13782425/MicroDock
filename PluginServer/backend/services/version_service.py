#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
版本管理服务
提供插件版本的增删改查和状态管理功能
"""

from datetime import datetime
from pathlib import Path
from typing import List, Optional, Dict, Any
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select, and_, or_, desc
from sqlalchemy.orm import selectinload

from ..models.plugin import Plugin, PluginVersion, PluginVersionCreate, PluginVersionUpdate
from ..utils.database import get_session
from ..services.security_service import SecurityService

class VersionService:
    """版本管理服务类"""

    def __init__(self):
        self.base_dir = Path(__file__).parent.parent.parent
        self.plugins_dir = self.base_dir / "data" / "plugins"

    async def get_versions(
        self,
        plugin_id: Optional[int] = None,
        skip: int = 0,
        limit: int = 100,
        is_outdated: Optional[bool] = None,
        include_plugin_info: bool = False
    ) -> List[PluginVersion]:
        """
        获取版本列表

        Args:
            plugin_id: 插件ID过滤
            skip: 跳过的记录数
            limit: 返回的记录数
            is_outdated: 是否过时过滤
            include_plugin_info: 是否包含插件信息

        Returns:
            版本列表
        """
        async with get_session() as session:
            query = select(PluginVersion)

            # 关联插件信息（如果需要）
            if include_plugin_info:
                query = query.options(selectinload(PluginVersion.plugin))

            # 应用过滤条件
            if plugin_id:
                query = query.where(PluginVersion.plugin_id == plugin_id)
            if is_outdated is not None:
                query = query.where(PluginVersion.is_outdated == is_outdated)

            # 应用分页和排序
            query = query.offset(skip).limit(limit).order_by(
                PluginVersion.created_at.desc()
            )

            result = await session.execute(query)
            return result.scalars().all()

    async def get_version_by_id(self, version_id: int) -> Optional[PluginVersion]:
        """根据ID获取版本"""
        async with get_session() as session:
            query = select(PluginVersion).where(PluginVersion.id == version_id)
            result = await session.execute(query)
            return result.scalar_one_or_none()

    async def get_plugin_versions(self, plugin_id: int) -> List[PluginVersion]:
        """获取指定插件的所有版本"""
        return await self.get_versions(plugin_id=plugin_id)

    async def get_latest_version(self, plugin_id: int) -> Optional[PluginVersion]:
        """获取插件的最新版本"""
        async with get_session() as session:
            query = select(PluginVersion).where(
                and_(
                    PluginVersion.plugin_id == plugin_id,
                    PluginVersion.is_outdated == False
                )
            ).order_by(PluginVersion.created_at.desc()).limit(1)

            result = await session.execute(query)
            return result.scalar_one_or_none()

    async def create_version(self, version_data: PluginVersionCreate) -> PluginVersion:
        """
        创建新版本

        Args:
            version_data: 版本创建数据

        Returns:
            创建的版本记录
        """
        async with get_session() as session:
            # 验证插件是否存在
            query = select(Plugin).where(Plugin.id == version_data.plugin_id)
            result = await session.execute(query)
            plugin = result.scalar_one_or_none()

            if not plugin:
                raise ValueError(f"插件 ID {version_data.plugin_id} 不存在")

            # 检查版本是否已存在
            existing_query = select(PluginVersion).where(
                and_(
                    PluginVersion.plugin_id == version_data.plugin_id,
                    PluginVersion.version == version_data.version
                )
            )
            existing_result = await session.execute(existing_query)
            existing = existing_result.scalar_one_or_none()

            if existing:
                raise ValueError(f"版本 {version_data.version} 已存在")

            # 计算文件哈希（如果提供了文件路径）
            file_hash = None
            if version_data.file_path:
                file_path = Path(version_data.file_path)
                if file_path.exists():
                    file_hash = SecurityService.calculate_file_sha256(file_path)

            # 创建版本记录
            db_version = PluginVersion(
                plugin_id=version_data.plugin_id,
                version=version_data.version,
                description=version_data.description,
                file_path=version_data.file_path,
                file_size=version_data.file_size or 0,
                file_hash=file_hash,
                changelog=version_data.changelog
            )

            session.add(db_version)
            await session.commit()
            await session.refresh(db_version)

            return db_version

    async def update_version(self, version_id: int, version_data: PluginVersionUpdate) -> Optional[PluginVersion]:
        """
        更新版本信息

        Args:
            version_id: 版本ID
            version_data: 更新数据

        Returns:
            更新后的版本记录
        """
        async with get_session() as session:
            query = select(PluginVersion).where(PluginVersion.id == version_id)
            result = await session.execute(query)
            version = result.scalar_one_or_none()

            if not version:
                return None

            # 应用更新
            update_data = version_data.dict(exclude_unset=True)
            for field, value in update_data.items():
                setattr(version, field, value)

            await session.commit()
            await session.refresh(version)

            return version

    async def mark_version_outdated(self, version_id: int, is_outdated: bool = True) -> bool:
        """
        标记版本为过时

        Args:
            version_id: 版本ID
            is_outdated: 是否过时

        Returns:
            是否成功标记
        """
        try:
            await self.update_version(
                version_id,
                PluginVersionUpdate(is_outdated=is_outdated)
            )
            return True
        except Exception:
            return False

    async def mark_plugin_outdated_versions(self, plugin_id: int, latest_version_id: int) -> int:
        """
        标记插件的所有其他版本为过时

        Args:
            plugin_id: 插件ID
            latest_version_id: 最新版本ID

        Returns:
            被标记为过时的版本数量
        """
        async with get_session() as session:
            query = select(PluginVersion).where(
                and_(
                    PluginVersion.plugin_id == plugin_id,
                    PluginVersion.id != latest_version_id
                )
            )

            result = await session.execute(query)
            versions = result.scalars().all()

            # 标记为过时
            for version in versions:
                version.is_outdated = True

            await session.commit()
            return len(versions)

    async def delete_version(self, version_id: int) -> bool:
        """
        删除版本

        Args:
            version_id: 版本ID

        Returns:
            是否成功删除
        """
        try:
            async with get_session() as session:
                query = select(PluginVersion).where(PluginVersion.id == version_id)
                result = await session.execute(query)
                version = result.scalar_one_or_none()

                if not version:
                    return False

                # 删除关联文件（如果存在）
                if version.file_path:
                    file_path = Path(version.file_path)
                    if file_path.exists():
                        file_path.unlink()

                # 删除数据库记录
                await session.delete(version)
                await session.commit()

                return True

        except Exception as e:
            print(f"删除版本失败: {e}")
            return False

    async def get_version_statistics(self) -> Dict[str, Any]:
        """
        获取版本统计信息

        Returns:
            版本统计信息
        """
        async with get_session() as session:
            # 总版本数
            total_query = select(PluginVersion)
            total_result = await session.execute(total_query)
            total_versions = len(total_result.scalars().all())

            # 过时版本数
            outdated_query = select(PluginVersion).where(PluginVersion.is_outdated == True)
            outdated_result = await session.execute(outdated_query)
            outdated_versions = len(outdated_result.scalars().all())

            # 最新版本数
            latest_query = select(PluginVersion).where(PluginVersion.is_outdated == False)
            latest_result = await session.execute(latest_query)
            latest_versions = len(latest_result.scalars().all())

            # 每个插件的版本数
            plugin_versions_query = select(
                Plugin.plugin_id,
                func.count(PluginVersion.id).label('version_count')
            ).outerjoin(PluginVersion).group_by(Plugin.plugin_id)

            # 这里需要导入func
            from sqlalchemy import func
            plugin_versions_result = await session.execute(plugin_versions_query)
            plugin_version_counts = plugin_versions_result.all()

            return {
                "total_versions": total_versions,
                "outdated_versions": outdated_versions,
                "latest_versions": latest_versions,
                "plugins_with_versions": len([v for v in plugin_version_counts if v.version_count > 0]),
                "average_versions_per_plugin": sum(v.version_count for v in plugin_version_counts) / max(len(plugin_version_counts), 1)
            }

    async def compare_versions(self, version_id1: int, version_id2: int) -> Dict[str, Any]:
        """
        比较两个版本的差异

        Args:
            version_id1: 版本1 ID
            version_id2: 版本2 ID

        Returns:
            版本比较结果
        """
        version1 = await self.get_version_by_id(version_id1)
        version2 = await self.get_version_by_id(version_id2)

        if not version1 or not version2:
            raise ValueError("版本不存在")

        if version1.plugin_id != version2.plugin_id:
            raise ValueError("版本属于不同插件，无法比较")

        comparison = {
            "plugin_id": version1.plugin_id,
            "version1": {
                "id": version1.id,
                "version": version1.version,
                "created_at": version1.created_at,
                "is_outdated": version1.is_outdated,
                "description": version1.description
            },
            "version2": {
                "id": version2.id,
                "version": version2.version,
                "created_at": version2.created_at,
                "is_outdated": version2.is_outdated,
                "description": version2.description
            }
        }

        # 比较时间
        if version1.created_at > version2.created_at:
            comparison["newer"] = "version1"
        elif version2.created_at > version1.created_at:
            comparison["newer"] = "version2"
        else:
            comparison["newer"] = "same"

        return comparison