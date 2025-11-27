"""
文件验证工具
"""
import re
from pathlib import Path
from fastapi import UploadFile, HTTPException
from app.config import settings


# Key 验证常量
MAX_KEY_LENGTH = 256
KEY_PATTERN = re.compile(r'^[a-zA-Z0-9_]+$')


def validate_key(key: str) -> bool:
    """
    验证 key 是否有效
    
    规则：
    - 只允许字母、数字、下划线 (a-z, A-Z, 0-9, _)
    - 最大长度 256 个字符
    - 不能为空
    
    Args:
        key: 待验证的 key
        
    Returns:
        bool: 是否有效
    """
    if not key or len(key) > MAX_KEY_LENGTH:
        return False
    return bool(KEY_PATTERN.match(key))


def validate_key_or_raise(key: str, key_name: str = "Key") -> None:
    """
    验证 key，无效则抛出 HTTPException
    
    Args:
        key: 待验证的 key
        key_name: key 的名称（用于错误信息）
        
    Raises:
        HTTPException: key 验证失败
    """
    if not key:
        raise HTTPException(
            status_code=400,
            detail=f"{key_name} 不能为空"
        )
    if len(key) > MAX_KEY_LENGTH:
        raise HTTPException(
            status_code=400,
            detail=f"{key_name} 长度不能超过 {MAX_KEY_LENGTH} 个字符"
        )
    if not KEY_PATTERN.match(key):
        raise HTTPException(
            status_code=400,
            detail=f"{key_name} 格式无效，只允许字母、数字和下划线"
        )


def validate_file_extension(filename: str) -> bool:
    """
    验证文件扩展名
    
    Args:
        filename: 文件名
        
    Returns:
        bool: 是否有效
    """
    file_path = Path(filename)
    return file_path.suffix.lower() in settings.ALLOWED_EXTENSIONS


async def validate_upload_file(file: UploadFile) -> None:
    """
    验证上传的文件
    
    Args:
        file: 上传的文件
        
    Raises:
        HTTPException: 文件验证失败
    """
    # 验证文件名
    if not file.filename:
        raise HTTPException(status_code=400, detail="文件名不能为空")
    
    # 验证文件扩展名
    if not validate_file_extension(file.filename):
        raise HTTPException(
            status_code=400,
            detail=f"不支持的文件格式，仅支持: {', '.join(settings.ALLOWED_EXTENSIONS)}"
        )
    
    # 验证文件大小
    file.file.seek(0, 2)  # 移动到文件末尾
    file_size = file.file.tell()
    file.file.seek(0)  # 重置到文件开头
    
    if file_size > settings.MAX_UPLOAD_SIZE:
        max_size_mb = settings.MAX_UPLOAD_SIZE / (1024 * 1024)
        raise HTTPException(
            status_code=400,
            detail=f"文件大小超过限制（最大 {max_size_mb}MB）"
        )
    
    if file_size == 0:
        raise HTTPException(status_code=400, detail="文件不能为空")
