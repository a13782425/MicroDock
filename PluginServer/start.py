"""
MicroDock 插件服务器一键启动脚本
"""
import subprocess
import sys
import os
import time
import webbrowser
import threading
from pathlib import Path
import signal

def check_environment():
    """检查环境"""
    print("🔍 检查环境...")
    
    # 检查 Python
    if sys.version_info < (3, 11):
        print("❌ 错误: 需要 Python 3.11 或更高版本")
        sys.exit(1)
        
    # 检查 Node.js (简单检查)
    try:
        subprocess.run(["npm", "--version"], capture_output=True, check=True)
    except (subprocess.CalledProcessError, FileNotFoundError):
        print("❌ 错误: 未找到 npm，请安装 Node.js")
        sys.exit(1)
        
    print("✓ 环境检查通过")

def install_dependencies():
    """安装依赖"""
    print("\n📦 安装依赖...")
    
    # 后端依赖
    print("  - 安装后端依赖...")
    subprocess.check_call([
        sys.executable, "-m", "pip", "install", "-r", "backend/requirements.txt", "-q"
    ])
    
    # 前端依赖
    print("  - 安装前端依赖...")
    subprocess.check_call(
        ["npm", "install"], 
        cwd="frontend",
        shell=True
    )
    
    print("✓ 依赖安装完成")

def start_backend():
    """启动后端服务"""
    print("🚀 启动后端服务 (Port 8000)...")
    # 确保数据目录存在
    Path("backend/data/uploads").mkdir(parents=True, exist_ok=True)
    Path("backend/data/temp").mkdir(parents=True, exist_ok=True)
    
    return subprocess.Popen(
        [sys.executable, "-m", "uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000", "--reload"],
        cwd="backend"
    )

def start_frontend():
    """启动前端服务"""
    print("🎨 启动前端服务 (Port 3000)...")
    return subprocess.Popen(
        ["npm", "run", "dev"],
        cwd="frontend",
        shell=True
    )

def open_browser():
    """打开浏览器"""
    time.sleep(3)  # 等待服务启动
    print("\n🌐 打开浏览器...")
    webbrowser.open("http://localhost:3000")

def main():
    print("=" * 60)
    print("MicroDock 插件管理系统 - 一键启动")
    print("=" * 60)
    
    try:
        check_environment()
        install_dependencies()
        
        # 启动服务
        backend_process = start_backend()
        frontend_process = start_frontend()
        
        # 打开浏览器
        threading.Thread(target=open_browser, daemon=True).start()
        
        print("\n✅ 服务已启动！")
        print("   后端 API: http://localhost:8000/docs")
        print("   前端界面: http://localhost:3000")
        print("\n按 Ctrl+C 停止所有服务...")
        
        # 等待进程结束
        backend_process.wait()
        frontend_process.wait()
        
    except KeyboardInterrupt:
        print("\n\n🛑 正在停止服务...")
        if 'backend_process' in locals():
            backend_process.terminate()
        if 'frontend_process' in locals():
            # Windows下终止shell启动的子进程比较麻烦，这里简单处理
            if os.name == 'nt':
                subprocess.run(["taskkill", "/F", "/T", "/PID", str(frontend_process.pid)])
            else:
                frontend_process.terminate()
        print("✓ 服务已停止")
        sys.exit(0)
    except Exception as e:
        print(f"\n❌ 发生错误: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()