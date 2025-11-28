"""
认证 API 路由

提供管理员登录、登出和状态查询功能
"""
from datetime import timedelta
from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import BaseModel

from app.config import settings
from app.schemas.common import ApiResponse
from app.utils.auth import (
    authenticate_admin,
    create_access_token,
    get_current_admin,
    require_admin,
    Token,
    TokenData
)


router = APIRouter(prefix="/api/auth", tags=["auth"])


class LoginRequest(BaseModel):
    """登录请求"""
    username: str
    password: str


class LoginData(BaseModel):
    """登录响应数据"""
    token: str
    token_type: str = "bearer"
    expires_in: int  # 过期时间（秒）


class AuthStatusData(BaseModel):
    """认证状态数据"""
    is_logged_in: bool
    username: str | None = None
    is_admin: bool = False


@router.post("/login", response_model=ApiResponse[LoginData])
async def login(request: LoginRequest):
    """
    管理员登录
    
    验证用户名和密码，成功后返回 JWT token。
    
    - **username**: 管理员用户名
    - **password**: 管理员密码
    """
    if not authenticate_admin(request.username, request.password):
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="用户名或密码错误",
            headers={"WWW-Authenticate": "Bearer"},
        )
    
    # 创建 token
    expires_delta = timedelta(minutes=settings.JWT_EXPIRE_MINUTES)
    access_token = create_access_token(
        data={"sub": request.username},
        expires_delta=expires_delta
    )
    
    data = LoginData(
        token=access_token,
        token_type="bearer",
        expires_in=settings.JWT_EXPIRE_MINUTES * 60  # 转换为秒
    )
    
    return ApiResponse.ok(data=data, message="登录成功")


@router.get("/me", response_model=ApiResponse[AuthStatusData])
async def get_auth_status(
    current_admin: TokenData | None = Depends(get_current_admin)
):
    """
    获取当前认证状态
    
    返回当前是否已登录以及登录用户信息。
    """
    if current_admin is None:
        data = AuthStatusData(
            is_logged_in=False,
            username=None,
            is_admin=False
        )
        return ApiResponse.ok(data=data, message="未登录")
    
    data = AuthStatusData(
        is_logged_in=True,
        username=current_admin.username,
        is_admin=current_admin.is_admin
    )
    return ApiResponse.ok(data=data, message="已登录")


@router.post("/logout", response_model=ApiResponse[None])
async def logout():
    """
    登出
    
    由于使用 JWT，服务端不需要做任何操作。
    前端需要清除本地存储的 token。
    """
    return ApiResponse.ok(message="登出成功，请在客户端清除 token")
