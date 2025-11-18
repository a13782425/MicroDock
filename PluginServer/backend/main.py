#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
MicroDock 插件管理服务器 - FastAPI版本
提供现代化的插件管理、版本控制和备份系统
"""

from fastapi import FastAPI, HTTPException, Depends
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from fastapi.responses import FileResponse
import uvicorn
import os
from pathlib import Path

# 导入API路由
from api import plugins, versions, backups

# 导入工具函数
from utils.database import init_database

# 添加当前目录到Python路径
import sys
from pathlib import Path
sys.path.append(str(Path(__file__).parent))

# 配置
BASE_DIR = Path(__file__).parent.parent
DATA_DIR = BASE_DIR / "data"
PLUGINS_DIR = DATA_DIR / "plugins"
BACKUPS_DIR = DATA_DIR / "backups"
UPLOADS_DIR = DATA_DIR / "uploads"

# 确保目录存在
for directory in [DATA_DIR, PLUGINS_DIR, BACKUPS_DIR, UPLOADS_DIR]:
    directory.mkdir(exist_ok=True)

# 创建FastAPI应用
app = FastAPI(
    title="MicroDock Plugin Server",
    description="现代化的插件管理和备份系统",
    version="2.0.0",
    docs_url="/api/docs",
    redoc_url="/api/redoc"
)

# 配置CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:3000", "http://127.0.0.1:3000"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# 注册API路由
app.include_router(plugins.router, prefix="/api/plugins", tags=["plugins"])
app.include_router(versions.router, prefix="/api/versions", tags=["versions"])
app.include_router(backups.router, prefix="/api/backups", tags=["backups"])

# 静态文件服务
app.mount("/static", StaticFiles(directory=str(DATA_DIR)), name="static")

# 前端静态文件（生产环境）
frontend_dist = BASE_DIR / "frontend" / "dist"
if frontend_dist.exists():
    app.mount("/", StaticFiles(directory=str(frontend_dist), html=True), name="frontend")
else:
    @app.get("/")
    async def root():
        return {
            "message": "MicroDock Plugin Server API",
            "docs": "/api/docs",
            "redoc": "/api/redoc",
            "status": "development"
        }

@app.on_event("startup")
async def startup_event():
    """应用启动时初始化"""
    await init_database()
    print("🚀 MicroDock Plugin Server 启动成功!")
    print(f"📂 插件目录: {PLUGINS_DIR}")
    print(f"📁 备份目录: {BACKUPS_DIR}")
    print(f"📚 API文档: http://localhost:8000/api/docs")

@app.get("/health")
async def health_check():
    """健康检查"""
    return {
        "status": "healthy",
        "version": "2.0.0",
        "services": {
            "database": "connected",
            "file_system": "available"
        }
    }

if __name__ == "__main__":
    uvicorn.run(
        "main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
        access_log=True
    )