"""
版本管理 API 路由
"""
from fastapi import APIRouter, Depends, HTTPException
from fastapi.responses import FileResponse
from sqlalchemy.ext.asyncio import AsyncSession
from pathlib import Path

from app.database import get_db
from app.schemas.version import VersionResponse, VersionDetailResponse
from app.schemas.common import SuccessResponse, IdRequest
from app.services.version_service import VersionService

router = APIRouter(prefix="/api/versions", tags=["versions"])


@router.post("/detail", response_model=VersionDetailResponse)
async def get_version(request: IdRequest, db: AsyncSession = Depends(get_db)):
    """获取版本详情"""
    version = await VersionService.get_version_by_id(db, request.id)
    if not version:
        raise HTTPException(status_code=404, detail="版本不存在")
    return version


@router.post("/deprecate", response_model=VersionResponse)
async def deprecate_version(request: IdRequest, db: AsyncSession = Depends(get_db)):
    """标记版本为过时"""
    version = await VersionService.mark_version_deprecated(db, request.id)
    return version


@router.post("/download")
async def download_version(request: IdRequest, db: AsyncSession = Depends(get_db)):
    """下载指定版本"""
    version = await VersionService.get_version_by_id(db, request.id)
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
