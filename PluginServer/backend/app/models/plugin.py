"""
插件数据模型
"""
from sqlalchemy import Column, Integer, String, Boolean, DateTime, ForeignKey
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func
from app.database import Base


class Plugin(Base):
    """插件模型"""
    
    __tablename__ = "plugins"
    
    # 主键
    id = Column(Integer, primary_key=True, index=True)
    
    # 基本信息
    name = Column(String, unique=True, nullable=False, index=True, comment="插件唯一标识符（反向域名格式）")
    display_name = Column(String, nullable=False, comment="显示名称")
    version_number = Column(String, nullable=False, comment="当前版本号")
    description = Column(String, default="", comment="描述")
    author = Column(String, default="", comment="作者")
    license = Column(String, default="", comment="许可证")
    homepage = Column(String, default="", comment="主页 URL")
    
    # DLL 信息
    main_dll = Column(String, nullable=False, comment="主 DLL 文件名")
    entry_class = Column(String, nullable=False, comment="入口类完全限定名")
    
    # 状态
    is_enabled = Column(Boolean, default=True, comment="是否启用")
    is_deprecated = Column(Boolean, default=False, comment="是否过时")
    
    # 版本关联
    current_version_id = Column(Integer, ForeignKey("plugin_versions.id"), nullable=True, comment="当前版本ID")
    
    # 时间戳
    created_at = Column(DateTime, server_default=func.now(), comment="创建时间")
    updated_at = Column(DateTime, server_default=func.now(), onupdate=func.now(), comment="更新时间")
    
    # 关系
    versions = relationship("PluginVersion", back_populates="plugin", foreign_keys="PluginVersion.plugin_id")
    
    def __repr__(self):
        return f"<Plugin(id={self.id}, name='{self.name}', version='{self.version_number}')>"
