#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
版本管理API路由
提供插件版本的RESTful API接口
"""

from fastapi import APIRouter, Depends, HTTPException, Query
from typing import List, Optional

from ..models.plugin import (
    PluginVersion, PluginVersionCreate, PluginVersionUpdate,
    PluginVersionResponse
)
from ..services.version_service import VersionService
from ..services.plugin_service import PluginService

router = APIRouter()

# 创建服务实例
version_service = VersionService()
plugin_service = PluginService()

@router.get("/", response_model=List[PluginVersionResponse])
async def get_versions(
    plugin_id: Optional[int] = Query(None, ge=1),
    skip: int = Query(0, ge=0),
    limit: int = Query(100, ge=1, le=1000),
    is_outdated: Optional[bool] = Query(None),
    include_plugin_info: bool = Query(default=False)
):
    """
    获取版本列表

    Args:
        plugin_id: 插件ID过滤
        skip: 跳过的记录数
        limit: 返回的记录数（最大1000）
        is_outdated: 是否过时过滤
        include_plugin_info: 是否包含插件信息

    Returns:
        版本列表
    """
    try:
        versions = await version_service.get_versions(
            plugin_id=plugin_id,
            skip=skip,
            limit=limit,
            is_outdated=is_outdated,
            include_plugin_info=include_plugin_info
        )

        return [PluginVersionResponse.from_orm(version) for version in versions]

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"获取版本列表失败: {str(e)}")

@router.get("/{version_id}", response_model=PluginVersionResponse)
async def get_version(version_id: int):
    """
    获取版本详情

    Args:
        version_id: 版本ID

    Returns:
        版本详情
    """
    try:
        version = await version_service.get_version_by_id(version_id)
        if not version:
            raise HTTPException(status_code=404, detail="版本不存在")

        return PluginVersionResponse.from_orm(version)

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"获取版本详情失败: {str(e)}")

@router.get("/plugin/{plugin_id}", response_model=List[PluginVersionResponse])
async def get_plugin_versions(
    plugin_id: int,
    include_outdated: bool = Query(default=True),
    skip: int = Query(0, ge=0),
    limit: int = Query(100, ge=1, le=1000)
):
    """
    获取指定插件的所有版本

    Args:
        plugin_id: 插件ID
        include_outdated: 是否包含过时版本
        skip: 跳过的记录数
        limit: 返回的记录数

    Returns:
        插件版本列表
    """
    try:
        # 验证插件是否存在
        plugin = await plugin_service.get_plugin_by_id(plugin_id)
        if not plugin:
            raise HTTPException(status_code=404, detail="插件不存在")

        versions = await version_service.get_versions(
            plugin_id=plugin_id,
            skip=skip,
            limit=limit,
            is_outdated=None if include_outdated else False
        )

        return [PluginVersionResponse.from_orm(version) for version in versions]

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"获取插件版本失败: {str(e)}")

@router.get("/plugin/{plugin_id}/latest", response_model=PluginVersionResponse)
async def get_latest_version(plugin_id: int):
    """
    获取插件的最新版本

    Args:
        plugin_id: 插件ID

    Returns:
        最新版本信息
    """
    try:
        # 验证插件是否存在
        plugin = await plugin_service.get_plugin_by_id(plugin_id)
        if not plugin:
            raise HTTPException(status_code=404, detail="插件不存在")

        version = await version_service.get_latest_version(plugin_id)
        if not version:
            raise HTTPException(status_code=404, detail="该插件没有版本记录")

        return PluginVersionResponse.from_orm(version)

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"获取最新版本失败: {str(e)}")

@router.post("/", response_model=PluginVersionResponse)
async def create_version(version_data: PluginVersionCreate):
    """
    创建新版本

    Args:
        version_data: 版本创建数据

    Returns:
        创建的版本信息
    """
    try:
        version = await version_service.create_version(version_data)
        return PluginVersionResponse.from_orm(version)

    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"创建版本失败: {str(e)}")

@router.put("/{version_id}", response_model=PluginVersionResponse)
async def update_version(
    version_id: int,
    version_data: PluginVersionUpdate
):
    """
    更新版本信息

    Args:
        version_id: 版本ID
        version_data: 更新数据

    Returns:
        更新后的版本信息
    """
    try:
        version = await version_service.update_version(version_id, version_data)
        if not version:
            raise HTTPException(status_code=404, detail="版本不存在")

        return PluginVersionResponse.from_orm(version)

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"更新版本失败: {str(e)}")

@router.delete("/{version_id}")
async def delete_version(version_id: int):
    """
    删除版本

    Args:
        version_id: 版本ID

    Returns:
        删除结果
    """
    try:
        success = await version_service.delete_version(version_id)
        if not success:
            raise HTTPException(status_code=404, detail="版本不存在")

        return {"success": True, "message": "版本删除成功"}

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"删除版本失败: {str(e)}")

@router.post("/{version_id}/mark-outdated")
async def mark_version_outdated(
    version_id: int,
    is_outdated: bool = Query(..., description="是否标记为过时")
):
    """
    标记版本为过时

    Args:
        version_id: 版本ID
        is_outdated: 是否过时

    Returns:
        操作结果
    """
    try:
        success = await version_service.mark_version_outdated(version_id, is_outdated)
        if not success:
            raise HTTPException(status_code=404, detail="版本不存在")

        return {
            "success": True,
            "message": f"版本已{'标记为过时' if is_outdated else '取消过时标记'}"
        }

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"标记版本状态失败: {str(e)}")

@router.post("/plugin/{plugin_id}/mark-others-outdated")
async def mark_other_versions_outdated(
    plugin_id: int,
    latest_version_id: int = Query(..., ge=1, description="最新版本ID")
):
    """
    标记插件的所有其他版本为过时

    Args:
        plugin_id: 插件ID
        latest_version_id: 最新版本ID

    Returns:
        操作结果
    """
    try:
        # 验证插件是否存在
        plugin = await plugin_service.get_plugin_by_id(plugin_id)
        if not plugin:
            raise HTTPException(status_code=404, detail="插件不存在")

        # 验证版本是否存在
        version = await version_service.get_version_by_id(latest_version_id)
        if not version or version.plugin_id != plugin_id:
            raise HTTPException(status_code=400, detail="指定的版本不存在或不属于该插件")

        marked_count = await version_service.mark_plugin_outdated_versions(plugin_id, latest_version_id)

        return {
            "success": True,
            "message": f"已标记 {marked_count} 个版本为过时",
            "marked_count": marked_count
        }

    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"标记其他版本失败: {str(e)}")

@router.get("/statistics")
async def get_version_statistics():
    """
    获取版本统计信息

    Returns:
        版本统计信息
    """
    try:
        statistics = await version_service.get_version_statistics()
        return {
            "success": True,
            "data": statistics
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"获取版本统计失败: {str(e)}")

@router.get("/{version_id1}/compare/{version_id2}")
async def compare_versions(version_id1: int, version_id2: int):
    """
    比较两个版本的差异

    Args:
        version_id1: 版本1 ID
        version_id2: 版本2 ID

    Returns:
        版本比较结果
    """
    try:
        comparison = await version_service.compare_versions(version_id1, version_id2)
        return {
            "success": True,
            "data": comparison
        }

    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"比较版本失败: {str(e)}")