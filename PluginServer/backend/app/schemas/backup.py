"""
备份相关的 Pydantic schemas
"""
from pydantic import BaseModel, Field
from typing import Optional, Literal
from datetime import datetime


class BackupListRequest(BaseModel):
    """获取备份列表请求"""
    user_key: str = Field(..., description="用户密钥")


class BackupDownloadRequest(BaseModel):
    """下载/删除备份请求"""
    user_key: str = Field(..., description="用户密钥")
    id: int = Field(..., description="备份ID")


class BackupResponse(BaseModel):
    """备份响应 schema"""
    id: int
    user_key: str
    backup_type: str
    plugin_name: Optional[str] = None
    file_name: str
    file_size: int
    file_hash: str
    description: str
    created_at: datetime
    
    class Config:
        from_attributes = True


class BackupListResponse(BaseModel):
    """备份列表响应"""
    total: int
    backups: list[BackupResponse]

