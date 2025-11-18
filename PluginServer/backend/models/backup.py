#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
备份数据模型
定义备份相关的数据库表结构和数据模型
"""

from sqlalchemy import Column, Integer, String, DateTime, Text, Boolean, Float
from sqlalchemy.sql import func
from datetime import datetime
from typing import Optional
from pydantic import BaseModel, Field

from ..utils.database import Base

# SQLAlchemy数据模型
class Backup(Base):
    """备份数据表"""
    __tablename__ = "backups"

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(255), nullable=False)
    description = Column(Text, nullable=True)
    backup_type = Column(String(50), nullable=False)  # main_program, plugin, system
    file_path = Column(String(500), nullable=False)
    file_size = Column(Integer, default=0)
    file_hash = Column(String(64), nullable=False)  # SHA256哈希
    access_key = Column(String(64), nullable=False, index=True)  # SHA256索引
    is_active = Column(Boolean, default=True)
    metadata = Column(Text, nullable=True)  # JSON格式存储额外信息
    created_at = Column(DateTime(timezone=True), server_default=func.now())

    def __repr__(self):
        return f"<Backup(id={self.id}, name='{self.name}', type='{self.backup_type}')>"

# Pydantic数据模型
class BackupBase(BaseModel):
    """备份基础数据模型"""
    name: str = Field(..., min_length=1, max_length=255)
    description: Optional[str] = None
    backup_type: str = Field(..., regex="^(main_program|plugin|system)$")
    access_key: str = Field(..., min_length=1, max_length=255)  # 用户自定义key
    metadata: Optional[dict] = {}

class BackupCreate(BackupBase):
    """创建备份的数据模型"""
    file_path: str = Field(..., min_length=1)
    file_size: int = Field(..., ge=0)

class BackupUpdate(BaseModel):
    """更新备份的数据模型"""
    name: Optional[str] = Field(None, min_length=1, max_length=255)
    description: Optional[str] = None
    is_active: Optional[bool] = None
    metadata: Optional[dict] = None

class BackupResponse(BackupBase):
    """备份响应数据模型"""
    id: int
    file_path: str
    file_size: int
    file_hash: str
    is_active: bool
    created_at: datetime

    class Config:
        from_attributes = True

class BackupAccessRequest(BaseModel):
    """备份访问请求模型"""
    access_key: str = Field(..., min_length=1, max_length=255)

class BackupUploadRequest(BaseModel):
    """备份上传请求模型"""
    name: str = Field(..., min_length=1, max_length=255)
    description: Optional[str] = None
    backup_type: str = Field(..., regex="^(main_program|plugin|system)$")
    access_key: str = Field(..., min_length=1, max_length=255)
    metadata: Optional[dict] = {}