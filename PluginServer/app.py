#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
MicroDock 插件管理服务器
提供Web界面来管理MicroDock插件（zip格式存储器类型）
"""

import os
import json
import shutil
import zipfile
import tempfile
import threading
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Optional
from dataclasses import dataclass, asdict
from flask import Flask, render_template, request, jsonify, send_file, redirect, url_for

app = Flask(__name__)

# 配置
PLUGINS_DIR = Path("./Plugins")
PLUGIN_SERVER_DIR = Path(__file__).parent
BACKUP_DIR = PLUGIN_SERVER_DIR / "backups"
TEMP_DIR = PLUGIN_SERVER_DIR / "temp"

# 确保目录存在
PLUGINS_DIR.mkdir(exist_ok=True)
BACKUP_DIR.mkdir(exist_ok=True)
TEMP_DIR.mkdir(exist_ok=True)

@dataclass
class PluginInfo:
    """插件信息数据类 - 存储器类型"""
    name: str
    version: str = "1.0.0"
    description: str = ""
    author: str = ""
    file_path: str = ""
    file_size: int = 0
    created_time: str = ""
    modified_time: str = ""
    plugin_type: str = "storage"  # 存储器类型
    dependencies: List[str] = None
    zip_contents: List[str] = None  # zip包内容列表

    def __post_init__(self):
        if self.dependencies is None:
            self.dependencies = []
        if self.zip_contents is None:
            self.zip_contents = []

class PluginManager:
    """插件管理器 - 专门处理zip存储器插件"""

    def __init__(self):
        self.plugins: Dict[str, PluginInfo] = {}
        self.scan_plugins()

    def scan_plugins(self) -> None:
        """扫描插件目录（查找zip和dll文件）"""
        self.plugins.clear()

        print(f"正在扫描插件目录: {PLUGINS_DIR.absolute()}")

        if not PLUGINS_DIR.exists():
            print(f"插件目录不存在: {PLUGINS_DIR}")
            return

        # 扫描zip文件
        zip_files = list(PLUGINS_DIR.glob("*.zip"))
        # 扫描dll文件（向后兼容）
        dll_files = list(PLUGINS_DIR.glob("*.dll"))

        all_files = zip_files + dll_files
        print(f"找到 {len(zip_files)} 个.zip文件, {len(dll_files)} 个.dll文件")

        for plugin_file in all_files:
            try:
                print(f"正在处理插件文件: {plugin_file}")
                plugin_info = self._extract_plugin_info(plugin_file)
                self.plugins[plugin_info.name] = plugin_info
                print(f"成功加载插件: {plugin_info.name}")
            except Exception as e:
                print(f"扫描插件失败 {plugin_file}: {e}")
                import traceback
                traceback.print_exc()

    def _extract_plugin_info(self, plugin_path: Path) -> PluginInfo:
        """提取插件信息"""
        stat = plugin_path.stat()

        # 默认配置
        plugin_config = {
            'name': plugin_path.stem,
            'version': '1.0.0',
            'description': '插件存储器',
            'author': '未知',
            'type': 'storage',
            'dependencies': []
        }

        # 如果是zip文件，尝试从zip包根目录读取plugin.json
        if plugin_path.suffix.lower() == '.zip':
            try:
                with zipfile.ZipFile(plugin_path, 'r') as zip_file:
                    # 查找zip根目录的plugin.json
                    for file_info in zip_file.filelist:
                        if file_info.filename.endswith('plugin.json') and '/' not in file_info.filename.replace('\\', '/'):
                            # 这是根目录的plugin.json
                            with zip_file.open(file_info) as json_file:
                                plugin_config.update(json.loads(json_file.read().decode('utf-8')))
                            break

                    # 获取zip包内容列表
                    plugin_config['zip_contents'] = [f.filename for f in zip_file.filelist]

            except Exception as e:
                print(f"读取zip插件配置失败: {e}")

        # 如果是dll文件，尝试读取旁边的plugin.json
        elif plugin_path.suffix.lower() == '.dll':
            config_file = plugin_path.parent / plugin_path.stem / "plugin.json"
            if config_file.exists():
                try:
                    with open(config_file, 'r', encoding='utf-8') as f:
                        plugin_config.update(json.load(f))
                except:
                    pass

        try:
            file_path = str(plugin_path.relative_to(PLUGIN_SERVER_DIR))
        except ValueError:
            file_path = str(plugin_path)

        return PluginInfo(
            name=plugin_config.get('name', plugin_path.stem),
            version=plugin_config.get('version', '1.0.0'),
            description=plugin_config.get('description', ''),
            author=plugin_config.get('author', ''),
            file_path=file_path,
            file_size=stat.st_size,
            created_time=datetime.fromtimestamp(stat.st_ctime).strftime('%Y-%m-%d %H:%M:%S'),
            modified_time=datetime.fromtimestamp(stat.st_mtime).strftime('%Y-%m-%d %H:%M:%S'),
            plugin_type=plugin_config.get('type', 'storage'),
            dependencies=plugin_config.get('dependencies', []),
            zip_contents=plugin_config.get('zip_contents', [])
        )

    def add_plugin(self, plugin_file_path: str) -> bool:
        """添加插件（支持zip和dll）"""
        try:
            source = Path(plugin_file_path)
            if not source.exists():
                return False

            # 支持zip和dll文件
            if source.suffix.lower() not in ['.zip', '.dll']:
                return False

            dest = PLUGINS_DIR / source.name

            # 如果目标文件已存在，先备份
            if dest.exists():
                backup_name = f"{dest.stem}_{datetime.now().strftime('%Y%m%d_%H%M%S')}{dest.suffix}"
                backup_path = BACKUP_DIR / backup_name
                shutil.copy2(dest, backup_path)

            shutil.copy2(source, dest)

            # 重新扫描插件
            self.scan_plugins()
            return True
        except Exception as e:
            print(f"添加插件失败: {e}")
            return False

    def remove_plugin(self, plugin_name: str) -> bool:
        """删除插件（自动备份）"""
        try:
            if plugin_name in self.plugins:
                plugin_path = PLUGIN_SERVER_DIR / self.plugins[plugin_name].file_path
                if plugin_path.exists():
                    # 创建备份
                    backup_name = f"{plugin_name}_{datetime.now().strftime('%Y%m%d_%H%M%S')}{plugin_path.suffix}"
                    backup_path = BACKUP_DIR / backup_name
                    shutil.copy2(plugin_path, backup_path)

                    # 删除原文件
                    plugin_path.unlink()

                    # 重新扫描
                    self.scan_plugins()
                    return True
        except Exception as e:
            print(f"删除插件失败: {e}")
        return False

    def get_plugin_version(self, plugin_name: str) -> Optional[str]:
        """获取插件版本"""
        if plugin_name in self.plugins:
            return self.plugins[plugin_name].version
        return None

    def download_plugin(self, plugin_name: str) -> Optional[Path]:
        """获取插件下载路径"""
        if plugin_name in self.plugins:
            plugin_path = PLUGIN_SERVER_DIR / self.plugins[plugin_name].file_path
            if plugin_path.exists():
                return plugin_path
        return None

# 创建插件管理器实例
plugin_manager = PluginManager()

@app.route('/')
def index():
    """主页 - 插件列表"""
    return render_template('index.html', plugins=plugin_manager.plugins)

@app.route('/api/plugins')
def api_plugins():
    """API: 获取插件列表"""
    return jsonify({
        'success': True,
        'data': [asdict(plugin) for plugin in plugin_manager.plugins.values()]
    })

@app.route('/api/plugins/<plugin_name>/version')
def api_plugin_version(plugin_name):
    """API: 获取插件版本"""
    version = plugin_manager.get_plugin_version(plugin_name)
    if version:
        return jsonify({
            'success': True,
            'version': version
        })
    else:
        return jsonify({
            'success': False,
            'message': '插件不存在'
        }), 404

@app.route('/api/plugins/<plugin_name>/download')
def api_download_plugin(plugin_name):
    """API: 下载插件"""
    plugin_path = plugin_manager.download_plugin(plugin_name)
    if plugin_path:
        return send_file(str(plugin_path), as_attachment=True)
    return "插件不存在", 404

@app.route('/api/plugins/<plugin_name>/delete', methods=['DELETE'])
def api_delete_plugin(plugin_name):
    """API: 删除插件"""
    success = plugin_manager.remove_plugin(plugin_name)
    return jsonify({
        'success': success,
        'message': f'插件已{"删除" if success else "删除失败"}'
    })

@app.route('/api/upload', methods=['POST'])
def api_upload_plugin():
    """API: 上传插件（支持zip和dll）"""
    if 'plugin_file' not in request.files:
        return jsonify({'success': False, 'message': '没有文件上传'})

    file = request.files['plugin_file']
    if file.filename == '':
        return jsonify({'success': False, 'message': '文件名为空'})

    # 支持zip和dll文件
    if file and file.filename.lower().endswith(('.zip', '.dll')):
        # 保存到临时目录
        temp_path = TEMP_DIR / file.filename
        file.save(str(temp_path))

        # 如果是zip文件，验证是否包含plugin.json
        if temp_path.suffix.lower() == '.zip':
            try:
                with zipfile.ZipFile(temp_path, 'r') as zip_file:
                    has_plugin_json = any(
                        f.filename.endswith('plugin.json') and '/' not in f.filename.replace('\\', '/')
                        for f in zip_file.filelist
                    )
                    if not has_plugin_json:
                        temp_path.unlink(missing_ok=True)
                        return jsonify({'success': False, 'message': 'zip文件必须包含根目录的plugin.json'})
            except Exception as e:
                temp_path.unlink(missing_ok=True)
                return jsonify({'success': False, 'message': f'zip文件格式错误: {str(e)}'})

        # 添加到插件目录
        success = plugin_manager.add_plugin(str(temp_path))

        # 清理临时文件
        temp_path.unlink(missing_ok=True)

        if success:
            return jsonify({'success': True, 'message': '插件上传成功'})
        else:
            return jsonify({'success': False, 'message': '插件上传失败'})

    return jsonify({'success': False, 'message': '只支持.zip和.dll插件文件'})

@app.route('/api/scan', methods=['POST'])
def api_scan_plugins():
    """API: 重新扫描插件"""
    plugin_manager.scan_plugins()
    return jsonify({
        'success': True,
        'message': '插件扫描完成',
        'data': [asdict(plugin) for plugin in plugin_manager.plugins.values()]
    })

@app.route('/api/backups')
def api_backups():
    """API: 获取备份列表"""
    backups = []
    for backup_file in BACKUP_DIR.glob("*"):
        if backup_file.is_file():
            stat = backup_file.stat()
            backups.append({
                'name': backup_file.name,
                'size': stat.st_size,
                'created_time': datetime.fromtimestamp(stat.st_ctime).strftime('%Y-%m-%d %H:%M:%S')
            })

    return jsonify({
        'success': True,
        'data': sorted(backups, key=lambda x: x['created_time'], reverse=True)
    })

@app.route('/download/backup/<backup_name>')
def download_backup(backup_name):
    """下载备份文件"""
    backup_path = BACKUP_DIR / backup_name
    if backup_path.exists():
        return send_file(str(backup_path), as_attachment=True)
    return "备份文件不存在", 404

@app.route('/api/plugins/<plugin_name>/contents')
def api_plugin_contents(plugin_name):
    """API: 获取插件内容列表（仅限zip文件）"""
    if plugin_name in plugin_manager.plugins:
        plugin = plugin_manager.plugins[plugin_name]
        if plugin.plugin_type == 'storage' and plugin.zip_contents:
            return jsonify({
                'success': True,
                'data': plugin.zip_contents
            })
    return jsonify({'success': False, 'message': '插件内容不可用'}), 404

if __name__ == '__main__':
    print("MicroDock 插件管理服务器启动中...")
    print(f"插件目录: {PLUGINS_DIR.absolute()}")
    print(f"访问地址: http://localhost:5000")

    # 在后台线程中启动Flask应用
    app.run(host='0.0.0.0', port=5000, debug=True)