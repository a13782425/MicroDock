"""
文件验证工具
"""
from pathlib import Path
from fastapi import UploadFile, HTTPException
from app.config import settings


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
