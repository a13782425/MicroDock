#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
插件数据模型
定义插件相关的数据库表结构和数据模型
"""

from sqlalchemy import Column, Integer, String, DateTime, Text, Boolean, ForeignKey, Float
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func
from datetime import datetime
from typing import Optional, List
from pydantic import BaseModel, Field

from ..utils.database import Base

# SQLAlchemy数据模型
class Plugin(Base):
    """插件数据表"""
    __tablename__ = "plugins"

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(255), unique=True, index=True, nullable=False)
    display_name = Column(String(255), nullable=True)
    description = Column(Text, nullable=True)
    author = Column(String(255), nullable=True)
    version = Column(String(50), nullable=False)
    plugin_type = Column(String(50), default="storage")  # storage, service, tab
    file_path = Column(String(500), nullable=False)
    file_size = Column(Integer, default=0)
    file_hash = Column(String(64), nullable=True)  # SHA256哈希
    is_active = Column(Boolean, default=True)
    is_outdated = Column(Boolean, default=False)
    dependencies = Column(Text, nullable=True)  # JSON格式存储依赖
    config = Column(Text, nullable=True)  # JSON格式存储配置
    created_at = Column(DateTime(timezone=True), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), server_default=func.now(), onupdate=func.now())

    # 关联关系
    versions = relationship("PluginVersion", back_populates="plugin", cascade="all, delete-orphan")

    def __repr__(self):
        return f"<Plugin(id={self.id}, name='{self.name}', version='{self.version}')>"

class PluginVersion(Base):
    """插件版本数据表"""
    __tablename__ = "plugin_versions"

    id = Column(Integer, primary_key=True, index=True)
    plugin_id = Column(Integer, ForeignKey("plugins.id"), nullable=False)
    version = Column(String(50), nullable=False)
    description = Column(Text, nullable=True)
    file_path = Column(String(500), nullable=True)
    file_size = Column(Integer, default=0)
    file_hash = Column(String(64), nullable=True)
    is_outdated = Column(Boolean, default=False)
    changelog = Column(Text, nullable=True)
    created_at = Column(DateTime(timezone=True), server_default=func.now())

    # 关联关系
    plugin = relationship("Plugin", back_populates="versions")

    def __repr__(self):
        return f"<PluginVersion(id={self.id}, version='{self.version}', plugin_id={self.plugin_id})>"

# Pydantic数据模型
class PluginBase(BaseModel):
    """插件基础数据模型"""
    name: str = Field(..., min_length=1, max_length=255)
    display_name: Optional[str] = Field(None, max_length=255)
    description: Optional[str] = None
    author: Optional[str] = Field(None, max_length=255)
    version: str = Field(..., min_length=1, max_length=50)
    plugin_type: str = Field(default="storage", regex="^(storage|service|tab)$")
    dependencies: Optional[List[str]] = []
    config: Optional[dict] = {}

class PluginCreate(PluginBase):
    """创建插件的数据模型"""
    file_path: str = Field(..., min_length=1)
    file_size: int = Field(..., ge=0)
    file_hash: Optional[str] = None

class PluginUpdate(BaseModel):
    """更新插件的数据模型"""
    display_name: Optional[str] = Field(None, max_length=255)
    description: Optional[str] = None
    author: Optional[str] = Field(None, max_length=255)
    is_active: Optional[bool] = None
    is_outdated: Optional[bool] = None
    dependencies: Optional[List[str]] = None
    config: Optional[dict] = None

class PluginResponse(PluginBase):
    """插件响应数据模型"""
    id: int
    file_path: str
    file_size: int
    file_hash: Optional[str]
    is_active: bool
    is_outdated: bool
    created_at: datetime
    updated_at: datetime

    class Config:
        from_attributes = True

class PluginVersionBase(BaseModel):
    """插件版本基础数据模型"""
    version: str = Field(..., min_length=1, max_length=50)
    description: Optional[str] = None
    changelog: Optional[str] = None

class PluginVersionCreate(PluginVersionBase):
    """创建插件版本的数据模型"""
    plugin_id: int
    file_path: Optional[str] = None
    file_size: Optional[int] = None
    file_hash: Optional[str] = None

class PluginVersionUpdate(BaseModel):
    """更新插件版本的数据模型"""
    is_outdated: Optional[bool] = None
    description: Optional[str] = None
    changelog: Optional[str] = None

class PluginVersionResponse(PluginVersionBase):
    """插件版本响应数据模型"""
    id: int
    plugin_id: int
    file_path: Optional[str]
    file_size: Optional[int]
    file_hash: Optional[str]
    is_outdated: bool
    created_at: datetime

    class Config:
        from_attributes = True

class PluginWithVersions(PluginResponse):
    """包含版本信息的插件响应模型"""
    versions: List[PluginVersionResponse] = []

    class Config:
        from_attributes = True