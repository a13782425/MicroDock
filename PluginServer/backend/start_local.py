#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
简化的本地启动脚本
"""

import os
import sys
import asyncio
import tempfile
from pathlib import Path
from fastapi import FastAPI, UploadFile, File
from fastapi.middleware.cors import CORSMiddleware
from dotenv import load_dotenv

# 加载环境变量
load_dotenv()

# 添加当前目录到Python路径
current_dir = Path(__file__).parent
sys.path.insert(0, str(current_dir))

# 导入插件解析工具
from utils.plugin_parser import parse_plugin_from_zip, validate_plugin_zip

# 简单的内存存储（仅用于演示）
plugin_storage = []

# 从环境变量读取配置
HOST = os.getenv("HOST", "0.0.0.0")
PORT = int(os.getenv("PORT", "8000"))
DEBUG = os.getenv("DEBUG", "false").lower() == "true"
RELOAD = os.getenv("RELOAD", "false").lower() == "true"
ALLOWED_ORIGINS = os.getenv("ALLOWED_ORIGINS", "http://localhost:3000,http://127.0.0.1:3000,http://localhost:3001,http://127.0.0.1:3001,http://localhost:3002,http://127.0.0.1:3002").split(",")
TEMP_DIR = os.getenv("TEMP_DIR", "./temp")

app = FastAPI(
    title="MicroDock Plugin Server (Local)",
    description="插件管理服务器 - 本地开发版本",
    version="2.0.0-local"
)

# 配置CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=ALLOWED_ORIGINS,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/")
async def root():
    return {"message": "MicroDock Plugin Server API", "version": "2.0.0-local"}

@app.get("/health")
async def health_check():
    return {"status": "healthy", "version": "2.0.0-local"}

@app.get("/api/plugins")
async def get_plugins():
    return {"success": True, "data": plugin_storage, "message": f"找到 {len(plugin_storage)} 个插件"}

@app.delete("/api/plugins/{plugin_id}")
async def delete_plugin(plugin_id: int):
    """删除插件"""
    global plugin_storage
    try:
        # 查找插件
        plugin_index = -1
        for i, plugin in enumerate(plugin_storage):
            if plugin.get("id") == plugin_id:
                plugin_index = i
                break

        if plugin_index == -1:
            return {"success": False, "error": f"插件 ID {plugin_id} 不存在"}, 404

        # 获取插件信息用于日志
        deleted_plugin = plugin_storage[plugin_index]

        # 从存储中删除
        plugin_storage.pop(plugin_index)

        return {
            "success": True,
            "message": f"插件 '{deleted_plugin.get('displayName', deleted_plugin.get('name'))}' 删除成功",
            "deleted_plugin": deleted_plugin
        }

    except Exception as e:
        return {"success": False, "error": f"删除插件失败: {str(e)}"}, 500

@app.post("/api/plugins")
async def upload_plugin(file: UploadFile = File(...)):
    """上传插件ZIP文件（简化版本）"""
    temp_file_path = None
    try:
        # 验证文件类型
        if not file.filename or not file.filename.lower().endswith('.zip'):
            return {"success": False, "error": "只支持 .zip 文件格式"}, 400

        # 读取文件内容到内存
        content = await file.read()

        # 使用系统临时目录，避免权限问题
        import tempfile
        import uuid

        temp_dir = Path(tempfile.gettempdir())
        temp_filename = f"plugin_upload_{uuid.uuid4().hex[:8]}.zip"
        temp_file_path = temp_dir / temp_filename

        # 写入临时文件
        with open(temp_file_path, 'wb') as temp_file:
            temp_file.write(content)

        try:
            # 验证ZIP文件
            is_valid, message = validate_plugin_zip(str(temp_file_path))
            if not is_valid:
                return {"success": False, "error": message}, 400

            # 解析插件信息
            metadata = parse_plugin_from_zip(str(temp_file_path))

            # 检查插件名称和版本是否已存在
            plugin_name = metadata.get("name")
            plugin_version = metadata.get("version")

            if plugin_name and plugin_version:
                for existing_plugin in plugin_storage:
                    if (existing_plugin.get("name") == plugin_name and
                        existing_plugin.get("version") == plugin_version):
                        return {
                            "success": False,
                            "error": f"插件 '{plugin_name}' 版本 '{plugin_version}' 已存在，不能重复上传相同名称和版本的插件"
                        }, 400

            # 创建插件记录
            import datetime
            plugin_record = {
                "id": len(plugin_storage) + 1,
                "name": metadata.get("name"),
                "displayName": metadata.get("displayName", metadata.get("name")),
                "version": metadata.get("version"),
                "description": metadata.get("description", ""),
                "author": metadata.get("author", ""),
                "filename": file.filename,
                "upload_time": datetime.datetime.now().isoformat(),
                "status": "active",
                "metadata": metadata
            }

            # 保存到内存存储
            plugin_storage.append(plugin_record)

            return {
                "success": True,
                "message": f"插件 '{plugin_record['displayName']}' 上传成功！",
                "data": plugin_record,
                "note": "插件已保存到本地演示存储"
            }

        except Exception as e:
            return {"success": False, "error": f"解析失败: {str(e)}"}, 400

    except Exception as e:
        return {"success": False, "error": f"文件处理失败: {str(e)}"}, 500
    finally:
        # 清理临时文件
        if temp_file_path and temp_file_path.exists():
            try:
                temp_file_path.unlink()
            except Exception as cleanup_error:
                print(f"清理临时文件失败: {cleanup_error}")

@app.post("/api/plugins/preview")
async def preview_plugin(file: UploadFile = File(...)):
    """预览插件ZIP文件内容（简化版本）"""
    temp_file_path = None
    try:
        # 验证文件类型
        if not file.filename or not file.filename.lower().endswith('.zip'):
            return {"success": False, "error": "只支持 .zip 文件格式"}, 400

        # 读取文件内容到内存
        content = await file.read()

        # 使用系统临时目录，避免权限问题
        import tempfile
        import uuid

        temp_dir = Path(tempfile.gettempdir())
        temp_filename = f"plugin_preview_{uuid.uuid4().hex[:8]}.zip"
        temp_file_path = temp_dir / temp_filename

        # 写入临时文件
        with open(temp_file_path, 'wb') as temp_file:
            temp_file.write(content)

        try:
            # 验证ZIP文件
            is_valid, message = validate_plugin_zip(str(temp_file_path))
            if not is_valid:
                return {"success": False, "error": message}, 400

            # 解析插件信息
            metadata = parse_plugin_from_zip(str(temp_file_path))

            return {
                "success": True,
                "filename": file.filename,
                "metadata": metadata,
                "message": "插件文件预览成功"
            }

        except Exception as e:
            return {"success": False, "error": f"解析失败: {str(e)}"}, 400

    except Exception as e:
        return {"success": False, "error": f"文件处理失败: {str(e)}"}, 500
    finally:
        # 清理临时文件
        if temp_file_path and temp_file_path.exists():
            try:
                temp_file_path.unlink()
            except Exception as cleanup_error:
                print(f"清理临时文件失败: {cleanup_error}")

if __name__ == "__main__":
    import uvicorn
    print("🚀 启动 MicroDock Plugin Server (本地版本)")
    print(f"📱 前端界面: http://localhost:3000")
    print(f"📚 API文档: http://localhost:{PORT}/docs")
    print(f"✅ 健康检查: http://localhost:{PORT}/health")
    print(f"🔧 后端端口: {PORT}")

    uvicorn.run(
        "start_local:app",
        host=HOST,
        port=PORT,
        reload=RELOAD
    )