"""
哈希计算工具
"""
import hashlib
import aiofiles
from pathlib import Path


async def calculate_file_hash(file_path: Path) -> str:
    """
    计算文件的 SHA256 哈希值
    
    Args:
        file_path: 文件路径
        
    Returns:
        str: SHA256 哈希值（十六进制字符串）
    """
    sha256_hash = hashlib.sha256()
    
    async with aiofiles.open(file_path, 'rb') as f:
        # 分块读取文件以处理大文件
        while chunk := await f.read(8192):
            sha256_hash.update(chunk)
    
    return sha256_hash.hexdigest()
