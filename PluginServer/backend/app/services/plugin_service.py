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
from app.utils.validators import validate_upload_file, validate_key_or_raise


class PluginService:
    """插件服务"""
    
    @staticmethod
    async def get_all_plugins(db: AsyncSession) -> List[dict]:
        """获取所有插件（包含总下载次数）"""
        # 1. 获取所有插件
        result = await db.execute(select(Plugin).order_by(Plugin.created_at.desc()))
        plugins = list(result.scalars().all())
        
        # 2. 获取每个插件的下载次数总和
        download_counts_query = select(
            PluginVersion.plugin_name,
            func.sum(PluginVersion.download_count).label('total_download_count')
        ).group_by(PluginVersion.plugin_name)
        
        download_result = await db.execute(download_counts_query)
        download_counts = {row.plugin_name: row.total_download_count or 0 for row in download_result}
        
        # 3. 将下载次数附加到插件对象
        plugins_with_downloads = []
        for plugin in plugins:
            plugin_dict = {
                'name': plugin.name,
                'display_name': plugin.display_name,
                'current_version': plugin.current_version,
                'description': plugin.description,
                'author': plugin.author,
                'license': plugin.license,
                'homepage': plugin.homepage,
                'main_dll': plugin.main_dll,
                'entry_class': plugin.entry_class,
                'is_enabled': plugin.is_enabled,
                'is_deprecated': plugin.is_deprecated,
                'total_download_count': download_counts.get(plugin.name, 0),
                'created_at': plugin.created_at,
                'updated_at': plugin.updated_at,
            }
            plugins_with_downloads.append(plugin_dict)
        
        return plugins_with_downloads
    
    @staticmethod
    async def get_plugin_by_name(db: AsyncSession, name: str) -> Optional[Plugin]:
        """根据名称获取插件（主键查询）"""
        result = await db.execute(select(Plugin).where(Plugin.name == name))
        return result.scalar_one_or_none()
    
    @staticmethod
    async def create_plugin_from_upload(
        db: AsyncSession,
        file: UploadFile,
        plugin_key: str
    ) -> Plugin:
        """
        从上传的ZIP文件创建插件
        
        Args:
            db: 数据库会话
            file: 上传的ZIP文件
            plugin_key: 插件密钥（首次上传绑定，后续验证）
            
        Returns:
            Plugin: 创建的插件对象
        """
        # 1. 验证文件
        await validate_upload_file(file)
        
        # 2. 验证 plugin_key 格式
        validate_key_or_raise(plugin_key, "插件密钥")
        
        # 3. 临时保存文件用于解析
        temp_path = Path("./data/temp") / file.filename
        temp_path.parent.mkdir(parents=True, exist_ok=True)
        
        try:
            # 保存临时文件
            with open(temp_path, 'wb') as f:
                content = await file.read()
                f.write(content)
            
            # 4. 解析 plugin.json
            plugin_data = await FileService.parse_plugin_json(temp_path)
            plugin_name = plugin_data['name']
            plugin_version = plugin_data['version']
            
            # 5. 检查插件是否已存在
            existing_plugin = await PluginService.get_plugin_by_name(db, plugin_name)
            if existing_plugin:
                # 验证 plugin_key 是否匹配
                if existing_plugin.upload_key != plugin_key:
                    raise HTTPException(
                        status_code=403,
                        detail="插件密钥不匹配，无权更新此插件"
                    )
                # 检查版本是否存在
                exists = await VersionService.check_version_exists(db, plugin_name, plugin_version)
                if exists:
                    raise HTTPException(
                        status_code=409,
                        detail=f"插件 '{plugin_name}' 的版本 '{plugin_version}' 已存在"
                    )
            
            # 6. 创建或更新插件
            if existing_plugin:
                plugin = existing_plugin
            else:
                plugin = Plugin(
                    name=plugin_name,
                    display_name=plugin_data.get('displayName', plugin_name),
                    description=plugin_data.get('description', ''),
                    author=plugin_data.get('author', ''),
                    license=plugin_data.get('license', ''),
                    homepage=plugin_data.get('homepage', ''),
                    main_dll=plugin_data['main'],
                    entry_class=plugin_data['entryClass'],
                    upload_key=plugin_key,
                )
                db.add(plugin)
                await db.flush()
            
            # 7. 重置文件指针并保存（使用插件名和版本号命名）
            file.file.seek(0)
            file_path, file_size = await FileService.save_upload_file(
                file, plugin_name, plugin_version
            )
            
            # 8. 计算文件哈希
            file_hash = await calculate_file_hash(file_path)
            
            # 9. 生成规范的文件名：{plugin_name}@{version}.zip
            formatted_file_name = f"{plugin_name}@{plugin_version}.zip"
            
            # 10. 创建版本记录
            version = PluginVersion(
                plugin_name=plugin_name,
                version=plugin_version,
                file_name=formatted_file_name,
                file_path=str(file_path),
                file_size=file_size,
                file_hash=file_hash,
                changelog=plugin_data.get('changelog', ''),
                dependencies=json.dumps(plugin_data.get('dependencies', {})),
                engines=json.dumps(plugin_data.get('engines', {})),
            )
            db.add(version)
            
            # 11. 更新插件的当前版本
            plugin.current_version = plugin_version
            
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
        name: str,
        is_enabled: Optional[bool] = None,
        is_deprecated: Optional[bool] = None
    ) -> Plugin:
        """更新插件信息（使用插件名）"""
        plugin = await PluginService.get_plugin_by_name(db, name)
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
    async def delete_plugin(db: AsyncSession, name: str) -> None:
        """删除插件（使用插件名，包括所有版本和文件）"""
        plugin = await PluginService.get_plugin_by_name(db, name)
        if not plugin:
            raise HTTPException(status_code=404, detail="插件不存在")
        
        # 1. 删除所有文件
        await FileService.delete_plugin_directory(name)
        
        # 2. 删除插件（版本会通过级联删除自动删除）
        await db.delete(plugin)
        await db.commit()
