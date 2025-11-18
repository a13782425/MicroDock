#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
数据库工具函数
负责SQLite数据库的初始化和连接管理
"""

import asyncio
from pathlib import Path
from sqlalchemy import create_engine, MetaData
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker
from sqlalchemy.ext.asyncio import create_async_engine, AsyncSession, async_sessionmaker

# 数据库配置
BASE_DIR = Path(__file__).parent.parent.parent
DATABASE_URL = f"sqlite+aiosqlite:///{BASE_DIR}/data/database.db"

# 创建异步数据库引擎
engine = create_async_engine(
    DATABASE_URL,
    echo=True,  # 开发环境显示SQL语句
    future=True
)

# 创建会话工厂
AsyncSessionLocal = async_sessionmaker(
    engine,
    class_=AsyncSession,
    expire_on_commit=False
)

# 创建基础模型类
Base = declarative_base()

async def init_database():
    """初始化数据库"""
    async with engine.begin() as conn:
        # 创建所有表
        await conn.run_sync(Base.metadata.create_all)
    print("✅ 数据库初始化完成")

async def get_database_session():
    """获取数据库会话"""
    async with AsyncSessionLocal() as session:
        try:
            yield session
        finally:
            await session.close()

async def get_session() -> AsyncSession:
    """获取异步数据库会话"""
    return AsyncSessionLocal()