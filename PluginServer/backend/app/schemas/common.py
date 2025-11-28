"""
通用响应 schemas
"""
from pydantic import BaseModel, Field
from typing import Any, Optional, Generic, TypeVar, List

# 泛型类型变量
T = TypeVar('T')


class ApiResponse(BaseModel, Generic[T]):
    """
    统一 API 响应格式
    
    成功: { "success": true, "message": "操作成功", "data": {...} }
    失败: { "success": false, "message": "错误信息", "data": null }
    """
    success: bool = True
    message: str = ""
    data: Optional[T] = None
    
    @classmethod
    def ok(cls, data: T = None, message: str = "操作成功") -> "ApiResponse[T]":
        """创建成功响应"""
        return cls(success=True, message=message, data=data)
    
    @classmethod
    def error(cls, message: str = "操作失败") -> "ApiResponse[None]":
        """创建失败响应"""
        return cls(success=False, message=message, data=None)


# 保留旧类型以兼容
class SuccessResponse(BaseModel):
    """成功响应 (已弃用，请使用 ApiResponse)"""
    success: bool = True
    message: str
    data: Optional[Any] = None


class ErrorResponse(BaseModel):
    """错误响应 (已弃用，请使用 ApiResponse)"""
    success: bool = False
    message: str
    error: Optional[str] = None


class HealthResponse(BaseModel):
    """健康检查响应数据"""
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
