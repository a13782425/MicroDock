"""
文件服务：处理文件上传、存储和ZIP解析
"""
import json
import zipfile
import shutil
from pathlib import Path
from typing import Dict, Any, Tuple
from fastapi import UploadFile, HTTPException
import aiofiles

from app.config import settings
from app.utils.hash import calculate_file_hash


class FileService:
    """文件处理服务"""
    
    @staticmethod
    async def save_upload_file(file: UploadFile, plugin_id: int, version_id: int) -> Tuple[Path, int]:
        """
        保存上传的文件
        
        Args:
            file: 上传的文件
            plugin_id: 插件ID
            version_id: 版本ID
            
        Returns:
            Tuple[Path, int]: (文件路径, 文件大小)
        """
        # 创建插件目录
        plugin_dir = settings.UPLOAD_DIR / str(plugin_id)
        plugin_dir.mkdir(parents=True, exist_ok=True)
        
        # 生成文件名：{version_id}_{original_filename}
        file_name = f"{version_id}_{file.filename}"
        file_path = plugin_dir / file_name
        
        # 保存文件
        file_size = 0
        async with aiofiles.open(file_path, 'wb') as f:
            while chunk := await file.read(8192):
                await f.write(chunk)
                file_size += len(chunk)
        
        return file_path, file_size
    
    @staticmethod
    async def parse_plugin_json(file_path: Path) -> Dict[str, Any]:
        """
        解析ZIP文件中的plugin.json
        
        Args:
            file_path: ZIP文件路径
            
        Returns:
            Dict: plugin.json的内容
            
        Raises:
            HTTPException: 解析失败
        """
        try:
            with zipfile.ZipFile(file_path, 'r') as zip_ref:
                # 查找根目录的plugin.json
                plugin_json_path = None
                for file_info in zip_ref.filelist:
                    # 检查是否是根目录的plugin.json
                    filename = file_info.filename.replace('\\', '/')
                    if filename == 'plugin.json' or filename.endswith('/plugin.json'):
                        # 确保是根目录（没有父目录或只有一层目录）
                        parts = filename.split('/')
                        if len(parts) <= 2:  # 根目录或一级子目录
                            plugin_json_path = filename
                            break
                
                if not plugin_json_path:
                    raise HTTPException(
                        status_code=400,
                        detail="ZIP文件中未找到plugin.json文件（必须在根目录）"
                    )
                
                # 读取并解析plugin.json
                with zip_ref.open(plugin_json_path) as json_file:
                    plugin_data = json.loads(json_file.read().decode('utf-8'))
                
                # 验证必需字段
                required_fields = ['name', 'version', 'main', 'entryClass']
                missing_fields = [field for field in required_fields if field not in plugin_data]
                
                if missing_fields:
                    raise HTTPException(
                        status_code=400,
                        detail=f"plugin.json缺少必需字段: {', '.join(missing_fields)}"
                    )
                
                return plugin_data
                
        except zipfile.BadZipFile:
            raise HTTPException(status_code=400, detail="无效的ZIP文件")
        except json.JSONDecodeError as e:
            raise HTTPException(status_code=400, detail=f"plugin.json格式错误: {str(e)}")
        except Exception as e:
            if isinstance(e, HTTPException):
                raise
            raise HTTPException(status_code=500, detail=f"解析ZIP文件失败: {str(e)}")
    
    @staticmethod
    async def delete_file(file_path: Path) -> None:
        """
        删除文件
        
        Args:
            file_path: 文件路径
        """
        if file_path.exists():
            file_path.unlink()
    
    @staticmethod
    async def delete_plugin_directory(plugin_id: int) -> None:
        """
        删除插件的所有文件
        
        Args:
            plugin_id: 插件ID
        """
        plugin_dir = settings.UPLOAD_DIR / str(plugin_id)
        if plugin_dir.exists():
            shutil.rmtree(plugin_dir)
