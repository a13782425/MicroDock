"""
FastAPI 应用主文件
"""
from fastapi import FastAPI, HTTPException, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from fastapi.exceptions import RequestValidationError
from contextlib import asynccontextmanager

from app.config import settings
from app.database import init_db
from app.api import plugins, system, backups, auth


@asynccontextmanager
async def lifespan(app: FastAPI):
    """应用生命周期管理"""
    # 启动时：初始化数据库
    await init_db()
    print("✓ 数据库已初始化")
    
    yield
    
    # 关闭时：清理资源
    print("应用关闭")


# 创建 FastAPI 应用
app = FastAPI(
    title=settings.APP_NAME,
    version=settings.APP_VERSION,
    description="插件管理后台系统 API",
    lifespan=lifespan
)


# ==================== 全局异常处理器 ====================

@app.exception_handler(HTTPException)
async def http_exception_handler(request: Request, exc: HTTPException):
    """
    处理 HTTPException，返回统一格式的错误响应
    """
    return JSONResponse(
        status_code=exc.status_code,
        content={
            "success": False,
            "message": exc.detail,
            "data": None
        }
    )


@app.exception_handler(RequestValidationError)
async def validation_exception_handler(request: Request, exc: RequestValidationError):
    """
    处理请求验证错误，返回统一格式的错误响应
    """
    # 提取第一个错误信息
    errors = exc.errors()
    if errors:
        first_error = errors[0]
        field = ".".join(str(loc) for loc in first_error.get("loc", []))
        message = f"参数验证失败: {field} - {first_error.get('msg', '无效值')}"
    else:
        message = "请求参数验证失败"
    
    return JSONResponse(
        status_code=422,
        content={
            "success": False,
            "message": message,
            "data": None
        }
    )


@app.exception_handler(Exception)
async def general_exception_handler(request: Request, exc: Exception):
    """
    处理未捕获的异常，返回统一格式的错误响应
    """
    # 在调试模式下显示详细错误
    if settings.DEBUG:
        message = f"服务器内部错误: {str(exc)}"
    else:
        message = "服务器内部错误"
    
    return JSONResponse(
        status_code=500,
        content={
            "success": False,
            "message": message,
            "data": None
        }
    )


# ==================== 中间件配置 ====================

# 配置 CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.CORS_ORIGINS,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# 注册路由
app.include_router(auth.router)
app.include_router(plugins.router)
app.include_router(backups.router)
app.include_router(system.router)


@app.get("/")
async def root():
    """根路径"""
    return {
        "message": "MicroDock Plugin Server API",
        "version": settings.APP_VERSION,
        "docs": "/docs",
        "health": "/api/health"
    }


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(
        "main:app",
        host=settings.HOST,
        port=settings.PORT,
        reload=settings.DEBUG
    )
