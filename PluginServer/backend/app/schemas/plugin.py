"""
插件相关的 Pydantic schemas
"""
from pydantic import BaseModel, Field
from typing import Optional, List
from datetime import datetime


class PluginBase(BaseModel):
    """插件基础 schema"""
    name: str = Field(..., description="插件唯一标识符（反向域名格式）")
    display_name: str = Field(..., description="显示名称")
    description: Optional[str] = Field(None, description="描述")
    author: Optional[str] = Field(None, description="作者")
    license: Optional[str] = Field(None, description="许可证")
    homepage: Optional[str] = Field(None, description="主页 URL")


class PluginCreate(PluginBase):
    """创建插件时的 schema（用于手动创建，实际上传时从plugin.json解析）"""
    pass


class PluginUpdate(BaseModel):
    """更新插件时的 schema"""
    display_name: Optional[str] = None
    description: Optional[str] = None
    author: Optional[str] = None
    license: Optional[str] = None
    homepage: Optional[str] = None
    is_enabled: Optional[bool] = None
    is_deprecated: Optional[bool] = None


class PluginResponse(BaseModel):
    """插件响应 schema"""
    name: str  # 主键
    display_name: str
    current_version: Optional[str]  # 当前版本号
    description: str
    author: str
    license: str
    homepage: str
    main_dll: str
    entry_class: str
    is_enabled: bool
    is_deprecated: bool
    created_at: datetime
    updated_at: datetime
    
    class Config:
        from_attributes = True


class PluginDetailResponse(PluginResponse):
    """插件详情响应 schema（包含版本列表）"""
    versions: List["VersionResponse"] = []
    
    class Config:
        from_attributes = True


# 导入 VersionResponse 以避免循环导入
from app.schemas.version import VersionResponse
PluginDetailResponse.model_rebuild()
