"""
插件管理 API 路由
"""
from typing import List
from fastapi import APIRouter, Depends, HTTPException, UploadFile, File, Form
from fastapi.responses import FileResponse
from sqlalchemy.ext.asyncio import AsyncSession
from pathlib import Path

from app.database import get_db
from app.schemas.plugin import PluginResponse, PluginDetailResponse
from app.schemas.version import VersionResponse, VersionDetailResponse
from app.schemas.common import ApiResponse, PluginNameRequest, PluginVersionRequest
from app.services.plugin_service import PluginService
from app.services.version_service import VersionService
from app.utils.auth import require_admin, TokenData

router = APIRouter(prefix="/api/plugins", tags=["plugins"])


@router.get("/list", response_model=ApiResponse[List[PluginResponse]])
async def get_plugins(db: AsyncSession = Depends(get_db)):
    """获取所有插件列表"""
    plugins = await PluginService.get_all_plugins(db)
    return ApiResponse.ok(data=plugins, message="获取插件列表成功")


@router.post("/detail", response_model=ApiResponse[PluginDetailResponse])
async def get_plugin(request: PluginNameRequest, db: AsyncSession = Depends(get_db)):
    """获取插件详情（包含版本列表）"""
    plugin = await PluginService.get_plugin_by_name(db, request.name)
    if not plugin:
        raise HTTPException(status_code=404, detail="插件不存在")
    
    # 加载版本列表
    versions = await VersionService.get_versions_by_plugin_name(db, request.name)
    plugin.versions = versions
    
    return ApiResponse.ok(data=plugin, message="获取插件详情成功")


@router.post("/upload", response_model=ApiResponse[PluginResponse], status_code=201)
async def upload_plugin(
    file: UploadFile = File(..., description="插件ZIP文件"),
    plugin_key: str = Form(..., description="插件密钥（首次上传绑定，后续验证）"),
    db: AsyncSession = Depends(get_db)
):
    """上传新插件（ZIP格式，包含plugin.json）"""
    plugin = await PluginService.create_plugin_from_upload(db, file, plugin_key)
    return ApiResponse.ok(data=plugin, message="插件上传成功")


@router.post("/enable", response_model=ApiResponse[PluginResponse])
async def enable_plugin(
    request: PluginNameRequest, 
    db: AsyncSession = Depends(get_db),
    admin: TokenData = Depends(require_admin)
):
    """启用插件（需要管理员权限）"""
    plugin = await PluginService.update_plugin(db, request.name, is_enabled=True)
    return ApiResponse.ok(data=plugin, message="插件已启用")


@router.post("/disable", response_model=ApiResponse[PluginResponse])
async def disable_plugin(
    request: PluginNameRequest, 
    db: AsyncSession = Depends(get_db),
    admin: TokenData = Depends(require_admin)
):
    """禁用插件（需要管理员权限）"""
    plugin = await PluginService.update_plugin(db, request.name, is_enabled=False)
    return ApiResponse.ok(data=plugin, message="插件已禁用")


@router.post("/deprecate", response_model=ApiResponse[PluginResponse])
async def deprecate_plugin(
    request: PluginNameRequest, 
    db: AsyncSession = Depends(get_db),
    admin: TokenData = Depends(require_admin)
):
    """标记插件为过时（需要管理员权限）"""
    plugin = await PluginService.update_plugin(db, request.name, is_deprecated=True)
    return ApiResponse.ok(data=plugin, message="插件已标记为过时")


@router.post("/delete", response_model=ApiResponse[None])
async def delete_plugin(
    request: PluginNameRequest, 
    db: AsyncSession = Depends(get_db),
    admin: TokenData = Depends(require_admin)
):
    """删除插件（需要管理员权限）"""
    await PluginService.delete_plugin(db, request.name)
    return ApiResponse.ok(message="插件已删除")


@router.post("/download")
async def download_plugin(request: PluginNameRequest, db: AsyncSession = Depends(get_db)):
    """下载插件当前版本"""
    plugin = await PluginService.get_plugin_by_name(db, request.name)
    if not plugin or not plugin.current_version:
        raise HTTPException(status_code=404, detail="插件或版本不存在")
    
    version = await VersionService.get_version(db, request.name, plugin.current_version)
    if not version:
        raise HTTPException(status_code=404, detail="版本不存在")
    
    file_path = Path(version.file_path)
    if not file_path.exists():
        raise HTTPException(status_code=404, detail="文件不存在")
    
    # 增加下载次数
    await VersionService.increment_download_count(db, request.name, plugin.current_version)
    
    return FileResponse(
        path=file_path,
        filename=version.file_name,
        media_type='application/zip'
    )


@router.post("/versions", response_model=ApiResponse[List[VersionResponse]])
async def get_plugin_versions(request: PluginNameRequest, db: AsyncSession = Depends(get_db)):
    """获取插件的所有版本列表"""
    plugin = await PluginService.get_plugin_by_name(db, request.name)
    if not plugin:
        raise HTTPException(status_code=404, detail=f"插件 '{request.name}' 不存在")
    
    versions = await VersionService.get_versions_by_plugin_name(db, request.name)
    return ApiResponse.ok(data=versions, message="获取版本列表成功")


@router.post("/version/detail", response_model=ApiResponse[VersionDetailResponse])
async def get_version_detail(request: PluginVersionRequest, db: AsyncSession = Depends(get_db)):
    """获取指定版本详情"""
    version = await VersionService.get_version(db, request.name, request.version)
    if not version:
        raise HTTPException(
            status_code=404, 
            detail=f"插件 '{request.name}' 的版本 '{request.version}' 不存在"
        )
    return ApiResponse.ok(data=version, message="获取版本详情成功")


@router.post("/version/deprecate", response_model=ApiResponse[VersionResponse])
async def deprecate_version(
    request: PluginVersionRequest, 
    db: AsyncSession = Depends(get_db),
    admin: TokenData = Depends(require_admin)
):
    """标记版本为过时（需要管理员权限）"""
    version = await VersionService.mark_version_deprecated(db, request.name, request.version)
    return ApiResponse.ok(data=version, message="版本已标记为过时")


@router.post("/version/download")
async def download_version(request: PluginVersionRequest, db: AsyncSession = Depends(get_db)):
    """下载指定版本"""
    version = await VersionService.get_version(db, request.name, request.version)
    if not version:
        raise HTTPException(
            status_code=404, 
            detail=f"插件 '{request.name}' 的版本 '{request.version}' 不存在"
        )
    
    file_path = Path(version.file_path)
    if not file_path.exists():
        raise HTTPException(status_code=404, detail="文件不存在")
    
    # 增加下载次数
    await VersionService.increment_download_count(db, request.name, request.version)
    
    return FileResponse(
        path=file_path,
        filename=version.file_name,
        media_type='application/zip'
    )
