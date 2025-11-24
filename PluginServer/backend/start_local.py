"""
本地开发启动脚本
"""
import subprocess
import sys
import os
from pathlib import Path

def check_python_version():
    """检查 Python 版本"""
    if sys.version_info < (3, 11):
        print("❌ 错误: 需要 Python 3.11 或更高版本")
        print(f"   当前版本: {sys.version}")
        sys.exit(1)
    print(f"✓ Python 版本: {sys.version_info.major}.{sys.version_info.minor}")

def install_dependencies():
    """安装依赖"""
    print("\n📦 安装 Python 依赖...")
    try:
        subprocess.check_call([
            sys.executable, "-m", "pip", "install", "-r", "requirements.txt", "-q"
        ])
        print("✓ 依赖安装完成")
        return True
    except subprocess.CalledProcessError as e:
        print(f"❌ 依赖安装失败: {e}")
        return False

def create_directories():
    """创建必要的目录"""
    print("\n📁 创建数据目录...")
    directories = ["./data", "./data/uploads", "./data/temp"]
    for dir_path in directories:
        Path(dir_path).mkdir(parents=True, exist_ok=True)
    print("✓ 目录创建完成")

def start_server():
    """启动服务器"""
    print("\n🚀 启动 FastAPI 服务器...")
    print("━" * 60)
    print("📍 后端服务: http://localhost:8000")
    print("📚 API 文档: http://localhost:8000/docs")
    print("🔍 健康检查: http://localhost:8000/api/health")
    print("━" * 60)
    print("\n按 Ctrl+C 停止服务器\n")
    
    try:
        # 启动 uvicorn
        subprocess.run([
            sys.executable, "-m", "uvicorn",
            "app.main:app",
            "--host", "0.0.0.0",
            "--port", "8000",
            "--reload"
        ])
    except KeyboardInterrupt:
        print("\n\n✓ 服务器已停止")
    except Exception as e:
        print(f"\n❌ 启动失败: {e}")
        sys.exit(1)

def main():
    """主函数"""
    print("=" * 60)
    print("MicroDock 插件管理服务器 - 本地开发")
    print("=" * 60)
    
    # 检查 Python 版本
    check_python_version()
    
    # 安装依赖
    if not install_dependencies():
        sys.exit(1)
    
    # 创建目录
    create_directories()
    
    # 启动服务器
    start_server()

if __name__ == "__main__":
    main()