"""
插件数据模型
"""
from sqlalchemy import Column, String, Boolean, DateTime
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func
from app.database import Base


class Plugin(Base):
    """插件模型"""
    
    __tablename__ = "plugins"
    
    # 主键：插件名称（唯一标识符，不可变）
    name = Column(String, primary_key=True, comment="插件唯一标识符（反向域名格式）")
    
    # 基本信息
    display_name = Column(String, nullable=False, comment="显示名称")
    description = Column(String, default="", comment="描述")
    author = Column(String, default="", comment="作者")
    license = Column(String, default="", comment="许可证")
    homepage = Column(String, default="", comment="主页 URL")
    
    # DLL 信息
    main_dll = Column(String, nullable=False, comment="主 DLL 文件名")
    entry_class = Column(String, nullable=False, comment="入口类完全限定名")
    
    # 当前版本（版本号字符串）
    current_version = Column(String, nullable=True, comment="当前版本号")
    
    # 状态
    is_enabled = Column(Boolean, default=True, comment="是否启用")
    is_deprecated = Column(Boolean, default=False, comment="是否过时")
    
    # 上传密钥（首次上传时绑定，后续更新需验证）
    upload_key = Column(String(256), nullable=False, comment="插件上传密钥")
    
    # 时间戳
    created_at = Column(DateTime, server_default=func.now(), comment="创建时间")
    updated_at = Column(DateTime, server_default=func.now(), onupdate=func.now(), comment="更新时间")
    
    # 关系：一个插件有多个版本
    versions = relationship("PluginVersion", back_populates="plugin", cascade="all, delete-orphan")
    
    def __repr__(self):
        return f"<Plugin(name='{self.name}', version='{self.current_version}')>"
