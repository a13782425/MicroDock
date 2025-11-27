"""
插件版本相关的 Pydantic schemas
"""
from pydantic import BaseModel, Field
from typing import Optional, Dict
from datetime import datetime


class VersionBase(BaseModel):
    """版本基础 schema"""
    version: str = Field(..., description="版本号")
    changelog: Optional[str] = Field(None, description="更新日志")


class VersionResponse(BaseModel):
    """版本响应 schema"""
    plugin_name: str  # 联合主键之一
    version: str      # 联合主键之一
    file_name: str
    file_size: int
    file_hash: str
    changelog: str
    is_deprecated: bool
    download_count: int
    created_at: datetime
    
    class Config:
        from_attributes = True


class VersionDetailResponse(VersionResponse):
    """版本详情响应 schema（包含依赖和引擎信息）"""
    dependencies: Dict[str, str] = {}
    engines: Dict[str, str] = {}
    
    class Config:
        from_attributes = True
