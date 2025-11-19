#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
插件管理API路由
提供插件的RESTful API接口
"""

from fastapi import APIRouter, Depends, HTTPException, UploadFile, File, Form, Query
from fastapi.responses import FileResponse
from sqlalchemy.ext.asyncio import AsyncSession
from typing import List, Optional
import os
import tempfile
import shutil
from pathlib import Path

from models.plugin import (
    Plugin, PluginCreate, PluginUpdate, PluginResponse,
    PluginWithVersions
)
from services.plugin_service import PluginService
from services.security_service import SecurityService
from utils.database import get_database_session
from utils.helpers import get_safe_filename
from utils.plugin_parser import PluginParser, PluginParseError, PluginValidationError

router = APIRouter()

# 创建插件服务实例
plugin_service = PluginService()

@router.get("/", response_model=List[PluginResponse])
async def get_plugins(
    skip: int = Query(0, ge=0),
    limit: int = Query(100, ge=1, le=1000),
    plugin_type: Optional[str] = Query(None, regex="^(storage|service|tab)$"),
    is_active: Optional[bool] = Query(None),
    search: Optional[str] = Query(None, min_length=1, max_length=100)
):
    """
    获取插件列表

    Args:
        skip: 跳过的记录数
        limit: 返回的记录数（最大1000）
        plugin_type: 插件类型过滤 (storage, service, tab)
        is_active: 是否激活过滤
        search: 搜索关键词

    Returns:
        插件列表
    """
    try:
        plugins = await plugin_service.get_plugins(
            skip=skip,
            limit=limit,
            plugin_type=plugin_type,
            is_active=is_active,
            search=search
        )

        # 转换为响应模型
        return [PluginResponse.from_orm(plugin) for plugin in plugins]

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"获取插件列表失败: {str(e)}")

@router.get("/{plugin_id}", response_model=PluginWithVersions)
async def get_plugin(plugin_id: int):
    """
    获取插件详情（包含版本信息）

    Args:
        plugin_id: 插件ID

    Returns:
        插件详情
    """
    try:
        plugin = await plugin_service.get_plugin_with_versions(plugin_id)
        if not plugin:
            raise HTTPException(status_code=404, detail="插件不存在")

        return plugin

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"获取插件详情失败: {str(e)}")

@router.get("/name/{plugin_name}", response_model=PluginResponse)
async def get_plugin_by_name(plugin_name: str):
    """
    根据名称获取插件

    Args:
        plugin_name: 插件名称

    Returns:
        插件信息
    """
    try:
        plugin = await plugin_service.get_plugin_by_name(plugin_name)
        if not plugin:
            raise HTTPException(status_code=404, detail="插件不存在")

        return PluginResponse.from_orm(plugin)

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"获取插件失败: {str(e)}")

@router.post("/preview", response_model=dict)
async def preview_plugin(file: UploadFile = File(...)):
    """
    预览ZIP文件中的插件元数据，不创建插件记录

    Args:
        file: 插件ZIP文件

    Returns:
        ZIP文件中的插件元数据信息
    """
    temp_file_path = None
    try:
        # 验证文件类型 - 只支持ZIP文件
        if not file.filename or not file.filename.lower().endswith('.zip'):
            raise HTTPException(status_code=400, detail="只支持 .zip 文件格式")

        # 创建临时文件保存上传内容
        with tempfile.NamedTemporaryFile(delete=False, suffix='.zip') as temp_file:
            temp_file_path = temp_file.name
            shutil.copyfileobj(file.file, temp_file)

        # 使用插件解析器解析ZIP文件
        with PluginParser() as parser:
            try:
                # 验证ZIP文件并提取元数据
                plugin_metadata = parser.extract_plugin_metadata(temp_file_path)

                # 获取文件列表
                file_list = parser.get_plugin_file_list(temp_file_path)

                return {
                    "success": True,
                    "filename": file.filename,
                    "metadata": plugin_metadata,
                    "file_list": file_list,
                    "message": "插件文件预览成功"
                }

            except (PluginParseError, PluginValidationError) as e:
                return {
                    "success": False,
                    "filename": file.filename,
                    "error": str(e),
                    "message": "插件文件解析失败"
                }

    except HTTPException:
        raise
    except Exception as e:
        return {
            "success": False,
            "filename": file.filename,
            "error": str(e),
            "message": "文件预览失败"
        }
    finally:
        # 清理临时文件
        if temp_file_path and Path(temp_file_path).exists():
            Path(temp_file_path).unlink(missing_ok=True)

@router.post("/", response_model=PluginResponse)
async def create_plugin(
    file: UploadFile = File(...),
    name: Optional[str] = Form(None, min_length=1, max_length=255),
    display_name: Optional[str] = Form(None, max_length=255),
    description: Optional[str] = Form(None),
    author: Optional[str] = Form(None, max_length=255),
    version: Optional[str] = Form(None, min_length=1, max_length=50),
    plugin_type: str = Form(default="storage", regex="^(storage|service|tab)$")
):
    """
    上传并创建新插件
    支持ZIP文件自动解析plugin.json元数据

    Args:
        file: 插件文件（仅支持ZIP格式）
        name: 插件名称（可选，将从plugin.json自动提取）
        display_name: 显示名称（可选，将从plugin.json自动提取）
        description: 插件描述（可选，将从plugin.json自动提取）
        author: 作者（可选，将从plugin.json自动提取）
        version: 版本号（可选，将从plugin.json自动提取）
        plugin_type: 插件类型

    Returns:
        创建的插件信息
    """
    temp_file_path = None
    try:
        # 验证文件类型 - 只支持ZIP文件
        if not file.filename or not file.filename.lower().endswith('.zip'):
            raise HTTPException(status_code=400, detail="只支持 .zip 文件格式")

        # 创建临时文件保存上传内容
        with tempfile.NamedTemporaryFile(delete=False, suffix='.zip') as temp_file:
            temp_file_path = temp_file.name
            shutil.copyfileobj(file.file, temp_file)

        # 使用插件解析器解析ZIP文件
        with PluginParser() as parser:
            try:
                # 验证ZIP文件并提取元数据
                plugin_metadata = parser.extract_plugin_metadata(temp_file_path)

                # 从ZIP文件中提取的信息优先，但允许表单字段覆盖
                final_name = name if name else plugin_metadata.get('name')
                final_display_name = display_name if display_name else plugin_metadata.get('displayName')
                final_description = description if description else plugin_metadata.get('description')
                final_author = author if author else plugin_metadata.get('author')
                final_version = version if version else plugin_metadata.get('version')

                # 验证必需字段
                if not final_name:
                    raise HTTPException(status_code=400, detail="插件名称缺失，请在plugin.json中指定或通过表单提供")
                if not final_display_name:
                    raise HTTPException(status_code=400, detail="插件显示名称缺失，请在plugin.json中指定或通过表单提供")
                if not final_version:
                    raise HTTPException(status_code=400, detail="插件版本号缺失，请在plugin.json中指定或通过表单提供")

            except (PluginParseError, PluginValidationError) as e:
                raise HTTPException(status_code=400, detail=f"插件文件解析失败: {str(e)}")

        # 创建安全的文件名
        safe_filename = get_safe_filename(file.filename)
        plugin_dir = Path(__file__).parent.parent.parent / "data" / "plugins"
        plugin_dir.mkdir(parents=True, exist_ok=True)

        file_path = plugin_dir / safe_filename

        # 如果文件已存在，添加时间戳
        if file_path.exists():
            timestamp = int(datetime.now().timestamp())
            stem, ext = os.path.splitext(safe_filename)
            safe_filename = f"{stem}_{timestamp}{ext}"
            file_path = plugin_dir / safe_filename

        # 移动临时文件到最终位置
        shutil.move(temp_file_path, file_path)
        temp_file_path = None  # 防止在finally中删除

        # 创建插件记录
        file_size = file_path.stat().st_size
        file_hash = SecurityService.calculate_file_sha256(file_path)

        plugin_create = PluginCreate(
            name=final_name,
            display_name=final_display_name,
            description=final_description,
            author=final_author,
            version=final_version,
            plugin_type=plugin_type,
            file_path=str(file_path),
            file_size=file_size,
            file_hash=file_hash
        )

        plugin = await plugin_service.create_plugin(plugin_create)
        return PluginResponse.from_orm(plugin)

    except HTTPException:
        raise
    except ValueError as e:
        # 处理业务逻辑错误（如重复插件）
        if "已存在" in str(e):
            raise HTTPException(status_code=409, detail=str(e))
        else:
            raise HTTPException(status_code=400, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"创建插件失败: {str(e)}")
    finally:
        # 清理临时文件
        if temp_file_path and Path(temp_file_path).exists():
            Path(temp_file_path).unlink(missing_ok=True)

@router.put("/{plugin_id}", response_model=PluginResponse)
async def update_plugin(
    plugin_id: int,
    plugin_data: PluginUpdate
):
    """
    更新插件信息

    Args:
        plugin_id: 插件ID
        plugin_data: 更新数据

    Returns:
        更新后的插件信息
    """
    try:
        plugin = await plugin_service.update_plugin(plugin_id, plugin_data)
        if not plugin:
            raise HTTPException(status_code=404, detail="插件不存在")

        return PluginResponse.from_orm(plugin)

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"更新插件失败: {str(e)}")

@router.delete("/{plugin_id}")
async def delete_plugin(plugin_id: int):
    """
    删除插件

    Args:
        plugin_id: 插件ID

    Returns:
        删除结果
    """
    try:
        success = await plugin_service.delete_plugin(plugin_id)
        if not success:
            raise HTTPException(status_code=404, detail="插件不存在")

        return {"success": True, "message": "插件删除成功"}

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"删除插件失败: {str(e)}")

@router.get("/{plugin_id}/download")
async def download_plugin(plugin_id: int):
    """
    下载插件文件

    Args:
        plugin_id: 插件ID

    Returns:
        插件文件下载
    """
    try:
        plugin = await plugin_service.get_plugin_by_id(plugin_id)
        if not plugin:
            raise HTTPException(status_code=404, detail="插件不存在")

        file_path = Path(plugin.file_path)
        if not file_path.exists():
            raise HTTPException(status_code=404, detail="插件文件不存在")

        # 验证文件完整性
        if plugin.file_hash:
            if not SecurityService.verify_file_integrity(file_path, plugin.file_hash):
                raise HTTPException(status_code=500, detail="插件文件已损坏")

        return FileResponse(
            path=str(file_path),
            filename=f"{plugin.name}_{plugin.version}{file_path.suffix}",
            media_type='application/octet-stream'
        )

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"下载插件失败: {str(e)}")

@router.post("/scan")
async def scan_plugins():
    """
    扫描插件目录，自动发现插件

    Returns:
        扫描结果统计
    """
    try:
        result = await plugin_service.scan_plugins()
        return {
            "success": True,
            "message": "插件扫描完成",
            "data": result
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"扫描插件失败: {str(e)}")

@router.get("/{plugin_id}/toggle", response_model=PluginResponse)
async def toggle_plugin(plugin_id: int, enabled: bool = Query(...)):
    """
    切换插件启用/禁用状态

    Args:
        plugin_id: 插件ID
        enabled: 是否启用

    Returns:
        更新后的插件信息
    """
    try:
        plugin = await plugin_service.update_plugin(
            plugin_id,
            PluginUpdate(is_active=enabled)
        )
        if not plugin:
            raise HTTPException(status_code=404, detail="插件不存在")

        return PluginResponse.from_orm(plugin)

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"切换插件状态失败: {str(e)}")

# 需要导入datetime
from datetime import datetime