"""
版本服务：处理插件版本相关的业务逻辑
"""
from typing import List, Optional
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select
from fastapi import HTTPException

from app.models.version import PluginVersion


class VersionService:
    """版本服务"""
    
    @staticmethod
    async def get_plugin_versions(db: AsyncSession, plugin_id: int) -> List[PluginVersion]:
        """获取插件的所有版本"""
        result = await db.execute(
            select(PluginVersion)
            .where(PluginVersion.plugin_id == plugin_id)
            .order_by(PluginVersion.created_at.desc())
        )
        return list(result.scalars().all())
    
    @staticmethod
    async def get_version_by_id(db: AsyncSession, version_id: int) -> Optional[PluginVersion]:
        """根据ID获取版本"""
        result = await db.execute(
            select(PluginVersion).where(PluginVersion.id == version_id)
        )
        return result.scalar_one_or_none()
    
    @staticmethod
    async def check_version_exists(db: AsyncSession, plugin_id: int, version: str) -> bool:
        """检查版本是否已存在"""
        result = await db.execute(
            select(PluginVersion)
            .where(
                PluginVersion.plugin_id == plugin_id,
                PluginVersion.version == version
            )
        )
        return result.scalar_one_or_none() is not None
    
    @staticmethod
    async def mark_version_deprecated(db: AsyncSession, version_id: int) -> PluginVersion:
        """标记版本为过时"""
        version = await VersionService.get_version_by_id(db, version_id)
        if not version:
            raise HTTPException(status_code=404, detail="版本不存在")
        
        version.is_deprecated = True
        await db.commit()
        await db.refresh(version)
        return version
    
    @staticmethod
    async def increment_download_count(db: AsyncSession, version_id: int) -> None:
        """增加下载次数"""
        version = await VersionService.get_version_by_id(db, version_id)
        if version:
            version.download_count += 1
            await db.commit()
