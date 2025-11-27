"""
备份数据模型
"""
from sqlalchemy import Column, Integer, String, DateTime, Text
from sqlalchemy.sql import func
from app.database import Base


class Backup(Base):
    """用户备份模型"""
    
    __tablename__ = "backups"
    
    # 主键
    id = Column(Integer, primary_key=True, index=True)
    
    # 用户标识
    user_key = Column(String(256), nullable=False, index=True, comment="用户密钥")
    
    # 备份类型
    backup_type = Column(String(20), nullable=False, comment="备份类型: program | plugin")
    
    # 插件名称（仅当 backup_type = 'plugin' 时使用）
    plugin_name = Column(String, nullable=True, index=True, comment="插件名称（仅 plugin 类型）")
    
    # 文件信息
    file_name = Column(String, nullable=False, comment="原文件名")
    file_path = Column(String, nullable=False, comment="存储路径")
    file_size = Column(Integer, nullable=False, comment="文件大小（字节）")
    file_hash = Column(String, nullable=False, comment="SHA256 哈希值")
    
    # 可选描述
    description = Column(Text, default="", comment="备份描述")
    
    # 时间戳
    created_at = Column(DateTime, server_default=func.now(), comment="创建时间")
    
    def __repr__(self):
        return f"<Backup(id={self.id}, user_key='{self.user_key}', type='{self.backup_type}', plugin='{self.plugin_name}')>"

