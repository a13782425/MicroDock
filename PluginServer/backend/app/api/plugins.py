"""
插件管理 API 路由
"""
from typing import List
from fastapi import APIRouter, Depends, HTTPException, UploadFile, File
from fastapi.responses import FileResponse
from sqlalchemy.ext.asyncio import AsyncSession
from pathlib import Path

from app.database import get_db
from app.schemas.plugin import PluginResponse, PluginDetailResponse
from app.schemas.version import VersionResponse
from app.schemas.common import SuccessResponse
from app.services.plugin_service import PluginService
from app.services.version_service import VersionService

router = APIRouter(prefix="/api/plugins", tags=["plugins"])


@router.get("", response_model=List[PluginResponse])
async def get_plugins(db: AsyncSession = Depends(get_db)):
    """获取所有插件列表"""
    plugins = await PluginService.get_all_plugins(db)
    return plugins


@router.get("/{plugin_id}", response_model=PluginDetailResponse)
async def get_plugin(plugin_id: int, db: AsyncSession = Depends(get_db)):
    """获取插件详情（包含版本列表）"""
    plugin = await PluginService.get_plugin_by_id(db, plugin_id)
    if not plugin:
        raise HTTPException(status_code=404, detail="插件不存在")
    
    # 加载版本列表
    versions = await VersionService.get_plugin_versions(db, plugin_id)
    plugin.versions = versions
    
    return plugin


@router.post("", response_model=PluginResponse, status_code=201)
async def upload_plugin(
    file: UploadFile = File(..., description="插件ZIP文件"),
    db: AsyncSession = Depends(get_db)
):
    """上传新插件（ZIP格式，包含plugin.json）"""
    plugin = await PluginService.create_plugin_from_upload(db, file)
    return plugin


@router.patch("/{plugin_id}/enable", response_model=PluginResponse)
async def enable_plugin(plugin_id: int, db: AsyncSession = Depends(get_db)):
    """启用插件"""
    plugin = await PluginService.update_plugin(db, plugin_id, is_enabled=True)
    return plugin


@router.patch("/{plugin_id}/disable", response_model=PluginResponse)
async def disable_plugin(plugin_id: int, db: AsyncSession = Depends(get_db)):
    """禁用插件"""
    plugin = await PluginService.update_plugin(db, plugin_id, is_enabled=False)
    return plugin


@router.patch("/{plugin_id}/deprecate", response_model=PluginResponse)
async def deprecate_plugin(plugin_id: int, db: AsyncSession = Depends(get_db)):
    """标记插件为过时"""
    plugin = await PluginService.update_plugin(db, plugin_id, is_deprecated=True)
    return plugin


@router.delete("/{plugin_id}", response_model=SuccessResponse)
async def delete_plugin(plugin_id: int, db: AsyncSession = Depends(get_db)):
    """删除插件"""
    await PluginService.delete_plugin(db, plugin_id)
    return SuccessResponse(message="插件已删除")


@router.get("/{plugin_id}/download")
async def download_plugin(plugin_id: int, db: AsyncSession = Depends(get_db)):
    """下载插件当前版本"""
    plugin = await PluginService.get_plugin_by_id(db, plugin_id)
    if not plugin or not plugin.current_version_id:
        raise HTTPException(status_code=404, detail="插件或版本不存在")
    
    version = await VersionService.get_version_by_id(db, plugin.current_version_id)
    if not version:
        raise HTTPException(status_code=404, detail="版本不存在")
    
    file_path = Path(version.file_path)
    if not file_path.exists():
        raise HTTPException(status_code=404, detail="文件不存在")
    
    # 增加下载次数
    await VersionService.increment_download_count(db, version.id)
    
    return FileResponse(
        path=file_path,
        filename=version.file_name,
        media_type='application/zip'
    )


@router.get("/{plugin_id}/versions", response_model=List[VersionResponse])
async def get_plugin_versions(plugin_id: int, db: AsyncSession = Depends(get_db)):
    """获取插件的所有版本列表"""
    versions = await VersionService.get_plugin_versions(db, plugin_id)
    return versions
