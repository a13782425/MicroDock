"""
通用响应 schemas
"""
from pydantic import BaseModel, Field
from typing import Any, Optional


class IdRequest(BaseModel):
    """通用 ID 请求"""
    id: int = Field(..., description="资源ID")


class SuccessResponse(BaseModel):
    """成功响应"""
    success: bool = True
    message: str
    data: Optional[Any] = None


class ErrorResponse(BaseModel):
    """错误响应"""
    success: bool = False
    message: str
    error: Optional[str] = None


class HealthResponse(BaseModel):
    """健康检查响应"""
    status: str
    version: str
    database: str
