"""
MicroDock 插件服务器一键启动脚本

支持通过 .env 文件或环境变量配置：
- BACKEND_HOST: 后端监听地址，默认 0.0.0.0
- BACKEND_PORT: 后端端口，默认 8000
- FRONTEND_PORT: 前端端口，默认 3000
- SKIP_INSTALL: 跳过依赖安装，默认 False
- AUTO_OPEN_BROWSER: 自动打开浏览器，默认 True
"""
import subprocess
import sys
import os
import time
import webbrowser
import threading
from pathlib import Path


# ==================== 配置加载 ====================

def load_env_file(env_path: str, override: bool = False) -> dict:
    """
    加载 .env 文件中的环境变量
    
    Args:
        env_path: .env 文件路径
        override: 是否覆盖已存在的环境变量
    
    Returns:
        dict: 解析出的键值对
    """
    result = {}
    if not os.path.exists(env_path):
        return result
    
    with open(env_path, 'r', encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            # 跳过空行和注释
            if not line or line.startswith('#'):
                continue
            # 解析 KEY=VALUE 格式
            if '=' in line:
                key, value = line.split('=', 1)
                key = key.strip()
                value = value.strip().strip('"').strip("'")
                result[key] = value
                # 根据 override 决定是否覆盖
                if override or key not in os.environ:
                    os.environ[key] = value
    
    return result


def get_config():
    """
    获取配置项
    
    优先级: 前后端目录 .env > 根目录 .env > 默认值
    
    Returns:
        dict: 配置字典
    """
    # 1. 先加载根目录配置（作为默认值/后备）
    load_env_file(".env", override=False)
    
    # 2. 加载后端配置（优先级更高，会覆盖根目录配置）
    backend_config = load_env_file("backend/.env", override=False)
    if 'HOST' in backend_config:
        os.environ['BACKEND_HOST'] = backend_config['HOST']
    if 'PORT' in backend_config:
        os.environ['BACKEND_PORT'] = backend_config['PORT']
    
    # 3. 加载前端配置（优先级更高，会覆盖根目录配置）
    frontend_config = load_env_file("frontend/.env", override=False)
    if 'VITE_PORT' in frontend_config:
        os.environ['FRONTEND_PORT'] = frontend_config['VITE_PORT']
    
    return {
        # 后端监听地址
        'backend_host': os.getenv('BACKEND_HOST', '0.0.0.0'),
        # 后端端口
        'backend_port': int(os.getenv('BACKEND_PORT', '8000')),
        # 前端端口
        'frontend_port': int(os.getenv('FRONTEND_PORT', '3000')),
        # 是否跳过依赖安装（加快启动速度）
        'skip_install': os.getenv('SKIP_INSTALL', 'False').lower() in ('true', '1', 'yes'),
        # 是否自动打开浏览器
        'auto_open_browser': os.getenv('AUTO_OPEN_BROWSER', 'True').lower() in ('true', '1', 'yes'),
    }


# ==================== 工具函数 ====================

def get_npm_command():
    """
    获取 npm 命令
    
    Windows 下使用 npm.cmd，其他系统使用 npm
    """
    return "npm.cmd" if os.name == 'nt' else "npm"


def check_environment():
    """
    检查运行环境
    
    - Python 版本 >= 3.11
    - Node.js 已安装
    """
    print("🔍 检查环境...")
    
    # 检查 Python 版本
    if sys.version_info < (3, 11):
        print("❌ 错误: 需要 Python 3.11 或更高版本")
        sys.exit(1)
    print(f"✓ Python {sys.version_info.major}.{sys.version_info.minor}")
        
    # 检查 Node.js
    npm_cmd = get_npm_command()
    try:
        result = subprocess.run(
            [npm_cmd, "--version"], 
            capture_output=True, 
            check=True, 
            shell=False,
            text=True
        )
        print(f"✓ Node.js/npm {result.stdout.strip()}")
    except (subprocess.CalledProcessError, FileNotFoundError):
        print(f"❌ 错误: 未找到 {npm_cmd}，请确保已安装 Node.js 并添加到环境变量")
        sys.exit(1)


def install_dependencies():
    """
    安装项目依赖
    
    - 后端: pip install -r requirements.txt
    - 前端: npm install
    """
    print("\n📦 安装依赖...")
    
    # 后端依赖
    print("  - 安装后端依赖...")
    subprocess.check_call([
        sys.executable, "-m", "pip", "install", "-r", "backend/requirements.txt", "-q"
    ])
    
    # 前端依赖
    print("  - 安装前端依赖...")
    npm_cmd = get_npm_command()
    subprocess.check_call(
        [npm_cmd, "install"], 
        cwd="frontend",
        shell=True
    )
    
    print("✓ 依赖安装完成")


# ==================== 服务启动 ====================

def start_backend(config: dict):
    """
    启动后端服务
    
    Args:
        config: 配置字典
    
    Returns:
        subprocess.Popen: 后端进程
    """
    host = config['backend_host']
    port = config['backend_port']
    
    print(f"🚀 启动后端服务 ({host}:{port})...")
    
    # 确保数据目录存在
    Path("backend/data/uploads").mkdir(parents=True, exist_ok=True)
    Path("backend/data/backups").mkdir(parents=True, exist_ok=True)
    Path("backend/data/temp").mkdir(parents=True, exist_ok=True)
    
    return subprocess.Popen(
        [
            sys.executable, "-m", "uvicorn", 
            "app.main:app", 
            "--host", host, 
            "--port", str(port), 
            "--reload"
        ],
        cwd="backend"
    )


def start_frontend(config: dict):
    """
    启动前端服务
    
    Args:
        config: 配置字典
    
    Returns:
        subprocess.Popen: 前端进程
    """
    port = config['frontend_port']
    backend_port = config['backend_port']
    
    print(f"🎨 启动前端服务 (Port {port})...")
    
    # 设置前端环境变量
    env = os.environ.copy()
    env['VITE_PORT'] = str(port)
    env['VITE_API_URL'] = f"http://localhost:{backend_port}"
    
    npm_cmd = get_npm_command()
    return subprocess.Popen(
        [npm_cmd, "run", "dev"],
        cwd="frontend",
        shell=False,
        env=env
    )


def open_browser(config: dict):
    """
    打开浏览器访问前端页面
    
    Args:
        config: 配置字典
    """
    time.sleep(3)  # 等待服务启动
    port = config['frontend_port']
    url = f"http://localhost:{port}"
    print(f"\n🌐 打开浏览器: {url}")
    webbrowser.open(url)


# ==================== 主函数 ====================

def main():
    """主函数"""
    print("=" * 60)
    print("MicroDock 插件管理系统 - 一键启动")
    print("=" * 60)
    
    # 获取配置（内部已处理配置加载，优先级: 前后端 .env > 根目录 .env > 默认值）
    config = get_config()
    
    # 打印配置信息
    print("\n⚙️  当前配置:")
    print(f"   后端地址: {config['backend_host']}:{config['backend_port']}")
    print(f"   前端端口: {config['frontend_port']}")
    print(f"   跳过安装: {config['skip_install']}")
    print(f"   自动打开浏览器: {config['auto_open_browser']}")
    
    try:
        # 检查环境
        check_environment()
        
        # 安装依赖（可通过配置跳过）
        if not config['skip_install']:
            install_dependencies()
        else:
            print("\n⏭️  跳过依赖安装")
        
        # 启动服务
        backend_process = start_backend(config)
        frontend_process = start_frontend(config)
        
        # 打开浏览器（可通过配置禁用）
        if config['auto_open_browser']:
            threading.Thread(target=open_browser, args=(config,), daemon=True).start()
        
        # 显示启动信息
        print("\n" + "=" * 60)
        print("✅ 服务已启动！")
        print(f"   后端 API:  http://localhost:{config['backend_port']}/docs")
        print(f"   前端界面: http://localhost:{config['frontend_port']}")
        print("=" * 60)
        print("\n按 Ctrl+C 停止所有服务...")
        
        # 等待进程结束
        backend_process.wait()
        frontend_process.wait()
        
    except KeyboardInterrupt:
        print("\n\n🛑 正在停止服务...")
        if 'backend_process' in locals():
            backend_process.terminate()
        if 'frontend_process' in locals():
            # Windows 下终止 shell 启动的子进程
            if os.name == 'nt':
                subprocess.run(
                    ["taskkill", "/F", "/T", "/PID", str(frontend_process.pid)],
                    capture_output=True
                )
            else:
                frontend_process.terminate()
        print("✓ 服务已停止")
        sys.exit(0)
    except Exception as e:
        print(f"\n❌ 发生错误: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()