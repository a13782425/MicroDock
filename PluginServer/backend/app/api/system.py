"""
系统管理 API 路由
"""
from fastapi import APIRouter
from app.schemas.common import HealthResponse
from app.config import settings

router = APIRouter(prefix="/api", tags=["system"])


@router.get("/health", response_model=HealthResponse)
async def health_check():
    """健康检查"""
    return HealthResponse(
        status="healthy",
        version=settings.APP_VERSION,
        database="connected"
    )
