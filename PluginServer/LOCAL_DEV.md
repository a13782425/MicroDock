# 本地开发启动指南

## 🚀 快速启动

### 1. 启动后端服务 (FastAPI)

```bash
cd backend
python start_local.py
```

**服务地址**: http://localhost:8001
**API文档**: http://localhost:8001/docs
**健康检查**: http://localhost:8001/health

### 2. 启动前端服务 (Vue3 + Vite)

```bash
cd frontend
npm install  # 首次运行需要安装依赖
npm run dev
```

**服务地址**: http://localhost:3001 (如果端口被占用，会自动选择其他端口)

## 📋 访问说明

1. **主界面**: 打开浏览器访问 http://localhost:3001
2. **API接口**: 后端服务运行在 http://localhost:8001
3. **环境配置**: 前端通过 `.env` 文件配置API地址

## 🔧 配置信息

- **后端端口**: 8001 (避免与占用8000端口的程序冲突)
- **前端端口**: 3001 (如果3000被占用会自动切换)
- **API基础URL**: http://localhost:8001
- **数据库**: SQLite (backend/data/plugins.db)

## 📦 技术栈

- **后端**: FastAPI + SQLAlchemy + SQLite
- **前端**: Vue3 + Vite + TailwindCSS + Pinia
- **开发工具**: Python 3.8+, Node.js 16+

## 🛠️ 开发说明

### 后端API结构
```
/api/plugins          # 插件管理
/api/versions         # 版本管理
/api/backups          # 备份管理
/api/settings         # 系统设置
```

### 前端页面结构
```
/                     # 仪表板
/plugins              # 插件列表
/versions             # 版本管理
/backups              # 备份管理
/settings             # 系统设置
```

## 🔍 故障排除

### 常见问题

1. **端口占用**: 如果8001或3001端口被占用，可以修改启动脚本中的端口号
2. **依赖缺失**: 确保安装了Python和Node.js，并运行了 `npm install`
3. **CORS错误**: 后端已配置允许前端跨域访问，正常情况下不会出现此问题

### 停止服务

在运行服务的终端窗口中按 `Ctrl+C` 停止服务。

---

*最后更新: 2025-11-18*