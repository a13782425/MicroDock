"""
通用响应 schemas
"""
from pydantic import BaseModel, Field
from typing import Any, Optional


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


class PluginNameRequest(BaseModel):
    """插件名查询请求"""
    name: str = Field(..., description="插件名称")


class PluginVersionRequest(BaseModel):
    """插件名 + 版本号查询请求"""
    name: str = Field(..., description="插件名称")
    version: str = Field(..., description="版本号")
