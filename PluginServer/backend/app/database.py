"""
数据库连接和会话管理
"""
from sqlalchemy.ext.asyncio import create_async_engine, AsyncSession, async_sessionmaker
from sqlalchemy.orm import declarative_base
from app.config import settings

# 验证 aiosqlite 是否正确安装
try:
    import aiosqlite
    print(f"[DEBUG] aiosqlite version: {aiosqlite.__version__}")
except ImportError as e:
    print(f"[ERROR] aiosqlite not found: {e}")
    raise

# 打印调试信息
print(f"[DEBUG] DATABASE_URL: {settings.DATABASE_URL}")

# 确保使用正确的异步驱动 URL
db_url = settings.DATABASE_URL
if db_url.startswith("sqlite:///") and not db_url.startswith("sqlite+aiosqlite:///"):
    db_url = db_url.replace("sqlite:///", "sqlite+aiosqlite:///")
    print(f"[DEBUG] Converted to async URL: {db_url}")

# 创建异步引擎
engine = create_async_engine(
    db_url,
    echo=settings.DEBUG,
    future=True
)

# 创建异步会话工厂
AsyncSessionLocal = async_sessionmaker(
    engine,
    class_=AsyncSession,
    expire_on_commit=False,
    autocommit=False,
    autoflush=False,
)

# 创建基础模型类
Base = declarative_base()


async def get_db() -> AsyncSession:
    """
    依赖注入函数：获取数据库会话
    """
    async with AsyncSessionLocal() as session:
        try:
            yield session
            await session.commit()
        except Exception:
            await session.rollback()
            raise
        finally:
            await session.close()


async def init_db():
    """
    初始化数据库（创建所有表）
    """
    # 导入所有模型以确保它们被注册到 Base.metadata
    from app.models.plugin import Plugin
    from app.models.version import PluginVersion
    from app.models.backup import Backup
    
    async with engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)
