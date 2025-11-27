"""
备份服务：处理用户备份相关的业务逻辑
"""
import shutil
from typing import List, Optional
from pathlib import Path
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select
from fastapi import HTTPException, UploadFile
import aiofiles

from app.models.backup import Backup
from app.config import settings
from app.utils.hash import calculate_file_hash
from app.utils.validators import validate_key_or_raise

# 备份存储根目录
BACKUP_DIR = Path("./data/backups")

# 允许的备份类型
ALLOWED_BACKUP_TYPES = {"program", "plugin"}


class BackupService:
    """备份服务"""
    
    @staticmethod
    def _get_backup_dir(user_key: str, backup_type: str, plugin_name: Optional[str] = None) -> Path:
        """
        获取用户备份目录
        
        目录结构:
        - program: backups/{user_key}/program/
        - plugin:  backups/{user_key}/plugin/{plugin_name}/
        """
        if backup_type == "plugin" and plugin_name:
            return BACKUP_DIR / user_key / backup_type / plugin_name
        return BACKUP_DIR / user_key / backup_type
    
    @staticmethod
    async def get_user_backups(db: AsyncSession, user_key: str) -> List[Backup]:
        """获取用户的所有备份"""
        result = await db.execute(
            select(Backup)
            .where(Backup.user_key == user_key)
            .order_by(Backup.created_at.desc())
        )
        return list(result.scalars().all())
    
    @staticmethod
    async def get_all_backups(db: AsyncSession) -> List[Backup]:
        """获取所有用户的备份（管理员用）"""
        result = await db.execute(
            select(Backup)
            .order_by(Backup.created_at.desc())
        )
        return list(result.scalars().all())
    
    @staticmethod
    async def get_backup_by_id(db: AsyncSession, backup_id: int) -> Optional[Backup]:
        """根据ID获取备份"""
        result = await db.execute(select(Backup).where(Backup.id == backup_id))
        return result.scalar_one_or_none()
    
    @staticmethod
    async def create_backup(
        db: AsyncSession,
        user_key: str,
        backup_type: str,
        file: UploadFile,
        description: str = "",
        plugin_name: Optional[str] = None
    ) -> Backup:
        """
        创建备份
        
        Args:
            db: 数据库会话
            user_key: 用户密钥
            backup_type: 备份类型 (program | plugin)
            file: 上传的文件
            description: 备份描述
            plugin_name: 插件名称（仅 plugin 类型需要）
            
        Returns:
            Backup: 创建的备份对象
        """
        # 1. 验证 user_key 格式
        validate_key_or_raise(user_key, "用户密钥")
        
        # 2. 验证备份类型
        if backup_type not in ALLOWED_BACKUP_TYPES:
            raise HTTPException(
                status_code=400,
                detail=f"无效的备份类型，只允许: {', '.join(ALLOWED_BACKUP_TYPES)}"
            )
        
        # 3. 验证 plugin 类型必须提供 plugin_name
        if backup_type == "plugin" and not plugin_name:
            raise HTTPException(
                status_code=400,
                detail="备份插件时必须提供插件名称 (plugin_name)"
            )
        
        # 4. 验证文件
        if not file.filename:
            raise HTTPException(status_code=400, detail="文件名不能为空")
        
        # 5. 创建备份目录
        backup_dir = BackupService._get_backup_dir(user_key, backup_type, plugin_name)
        backup_dir.mkdir(parents=True, exist_ok=True)
        
        # 6. 先保存到临时文件以计算哈希
        temp_path = backup_dir / f"temp_{file.filename}"
        file_size = 0
        async with aiofiles.open(temp_path, 'wb') as f:
            while chunk := await file.read(8192):
                await f.write(chunk)
                file_size += len(chunk)
        
        # 7. 计算文件哈希
        file_hash = await calculate_file_hash(temp_path)
        
        # 8. 使用哈希前8位作为文件名（简洁且唯一）
        ext = Path(file.filename).suffix or ".zip"
        safe_filename = f"{file_hash[:8]}{ext}"
        file_path = backup_dir / safe_filename
        
        # 9. 重命名临时文件为最终文件名
        temp_path.rename(file_path)
        
        # 10. 创建数据库记录
        backup = Backup(
            user_key=user_key,
            backup_type=backup_type,
            plugin_name=plugin_name,
            file_name=file.filename,
            file_path=str(file_path),
            file_size=file_size,
            file_hash=file_hash,
            description=description,
        )
        db.add(backup)
        await db.commit()
        await db.refresh(backup)
        
        return backup
    
    @staticmethod
    async def download_backup(
        db: AsyncSession,
        user_key: str,
        backup_id: int
    ) -> tuple[Path, str]:
        """
        获取备份下载信息
        
        Args:
            db: 数据库会话
            user_key: 用户密钥
            backup_id: 备份ID
            
        Returns:
            tuple[Path, str]: (文件路径, 文件名)
        """
        # 1. 验证 user_key 格式
        validate_key_or_raise(user_key, "用户密钥")
        
        # 2. 获取备份记录
        backup = await BackupService.get_backup_by_id(db, backup_id)
        if not backup:
            raise HTTPException(status_code=404, detail="备份不存在")
        
        # 3. 验证用户权限
        if backup.user_key != user_key:
            raise HTTPException(status_code=403, detail="用户密钥不匹配，无权访问此备份")
        
        # 4. 检查文件是否存在
        file_path = Path(backup.file_path)
        if not file_path.exists():
            raise HTTPException(status_code=404, detail="备份文件不存在")
        
        return file_path, backup.file_name
    
    @staticmethod
    async def delete_backup(
        db: AsyncSession,
        user_key: str,
        backup_id: int
    ) -> None:
        """
        删除备份
        
        Args:
            db: 数据库会话
            user_key: 用户密钥
            backup_id: 备份ID
        """
        # 1. 验证 user_key 格式
        validate_key_or_raise(user_key, "用户密钥")
        
        # 2. 获取备份记录
        backup = await BackupService.get_backup_by_id(db, backup_id)
        if not backup:
            raise HTTPException(status_code=404, detail="备份不存在")
        
        # 3. 验证用户权限
        if backup.user_key != user_key:
            raise HTTPException(status_code=403, detail="用户密钥不匹配，无权删除此备份")
        
        # 4. 删除文件
        file_path = Path(backup.file_path)
        if file_path.exists():
            file_path.unlink()
        
        # 5. 删除数据库记录
        await db.delete(backup)
        await db.commit()

