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
    def _get_backup_dir(user_key: str, backup_type: str) -> Path:
        """获取用户备份目录"""
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
        description: str = ""
    ) -> Backup:
        """
        创建备份
        
        Args:
            db: 数据库会话
            user_key: 用户密钥
            backup_type: 备份类型 (program | plugin)
            file: 上传的文件
            description: 备份描述
            
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
        
        # 3. 验证文件
        if not file.filename:
            raise HTTPException(status_code=400, detail="文件名不能为空")
        
        # 4. 创建备份目录
        backup_dir = BackupService._get_backup_dir(user_key, backup_type)
        backup_dir.mkdir(parents=True, exist_ok=True)
        
        # 5. 生成文件路径（使用时间戳避免冲突）
        from datetime import datetime
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        safe_filename = f"{timestamp}_{file.filename}"
        file_path = backup_dir / safe_filename
        
        # 6. 保存文件
        file_size = 0
        async with aiofiles.open(file_path, 'wb') as f:
            while chunk := await file.read(8192):
                await f.write(chunk)
                file_size += len(chunk)
        
        # 7. 计算文件哈希
        file_hash = await calculate_file_hash(file_path)
        
        # 8. 创建数据库记录
        backup = Backup(
            user_key=user_key,
            backup_type=backup_type,
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

