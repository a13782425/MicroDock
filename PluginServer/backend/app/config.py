"""
配置管理模块

所有配置项都可以通过环境变量或 .env 文件覆盖。
环境变量名与配置项名称相同（大写）。

示例 .env 文件:
    HOST=0.0.0.0
    PORT=8000
    DEBUG=True
"""
from pydantic_settings import BaseSettings
from pydantic import Field
from pathlib import Path
from typing import List, Set


class Settings(BaseSettings):
    """
    应用配置类
    
    使用 pydantic-settings 自动从环境变量和 .env 文件加载配置。
    优先级: 环境变量 > .env 文件 > 默认值
    """
    
    # ==================== 应用信息 ====================
    # 应用名称，用于日志和文档显示
    APP_NAME: str = Field(
        default="MicroDock Plugin Server",
        description="应用名称"
    )
    
    # 应用版本号
    APP_VERSION: str = Field(
        default="2.0.0",
        description="应用版本"
    )
    
    # 调试模式，开启后会显示详细的SQL日志和错误信息
    DEBUG: bool = Field(
        default=True,
        description="调试模式开关，生产环境建议设为 False"
    )
    
    # ==================== 服务器配置 ====================
    # 服务器监听地址，0.0.0.0 表示监听所有网络接口
    HOST: str = Field(
        default="0.0.0.0",
        description="服务器监听地址"
    )
    
    # 服务器监听端口
    PORT: int = Field(
        default=8000,
        description="服务器监听端口"
    )
    
    # ==================== 数据库配置 ====================
    # SQLite 数据库连接 URL，使用 aiosqlite 异步驱动
    DATABASE_URL: str = Field(
        default="sqlite+aiosqlite:///./data/plugins.db",
        description="数据库连接 URL"
    )
    
    # ==================== 文件存储配置 ====================
    # 插件文件上传目录
    UPLOAD_DIR: Path = Field(
        default=Path("./data/uploads"),
        description="插件文件上传存储目录"
    )
    
    # 备份文件存储目录
    BACKUP_DIR: Path = Field(
        default=Path("./data/backups"),
        description="备份文件存储目录"
    )
    
    # 临时文件目录
    TEMP_DIR: Path = Field(
        default=Path("./data/temp"),
        description="临时文件目录"
    )
    
    # 单个文件最大上传大小（字节），默认 100MB
    MAX_UPLOAD_SIZE: int = Field(
        default=100 * 1024 * 1024,
        description="最大上传文件大小（字节），默认 100MB"
    )
    
    # 允许上传的文件扩展名
    ALLOWED_EXTENSIONS: Set[str] = Field(
        default={".zip"},
        description="允许上传的文件扩展名"
    )
    
    # ==================== CORS 跨域配置 ====================
    # 允许的跨域来源列表，支持前端开发服务器
    CORS_ORIGINS: List[str] = Field(
        default=[
            "http://localhost:3000",
            "http://localhost:3001",
            "http://127.0.0.1:3000",
        ],
        description="允许的跨域来源列表"
    )
    
    class Config:
        # 环境变量文件路径
        env_file = ".env"
        # 环境变量名大小写敏感
        case_sensitive = True
        # 允许额外字段
        extra = "allow"


# 全局配置实例
settings = Settings()

# 确保必要的目录存在
settings.UPLOAD_DIR.mkdir(parents=True, exist_ok=True)
settings.BACKUP_DIR.mkdir(parents=True, exist_ok=True)
settings.TEMP_DIR.mkdir(parents=True, exist_ok=True)
Path("./data").mkdir(parents=True, exist_ok=True)
