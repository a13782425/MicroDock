#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
MicroDock 插件管理服务器启动脚本
"""

import os
import sys
import subprocess
import threading
import time
import webbrowser
from pathlib import Path

def check_python_version():
    """检查Python版本"""
    if sys.version_info < (3, 7):
        print("错误: 需要Python 3.7或更高版本")
        sys.exit(1)
    print(f"✓ Python版本: {sys.version}")

def install_dependencies():
    """安装依赖包"""
    requirements_file = Path(__file__).parent / "requirements.txt"

    if not requirements_file.exists():
        print("警告: requirements.txt 文件不存在")
        return False

    print("正在检查和安装依赖包...")
    try:
        subprocess.check_call([
            sys.executable, "-m", "pip", "install", "-r", str(requirements_file)
        ])
        print("✓ 依赖包安装完成")
        return True
    except subprocess.CalledProcessError as e:
        print(f"错误: 依赖包安装失败 - {e}")
        return False

def check_flask():
    """检查Flask是否已安装"""
    try:
        import flask
        print(f"✓ Flask已安装: {flask.__version__}")
        return True
    except ImportError:
        return False

def create_directories():
    """创建必要的目录"""
    directories = ["Plugins", "backups", "temp"]
    for dir_name in directories:
        dir_path = Path(dir_name)
        dir_path.mkdir(exist_ok=True)
        print(f"✓ 目录已创建: {dir_path}")

def open_browser():
    """延迟打开浏览器"""
    time.sleep(2)  # 等待服务器启动
    try:
        webbrowser.open("http://localhost:5000")
        print("✓ 浏览器已打开")
    except Exception as e:
        print(f"警告: 无法自动打开浏览器 - {e}")

def main():
    """主函数"""
    print("=" * 60)
    print("MicroDock 插件管理服务器启动器")
    print("=" * 60)

    # 检查Python版本
    check_python_version()

    # 创建必要目录
    create_directories()

    # 检查Flask
    if not check_flask():
        print("正在安装Flask...")
        if not install_dependencies():
            print("Flask安装失败，请手动运行: pip install flask")
            sys.exit(1)

    print("\n正在启动插件管理服务器...")
    print("访问地址: http://localhost:5000")
    print("按 Ctrl+C 停止服务器")
    print("-" * 60)

    # 在后台线程中打开浏览器
    browser_thread = threading.Thread(target=open_browser, daemon=True)
    browser_thread.start()

    try:
        # 导入并运行Flask应用
        from app import app
        app.run(host='0.0.0.0', port=5000, debug=False)
    except KeyboardInterrupt:
        print("\n服务器已停止")
    except Exception as e:
        print(f"启动失败: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()