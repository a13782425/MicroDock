"""
JWT 认证模块

提供 JWT token 的生成、验证和依赖注入功能
"""
from datetime import datetime, timedelta
from typing import Optional
from fastapi import Depends, HTTPException, status
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from jose import JWTError, jwt
from passlib.context import CryptContext
from pydantic import BaseModel

from app.config import settings


# 密码加密上下文
pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")

# HTTP Bearer 认证方案
security = HTTPBearer(auto_error=False)


class TokenData(BaseModel):
    """Token 数据模型"""
    username: str
    is_admin: bool = True


class Token(BaseModel):
    """Token 响应模型"""
    access_token: str
    token_type: str = "bearer"
    expires_in: int  # 过期时间（秒）


def verify_password(plain_password: str, hashed_password: str) -> bool:
    """
    验证密码
    
    Args:
        plain_password: 明文密码
        hashed_password: 哈希密码
    
    Returns:
        bool: 是否匹配
    """
    return pwd_context.verify(plain_password, hashed_password)


def get_password_hash(password: str) -> str:
    """
    获取密码哈希值
    
    Args:
        password: 明文密码
    
    Returns:
        str: 哈希后的密码
    """
    return pwd_context.hash(password)


def authenticate_admin(username: str, password: str) -> bool:
    """
    验证管理员账号密码
    
    Args:
        username: 用户名
        password: 密码
    
    Returns:
        bool: 验证是否成功
    """
    # 直接比较明文（配置文件中的密码是明文）
    return (
        username == settings.ADMIN_USERNAME and 
        password == settings.ADMIN_PASSWORD
    )


def create_access_token(data: dict, expires_delta: Optional[timedelta] = None) -> str:
    """
    创建 JWT access token
    
    Args:
        data: 要编码的数据
        expires_delta: 过期时间增量
    
    Returns:
        str: JWT token
    """
    to_encode = data.copy()
    
    if expires_delta:
        expire = datetime.utcnow() + expires_delta
    else:
        expire = datetime.utcnow() + timedelta(minutes=settings.JWT_EXPIRE_MINUTES)
    
    to_encode.update({"exp": expire})
    encoded_jwt = jwt.encode(
        to_encode, 
        settings.JWT_SECRET_KEY, 
        algorithm="HS256"
    )
    return encoded_jwt


def verify_token(token: str) -> Optional[TokenData]:
    """
    验证 JWT token
    
    Args:
        token: JWT token
    
    Returns:
        TokenData: token 数据，验证失败返回 None
    """
    try:
        payload = jwt.decode(
            token, 
            settings.JWT_SECRET_KEY, 
            algorithms=["HS256"]
        )
        username: str = payload.get("sub")
        if username is None:
            return None
        return TokenData(username=username, is_admin=True)
    except JWTError:
        return None


async def get_current_admin(
    credentials: Optional[HTTPAuthorizationCredentials] = Depends(security)
) -> Optional[TokenData]:
    """
    获取当前管理员（可选）
    
    用于需要区分管理员和普通用户的接口。
    如果提供了有效 token，返回管理员信息；否则返回 None。
    
    Args:
        credentials: HTTP Bearer 凭证
    
    Returns:
        TokenData: 管理员信息，未登录或无效返回 None
    """
    if credentials is None:
        return None
    
    token_data = verify_token(credentials.credentials)
    return token_data


async def require_admin(
    credentials: Optional[HTTPAuthorizationCredentials] = Depends(security)
) -> TokenData:
    """
    要求管理员权限
    
    用于需要管理员权限的接口。
    如果未登录或 token 无效，抛出 401 错误。
    
    Args:
        credentials: HTTP Bearer 凭证
    
    Returns:
        TokenData: 管理员信息
    
    Raises:
        HTTPException: 未授权时抛出 401 错误
    """
    credentials_exception = HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="需要管理员登录",
        headers={"WWW-Authenticate": "Bearer"},
    )
    
    if credentials is None:
        raise credentials_exception
    
    token_data = verify_token(credentials.credentials)
    if token_data is None:
        raise credentials_exception
    
    return token_data

