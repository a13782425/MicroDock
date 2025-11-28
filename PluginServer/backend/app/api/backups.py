"""
备份管理 API 路由
"""
from typing import List, Optional
from fastapi import APIRouter, Depends, UploadFile, File, Form
from fastapi.responses import FileResponse
from sqlalchemy.ext.asyncio import AsyncSession

from app.database import get_db
from app.schemas.backup import (
    BackupResponse,
    BackupListRequest,
    BackupDownloadRequest,
    BackupListResponse
)
from app.schemas.common import ApiResponse
from app.services.backup_service import BackupService
from app.utils.auth import require_admin, TokenData

router = APIRouter(prefix="/api/backups", tags=["backups"])


@router.post("/upload", response_model=ApiResponse[BackupResponse], status_code=201)
async def upload_backup(
    file: UploadFile = File(..., description="备份文件"),
    user_key: str = Form(..., description="用户密钥"),
    backup_type: str = Form(..., description="备份类型: program | plugin"),
    plugin_name: Optional[str] = Form(None, description="插件名称（仅 plugin 类型需要）"),
    description: str = Form("", description="备份描述（可选）"),
    db: AsyncSession = Depends(get_db)
):
    """上传备份文件"""
    backup = await BackupService.create_backup(
        db, user_key, backup_type, file, description, plugin_name
    )
    return ApiResponse.ok(data=backup, message="备份上传成功")


@router.post("/list", response_model=ApiResponse[BackupListResponse])
async def list_backups(
    request: BackupListRequest,
    db: AsyncSession = Depends(get_db)
):
    """获取用户的备份列表"""
    backups = await BackupService.get_user_backups(db, request.user_key)
    data = BackupListResponse(total=len(backups), backups=backups)
    return ApiResponse.ok(data=data, message="获取备份列表成功")


@router.get("/list-all", response_model=ApiResponse[BackupListResponse])
async def list_all_backups(
    db: AsyncSession = Depends(get_db),
    admin: TokenData = Depends(require_admin)
):
    """获取所有用户的备份列表（需要管理员权限）"""
    backups = await BackupService.get_all_backups(db)
    data = BackupListResponse(total=len(backups), backups=backups)
    return ApiResponse.ok(data=data, message="获取所有备份列表成功")


@router.post("/download")
async def download_backup(
    request: BackupDownloadRequest,
    db: AsyncSession = Depends(get_db)
):
    """下载备份文件"""
    file_path, file_name = await BackupService.download_backup(
        db, request.user_key, request.id
    )
    return FileResponse(
        path=file_path,
        filename=file_name,
        media_type='application/octet-stream'
    )


@router.post("/delete", response_model=ApiResponse[None])
async def delete_backup(
    request: BackupDownloadRequest,
    db: AsyncSession = Depends(get_db),
    admin: TokenData = Depends(require_admin)
):
    """删除备份（需要管理员权限）"""
    await BackupService.delete_backup(db, request.user_key, request.id)
    return ApiResponse.ok(message="备份已删除")
