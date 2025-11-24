"""
插件服务：处理插件相关的业务逻辑
"""
import json
from typing import List, Optional
from pathlib import Path
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select, func
from fastapi import HTTPException, UploadFile

from app.models.plugin import Plugin
from app.models.version import PluginVersion
from app.services.file_service import FileService
from app.services.version_service import VersionService
from app.utils.hash import calculate_file_hash
from app.utils.validators import validate_upload_file


class PluginService:
    """插件服务"""
    
    @staticmethod
    async def get_all_plugins(db: AsyncSession) -> List[Plugin]:
        """获取所有插件"""
        result = await db.execute(select(Plugin).order_by(Plugin.created_at.desc()))
        return list(result.scalars().all())
    
    @staticmethod
    async def get_plugin_by_id(db: AsyncSession, plugin_id: int) -> Optional[Plugin]:
        """根据ID获取插件"""
        result = await db.execute(select(Plugin).where(Plugin.id == plugin_id))
        return result.scalar_one_or_none()
    
    @staticmethod
    async def get_plugin_by_name(db: AsyncSession, name: str) -> Optional[Plugin]:
        """根据名称获取插件"""
        result = await db.execute(select(Plugin).where(Plugin.name == name))
        return result.scalar_one_or_none()
    
    @staticmethod
    async def create_plugin_from_upload(
        db: AsyncSession,
        file: UploadFile
    ) -> Plugin:
        """
        从上传的ZIP文件创建插件
        
        Args:
            db: 数据库会话
            file: 上传的ZIP文件
            
        Returns:
            Plugin: 创建的插件对象
        """
        # 1. 验证文件
        await validate_upload_file(file)
        
        # 2. 临时保存文件用于解析
        temp_path = Path("./data/temp") / file.filename
        temp_path.parent.mkdir(parents=True, exist_ok=True)
        
        try:
            # 保存临时文件
            with open(temp_path, 'wb') as f:
                content = await file.read()
                f.write(content)
            
            # 3. 解析 plugin.json
            plugin_data = await FileService.parse_plugin_json(temp_path)
            
            # 4. 检查插件是否已存在该版本
            existing_plugin = await PluginService.get_plugin_by_name(db, plugin_data['name'])
            if existing_plugin:
                # 检查版本是否存在
                exists = await VersionService.check_version_exists(
                    db, existing_plugin.id, plugin_data['version']
                )
                if exists:
                    raise HTTPException(
                        status_code=409,
                        detail=f"插件 '{plugin_data['name']}' 的版本 '{plugin_data['version']}' 已存在"
                    )
            
            # 5. 创建或更新插件
            if existing_plugin:
                plugin = existing_plugin
            else:
                plugin = Plugin(
                    name=plugin_data['name'],
                    display_name=plugin_data.get('displayName', plugin_data['name']),
                    version_number=plugin_data['version'],
                    description=plugin_data.get('description', ''),
                    author=plugin_data.get('author', ''),
                    license=plugin_data.get('license', ''),
                    homepage=plugin_data.get('homepage', ''),
                    main_dll=plugin_data['main'],
                    entry_class=plugin_data['entryClass'],
                )
                db.add(plugin)
                await db.flush()  # 刷新以获取ID
            
            # 6. 重置文件指针并保存
            file.file.seek(0)
            file_path, file_size = await FileService.save_upload_file(file, plugin.id, 0)
            
            # 7. 计算文件哈希
            file_hash = await calculate_file_hash(file_path)
            
            # 8. 创建版本记录
            version = PluginVersion(
                plugin_id=plugin.id,
                version=plugin_data['version'],
                file_name=file.filename,
                file_path=str(file_path),
                file_size=file_size,
                file_hash=file_hash,
                changelog=plugin_data.get('changelog', ''),
                dependencies=json.dumps(plugin_data.get('dependencies', {})),
                engines=json.dumps(plugin_data.get('engines', {})),
            )
            db.add(version)
            await db.flush()
            
            # 9. 更新插件的当前版本
            plugin.current_version_id = version.id
            plugin.version_number = version.version
            
            # 10. 重命名文件（使用正确的version_id）
            new_file_path = file_path.parent / f"{version.id}_{file.filename}"
            file_path.rename(new_file_path)
            version.file_path = str(new_file_path)
            
            await db.commit()
            await db.refresh(plugin)
            
            return plugin
            
        finally:
            # 清理临时文件
            if temp_path.exists():
                temp_path.unlink()
    
    @staticmethod
    async def update_plugin(
        db: AsyncSession,
        plugin_id: int,
        is_enabled: Optional[bool] = None,
        is_deprecated: Optional[bool] = None
    ) -> Plugin:
        """更新插件信息"""
        plugin = await PluginService.get_plugin_by_id(db, plugin_id)
        if not plugin:
            raise HTTPException(status_code=404, detail="插件不存在")
        
        if is_enabled is not None:
            plugin.is_enabled = is_enabled
        if is_deprecated is not None:
            plugin.is_deprecated = is_deprecated
        
        await db.commit()
        await db.refresh(plugin)
        return plugin
    
    @staticmethod
    async def delete_plugin(db: AsyncSession, plugin_id: int) -> None:
        """删除插件（包括所有版本和文件）"""
        plugin = await PluginService.get_plugin_by_id(db, plugin_id)
        if not plugin:
            raise HTTPException(status_code=404, detail="插件不存在")
        
        # 删除所有文件
        await FileService.delete_plugin_directory(plugin_id)
        
        # 删除数据库记录
        await db.delete(plugin)
        await db.commit()
