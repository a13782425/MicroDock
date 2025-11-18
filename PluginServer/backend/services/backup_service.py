#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
备份管理服务
提供主程序备份、插件备份和SHA256索引功能
"""

import json
import shutil
import tempfile
from datetime import datetime
from pathlib import Path
from typing import List, Optional, Dict, Any, Union
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select, and_, or_, desc
from sqlalchemy.orm import selectinload

from ..models.backup import Backup, BackupCreate, BackupUpdate
from ..utils.database import get_session
from ..services.security_service import SecurityService
from ..utils.helpers import ensure_directory_exists, create_backup_filename

class BackupService:
    """备份管理服务类"""

    def __init__(self):
        self.base_dir = Path(__file__).parent.parent.parent
        self.backups_dir = self.base_dir / "data" / "backups"
        self.temp_dir = self.base_dir / "data" / "uploads"

        # 确保目录存在
        ensure_directory_exists(self.backups_dir)
        ensure_directory_exists(self.temp_dir)

    async def get_backups(
        self,
        skip: int = 0,
        limit: int = 100,
        backup_type: Optional[str] = None,
        is_active: Optional[bool] = None,
        search: Optional[str] = None
    ) -> List[Backup]:
        """
        获取备份列表

        Args:
            skip: 跳过的记录数
            limit: 返回的记录数
            backup_type: 备份类型过滤
            is_active: 是否激活过滤
            search: 搜索关键词

        Returns:
            备份列表
        """
        async with get_session() as session:
            query = select(Backup)

            # 应用过滤条件
            if backup_type:
                query = query.where(Backup.backup_type == backup_type)
            if is_active is not None:
                query = query.where(Backup.is_active == is_active)
            if search:
                query = query.where(
                    or_(
                        Backup.name.ilike(f"%{search}%"),
                        Backup.description.ilike(f"%{search}%")
                    )
                )

            # 应用分页和排序
            query = query.offset(skip).limit(limit).order_by(Backup.created_at.desc())

            result = await session.execute(query)
            return result.scalars().all()

    async def get_backup_by_id(self, backup_id: int) -> Optional[Backup]:
        """根据ID获取备份"""
        async with get_session() as session:
            query = select(Backup).where(Backup.id == backup_id)
            result = await session.execute(query)
            return result.scalar_one_or_none()

    async def get_backup_by_access_key(self, access_key: str) -> Optional[Backup]:
        """根据访问密钥获取备份"""
        # 计算SHA256哈希
        access_key_hash = SecurityService.calculate_sha256(access_key)

        async with get_session() as session:
            query = select(Backup).where(Backup.access_key == access_key_hash)
            result = await session.execute(query)
            return result.scalar_one_or_none()

    async def create_backup(
        self,
        name: str,
        backup_type: str,
        file_path: Union[str, Path],
        user_key: str,
        description: Optional[str] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> Backup:
        """
        创建备份

        Args:
            name: 备份名称
            backup_type: 备份类型
            file_path: 文件路径
            user_key: 用户自定义密钥
            description: 备份描述
            metadata: 备份元数据

        Returns:
            创建的备份记录
        """
        # 验证文件存在
        source_file = Path(file_path)
        if not source_file.exists():
            raise ValueError(f"源文件不存在: {source_file}")

        # 验证密钥格式
        if not SecurityService.validate_access_key(user_key):
            raise ValueError("访问密钥格式无效")

        # 计算文件哈希
        file_hash = SecurityService.calculate_file_sha256(source_file)
        file_size = source_file.stat().st_size

        # 生成访问密钥哈希
        timestamp = datetime.now().isoformat()
        access_key_hash = SecurityService.calculate_sha256(f"{user_key}:{timestamp}")

        # 创建备份文件名
        safe_name = SecurityService.sanitize_filename(name)
        backup_filename = create_backup_filename(safe_name)
        backup_path = self.backups_dir / backup_filename

        # 如果文件已存在，添加时间戳
        if backup_path.exists():
            backup_filename = create_backup_filename(safe_name, datetime.now().strftime('%Y%m%d_%H%M%S'))
            backup_path = self.backups_dir / backup_filename

        try:
            # 复制文件到备份目录
            shutil.copy2(source_file, backup_path)

            # 创建数据库记录
            db_backup = Backup(
                name=name,
                description=description,
                backup_type=backup_type,
                file_path=str(backup_path),
                file_size=file_size,
                file_hash=file_hash,
                access_key=access_key_hash,
                metadata=json.dumps(metadata) if metadata else None
            )

            async with get_session() as session:
                session.add(db_backup)
                await session.commit()
                await session.refresh(db_backup)

            return db_backup

        except Exception as e:
            # 清理已复制的文件
            if backup_path.exists():
                backup_path.unlink()
            raise e

    async def upload_backup(
        self,
        name: str,
        backup_type: str,
        user_key: str,
        file_content: bytes,
        filename: str,
        description: Optional[str] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> Backup:
        """
        上传备份文件

        Args:
            name: 备份名称
            backup_type: 备份类型
            user_key: 用户自定义密钥
            file_content: 文件内容
            filename: 原始文件名
            description: 备份描述
            metadata: 备份元数据

        Returns:
            创建的备份记录
        """
        # 验证密钥格式
        if not SecurityService.validate_access_key(user_key):
            raise ValueError("访问密钥格式无效")

        # 创建临时文件
        with tempfile.NamedTemporaryFile(delete=False, suffix=Path(filename).suffix) as temp_file:
            temp_file.write(file_content)
            temp_path = Path(temp_file.name)

        try:
            # 计算文件哈希和大小
            file_hash = SecurityService.calculate_file_sha256(temp_path)
            file_size = temp_path.stat().st_size

            # 生成访问密钥哈希
            timestamp = datetime.now().isoformat()
            access_key_hash = SecurityService.calculate_sha256(f"{user_key}:{timestamp}")

            # 创建备份文件名
            safe_name = SecurityService.sanitize_filename(name)
            backup_filename = create_backup_filename(safe_name)
            backup_path = self.backups_dir / backup_filename

            # 如果文件已存在，添加时间戳
            if backup_path.exists():
                backup_filename = create_backup_filename(safe_name, datetime.now().strftime('%Y%m%d_%H%M%S'))
                backup_path = self.backups_dir / backup_filename

            # 移动临时文件到备份目录
            shutil.move(str(temp_path), str(backup_path))

            # 创建数据库记录
            db_backup = Backup(
                name=name,
                description=description,
                backup_type=backup_type,
                file_path=str(backup_path),
                file_size=file_size,
                file_hash=file_hash,
                access_key=access_key_hash,
                metadata=json.dumps(metadata) if metadata else None
            )

            async with get_session() as session:
                session.add(db_backup)
                await session.commit()
                await session.refresh(db_backup)

            return db_backup

        except Exception as e:
            # 清理临时文件
            if temp_path.exists():
                temp_path.unlink()
            raise e

    async def update_backup(self, backup_id: int, backup_data: BackupUpdate) -> Optional[Backup]:
        """
        更新备份信息

        Args:
            backup_id: 备份ID
            backup_data: 更新数据

        Returns:
            更新后的备份记录
        """
        async with get_session() as session:
            query = select(Backup).where(Backup.id == backup_id)
            result = await session.execute(query)
            backup = result.scalar_one_or_none()

            if not backup:
                return None

            # 应用更新
            update_data = backup_data.dict(exclude_unset=True)

            # 处理特殊字段
            if 'metadata' in update_data:
                update_data['metadata'] = json.dumps(update_data['metadata'])

            for field, value in update_data.items():
                setattr(backup, field, value)

            await session.commit()
            await session.refresh(backup)

            return backup

    async def delete_backup(self, backup_id: int) -> bool:
        """
        删除备份

        Args:
            backup_id: 备份ID

        Returns:
            是否成功删除
        """
        try:
            async with get_session() as session:
                query = select(Backup).where(Backup.id == backup_id)
                result = await session.execute(query)
                backup = result.scalar_one_or_none()

                if not backup:
                    return False

                # 删除文件
                file_path = Path(backup.file_path)
                if file_path.exists():
                    file_path.unlink()

                # 删除数据库记录
                await session.delete(backup)
                await session.commit()

                return True

        except Exception as e:
            print(f"删除备份失败: {e}")
            return False

    async def download_backup_by_key(self, access_key: str) -> Optional[Path]:
        """
        通过访问密钥下载备份

        Args:
            access_key: 用户访问密钥

        Returns:
            备份文件路径
        """
        backup = await self.get_backup_by_access_key(access_key)
        if not backup:
            return None

        # 验证文件完整性
        file_path = Path(backup.file_path)
        if not file_path.exists():
            return None

        if not SecurityService.verify_file_integrity(file_path, backup.file_hash):
            return None

        return file_path

    async def verify_backup_integrity(self, backup_id: int) -> bool:
        """
        验证备份文件完整性

        Args:
            backup_id: 备份ID

        Returns:
            文件完整性是否有效
        """
        backup = await self.get_backup_by_id(backup_id)
        if not backup:
            return False

        file_path = Path(backup.file_path)
        if not file_path.exists():
            return False

        return SecurityService.verify_file_integrity(file_path, backup.file_hash)

    async def create_plugin_snapshot(
        self,
        plugin_ids: List[int],
        snapshot_name: str,
        user_key: str,
        description: Optional[str] = None
    ) -> Backup:
        """
        创建插件快照备份

        Args:
            plugin_ids: 插件ID列表
            snapshot_name: 快照名称
            user_key: 用户访问密钥
            description: 备份描述

        Returns:
            创建的备份记录
        """
        from ..services.plugin_service import PluginService

        plugin_service = PluginService()
        plugin_files = []

        # 收集插件文件
        for plugin_id in plugin_ids:
            plugin = await plugin_service.get_plugin_by_id(plugin_id)
            if plugin and plugin.file_path:
                plugin_files.append({
                    'id': plugin.id,
                    'name': plugin.name,
                    'version': plugin.version,
                    'file_path': plugin.file_path,
                    'file_hash': plugin.file_hash
                })

        if not plugin_files:
            raise ValueError("没有找到有效的插件文件")

        # 创建临时目录
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_path = Path(temp_dir)
            snapshot_dir = temp_path / f"plugin_snapshot_{snapshot_name}"
            snapshot_dir.mkdir()

            # 复制插件文件
            for plugin_info in plugin_files:
                source_file = Path(plugin_info['file_path'])
                if source_file.exists():
                    dest_file = snapshot_dir / f"{plugin_info['name']}_{plugin_info['version']}{source_file.suffix}"
                    shutil.copy2(source_file, dest_file)

            # 创建快照信息文件
            snapshot_info = {
                'name': snapshot_name,
                'description': description,
                'created_at': datetime.now().isoformat(),
                'plugins': plugin_files,
                'type': 'plugin_snapshot'
            }

            info_file = snapshot_dir / 'snapshot_info.json'
            with open(info_file, 'w', encoding='utf-8') as f:
                json.dump(snapshot_info, f, indent=2, ensure_ascii=False)

            # 创建ZIP压缩包
            import zipfile
            zip_path = self.temp_dir / f"{snapshot_name}.zip"
            with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
                for file_path in snapshot_dir.rglob('*'):
                    if file_path.is_file():
                        arcname = file_path.relative_to(snapshot_dir)
                        zipf.write(file_path, arcname)

            # 创建备份记录
            return await self.upload_backup(
                name=snapshot_name,
                backup_type='plugin_snapshot',
                user_key=user_key,
                file_content=zip_path.read_bytes(),
                filename=f"{snapshot_name}.zip",
                description=description,
                metadata={
                    'plugin_count': len(plugin_files),
                    'plugin_ids': plugin_ids,
                    'snapshot_type': 'plugin_snapshot'
                }
            )

    async def get_backup_statistics(self) -> Dict[str, Any]:
        """
        获取备份统计信息

        Returns:
            备份统计信息
        """
        async with get_session() as session:
            # 总备份数
            total_query = select(Backup)
            total_result = await session.execute(total_query)
            total_backups = len(total_result.scalars().all())

            # 按类型统计
            from sqlalchemy import func

            type_query = select(
                Backup.backup_type,
                func.count(Backup.id).label('count'),
                func.sum(Backup.file_size).label('total_size')
            ).group_by(Backup.backup_type)

            type_result = await session.execute(type_query)
            type_stats = type_result.all()

            # 激活备份数
            active_query = select(Backup).where(Backup.is_active == True)
            active_result = await session.execute(active_query)
            active_backups = len(active_result.scalars().all())

            return {
                "total_backups": total_backups,
                "active_backups": active_backups,
                "inactive_backups": total_backups - active_backups,
                "backup_types": [
                    {
                        "type": stat.backup_type,
                        "count": stat.count,
                        "total_size": stat.total_size or 0
                    }
                    for stat in type_stats
                ],
                "total_storage_size": sum(stat.total_size or 0 for stat in type_stats)
            }