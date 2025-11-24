"""
插件版本数据模型
"""
from sqlalchemy import Column, Integer, String, Boolean, DateTime, ForeignKey, UniqueConstraint, Text
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func
from app.database import Base


class PluginVersion(Base):
    """插件版本模型"""
    
    __tablename__ = "plugin_versions"
    
    # 主键
    id = Column(Integer, primary_key=True, index=True)
    
    # 插件关联
    plugin_id = Column(Integer, ForeignKey("plugins.id", ondelete="CASCADE"), nullable=False, index=True, comment="所属插件ID")
    
    # 版本信息
    version = Column(String, nullable=False, index=True, comment="版本号")
    
    # 文件信息
    file_name = Column(String, nullable=False, comment="文件名")
    file_path = Column(String, nullable=False, comment="文件存储路径")
    file_size = Column(Integer, nullable=False, comment="文件大小（字节）")
    file_hash = Column(String, nullable=False, comment="SHA256 哈希值")
    
    # 元数据
    changelog = Column(Text, default="", comment="更新日志")
    dependencies = Column(Text, default="{}", comment="依赖信息（JSON）")
    engines = Column(Text, default="{}", comment="引擎要求（JSON）")
    
    # 状态
    is_deprecated = Column(Boolean, default=False, comment="是否过时")
    download_count = Column(Integer, default=0, comment="下载次数")
    
    # 时间戳
    created_at = Column(DateTime, server_default=func.now(), comment="创建时间")
    
    # 关系
    plugin = relationship("Plugin", back_populates="versions", foreign_keys=[plugin_id])
    
    # 唯一性约束：同一插件的版本号不能重复
    __table_args__ = (
        UniqueConstraint('plugin_id', 'version', name='uq_plugin_version'),
    )
    
    def __repr__(self):
        return f"<PluginVersion(id={self.id}, plugin_id={self.plugin_id}, version='{self.version}')>"
