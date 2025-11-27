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
    async def get_versions_by_plugin_name(
        db: AsyncSession, 
        plugin_name: str
    ) -> List[PluginVersion]:
        """根据插件名获取所有版本"""
        result = await db.execute(
            select(PluginVersion)
            .where(PluginVersion.plugin_name == plugin_name)
            .order_by(PluginVersion.created_at.desc())
        )
        return list(result.scalars().all())
    
    @staticmethod
    async def get_version(
        db: AsyncSession, 
        plugin_name: str, 
        version: str
    ) -> Optional[PluginVersion]:
        """根据插件名和版本号获取版本（联合主键查询）"""
        result = await db.execute(
            select(PluginVersion)
            .where(
                PluginVersion.plugin_name == plugin_name,
                PluginVersion.version == version
            )
        )
        return result.scalar_one_or_none()
    
    @staticmethod
    async def check_version_exists(
        db: AsyncSession, 
        plugin_name: str, 
        version: str
    ) -> bool:
        """检查版本是否已存在"""
        result = await VersionService.get_version(db, plugin_name, version)
        return result is not None
    
    @staticmethod
    async def mark_version_deprecated(
        db: AsyncSession, 
        plugin_name: str, 
        version: str
    ) -> PluginVersion:
        """标记版本为过时"""
        ver = await VersionService.get_version(db, plugin_name, version)
        if not ver:
            raise HTTPException(
                status_code=404, 
                detail=f"插件 '{plugin_name}' 的版本 '{version}' 不存在"
            )
        
        ver.is_deprecated = True
        await db.commit()
        await db.refresh(ver)
        return ver
    
    @staticmethod
    async def increment_download_count(
        db: AsyncSession, 
        plugin_name: str, 
        version: str
    ) -> None:
        """增加下载次数"""
        ver = await VersionService.get_version(db, plugin_name, version)
        if ver:
            ver.download_count += 1
            await db.commit()
