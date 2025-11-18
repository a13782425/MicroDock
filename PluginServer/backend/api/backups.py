#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
备份管理API路由
提供备份的RESTful API接口和SHA256索引功能
"""

from fastapi import APIRouter, Depends, HTTPException, Query, UploadFile, File, Form
from fastapi.responses import FileResponse
from typing import List, Optional, Dict, Any

from ..models.backup import (
    Backup, BackupCreate, BackupUpdate, BackupResponse,
    BackupUploadRequest, BackupAccessRequest
)
from ..services.backup_service import BackupService
from ..services.security_service import SecurityService

router = APIRouter()

# 创建备份服务实例
backup_service = BackupService()

@router.get("/", response_model=List[BackupResponse])
async def get_backups(
    skip: int = Query(0, ge=0),
    limit: int = Query(100, ge=1, le=1000),
    backup_type: Optional[str] = Query(None, regex="^(main_program|plugin|system|plugin_snapshot)$"),
    is_active: Optional[bool] = Query(None),
    search: Optional[str] = Query(None, min_length=1, max_length=100)
):
    """
    获取备份列表

    Args:
        skip: 跳过的记录数
        limit: 返回的记录数（最大1000）
        backup_type: 备份类型过滤
        is_active: 是否激活过滤
        search: 搜索关键词

    Returns:
        备份列表
    """
    try:
        backups = await backup_service.get_backups(
            skip=skip,
            limit=limit,
            backup_type=backup_type,
            is_active=is_active,
            search=search
        )

        return [BackupResponse.from_orm(backup) for backup in backups]

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"获取备份列表失败: {str(e)}")

@router.get("/{backup_id}", response_model=BackupResponse)
async def get_backup(backup_id: int):
    """
    获取备份详情

    Args:
        backup_id: 备份ID

    Returns:
        备份详情
    """
    try:
        backup = await backup_service.get_backup_by_id(backup_id)
        if not backup:
            raise HTTPException(status_code=404, detail="备份不存在")

        return BackupResponse.from_orm(backup)

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"获取备份详情失败: {str(e)}")

@router.post("/", response_model=BackupResponse)
async def create_backup(
    name: str = Form(..., min_length=1, max_length=255),
    backup_type: str = Form(..., regex="^(main_program|plugin|system|plugin_snapshot)$"),
    access_key: str = Form(..., min_length=8, max_length=255),
    description: Optional[str] = Form(None),
    metadata: Optional[str] = Form(None),  # JSON字符串
    file: UploadFile = File(...)
):
    """
    上传并创建备份

    Args:
        name: 备份名称
        backup_type: 备份类型
        access_key: 用户访问密钥
        description: 备份描述
        metadata: 备份元数据（JSON字符串）
        file: 备份文件

    Returns:
        创建的备份信息
    """
    try:
        # 解析元数据
        metadata_dict = {}
        if metadata:
            import json
            try:
                metadata_dict = json.loads(metadata)
            except json.JSONDecodeError:
                raise HTTPException(status_code=400, detail="元数据格式无效")

        # 读取文件内容
        file_content = await file.read()

        # 创建备份
        backup = await backup_service.upload_backup(
            name=name,
            backup_type=backup_type,
            user_key=access_key,
            file_content=file_content,
            filename=file.filename,
            description=description,
            metadata=metadata_dict
        )

        return BackupResponse.from_orm(backup)

    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"创建备份失败: {str(e)}")

@router.put("/{backup_id}", response_model=BackupResponse)
async def update_backup(
    backup_id: int,
    backup_data: BackupUpdate
):
    """
    更新备份信息

    Args:
        backup_id: 备份ID
        backup_data: 更新数据

    Returns:
        更新后的备份信息
    """
    try:
        backup = await backup_service.update_backup(backup_id, backup_data)
        if not backup:
            raise HTTPException(status_code=404, detail="备份不存在")

        return BackupResponse.from_orm(backup)

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"更新备份失败: {str(e)}")

@router.delete("/{backup_id}")
async def delete_backup(backup_id: int):
    """
    删除备份

    Args:
        backup_id: 备份ID

    Returns:
        删除结果
    """
    try:
        success = await backup_service.delete_backup(backup_id)
        if not success:
            raise HTTPException(status_code=404, detail="备份不存在")

        return {"success": True, "message": "备份删除成功"}

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"删除备份失败: {str(e)}")

@router.get("/download/{backup_id}")
async def download_backup(backup_id: int):
    """
    通过ID下载备份

    Args:
        backup_id: 备份ID

    Returns:
        备份文件下载
    """
    try:
        backup = await backup_service.get_backup_by_id(backup_id)
        if not backup:
            raise HTTPException(status_code=404, detail="备份不存在")

        # 验证文件完整性
        if not await backup_service.verify_backup_integrity(backup_id):
            raise HTTPException(status_code=500, detail="备份文件已损坏")

        file_path = backup.file_path
        from pathlib import Path
        file_path_obj = Path(file_path)

        if not file_path_obj.exists():
            raise HTTPException(status_code=404, detail="备份文件不存在")

        return FileResponse(
            path=str(file_path_obj),
            filename=f"{backup.name}_{backup.id}{file_path_obj.suffix}",
            media_type='application/octet-stream'
        )

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"下载备份失败: {str(e)}")

@router.post("/download")
async def download_backup_by_key(request: BackupAccessRequest):
    """
    通过访问密钥下载备份

    Args:
        request: 访问请求，包含访问密钥

    Returns:
        备份文件下载
    """
    try:
        file_path = await backup_service.download_backup_by_key(request.access_key)
        if not file_path:
            raise HTTPException(status_code=404, detail="备份不存在或访问密钥无效")

        return FileResponse(
            path=str(file_path),
            filename=file_path.name,
            media_type='application/octet-stream'
        )

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"下载备份失败: {str(e)}")

@router.post("/plugin-snapshot")
async def create_plugin_snapshot(
    plugin_ids: List[int] = Form(...),
    snapshot_name: str = Form(..., min_length=1, max_length=255),
    access_key: str = Form(..., min_length=8, max_length=255),
    description: Optional[str] = Form(None)
):
    """
    创建插件快照备份

    Args:
        plugin_ids: 插件ID列表（JSON字符串）
        snapshot_name: 快照名称
        access_key: 用户访问密钥
        description: 备份描述

    Returns:
        创建的备份信息
    """
    try:
        backup = await backup_service.create_plugin_snapshot(
            plugin_ids=plugin_ids,
            snapshot_name=snapshot_name,
            user_key=access_key,
            description=description
        )

        return BackupResponse.from_orm(backup)

    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"创建插件快照失败: {str(e)}")

@router.get("/{backup_id}/verify")
async def verify_backup_integrity(backup_id: int):
    """
    验证备份文件完整性

    Args:
        backup_id: 备份ID

    Returns:
        验证结果
    """
    try:
        backup = await backup_service.get_backup_by_id(backup_id)
        if not backup:
            raise HTTPException(status_code=404, detail="备份不存在")

        is_valid = await backup_service.verify_backup_integrity(backup_id)

        return {
            "success": True,
            "backup_id": backup_id,
            "backup_name": backup.name,
            "is_valid": is_valid,
            "message": "文件完整性验证通过" if is_valid else "文件已损坏"
        }

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"验证备份完整性失败: {str(e)}")

@router.get("/statistics")
async def get_backup_statistics():
    """
    获取备份统计信息

    Returns:
        备份统计信息
    """
    try:
        statistics = await backup_service.get_backup_statistics()
        return {
            "success": True,
            "data": statistics
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"获取备份统计失败: {str(e)}")

@router.post("/validate-key")
async def validate_access_key(access_key: str = Form(...)):
    """
    验证访问密钥格式

    Args:
        access_key: 访问密钥

    Returns:
        验证结果
    """
    try:
        is_valid = SecurityService.validate_access_key(access_key)
        backup_exists = False

        if is_valid:
            backup = await backup_service.get_backup_by_access_key(access_key)
            backup_exists = backup is not None

        return {
            "success": True,
            "is_valid_format": is_valid,
            "backup_exists": backup_exists,
            "message": "密钥格式有效" if is_valid else "密钥格式无效"
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"验证访问密钥失败: {str(e)}")

@router.post("/generate-key-hash")
async def generate_key_hash(access_key: str = Form(...)):
    """
    生成访问密钥的SHA256哈希

    Args:
        access_key: 访问密钥

    Returns:
        SHA256哈希值
    """
    try:
        key_hash = SecurityService.calculate_sha256(access_key)

        return {
            "success": True,
            "original_key": access_key,
            "sha256_hash": key_hash
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"生成密钥哈希失败: {str(e)}")