"""
备份管理 API 路由
"""
from typing import List
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
from app.schemas.common import SuccessResponse
from app.services.backup_service import BackupService

router = APIRouter(prefix="/api/backups", tags=["backups"])


@router.post("/upload", response_model=BackupResponse, status_code=201)
async def upload_backup(
    file: UploadFile = File(..., description="备份文件"),
    user_key: str = Form(..., description="用户密钥"),
    backup_type: str = Form(..., description="备份类型: program | plugin"),
    description: str = Form("", description="备份描述（可选）"),
    db: AsyncSession = Depends(get_db)
):
    """上传备份文件"""
    backup = await BackupService.create_backup(
        db, user_key, backup_type, file, description
    )
    return backup


@router.post("/list", response_model=BackupListResponse)
async def list_backups(
    request: BackupListRequest,
    db: AsyncSession = Depends(get_db)
):
    """获取用户的备份列表"""
    backups = await BackupService.get_user_backups(db, request.user_key)
    return BackupListResponse(
        total=len(backups),
        backups=backups
    )


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


@router.post("/delete", response_model=SuccessResponse)
async def delete_backup(
    request: BackupDownloadRequest,
    db: AsyncSession = Depends(get_db)
):
    """删除备份"""
    await BackupService.delete_backup(db, request.user_key, request.id)
    return SuccessResponse(message="备份已删除")

