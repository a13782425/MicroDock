# CLAUDE.md - MicroDock PluginServer 项目指南

本文档为 AI 助手提供项目上下文，帮助理解代码库结构和开发规范。

## 项目概述

**MicroDock PluginServer** 是一个插件管理服务器，用于管理和分发 MicroDock 应用的插件（ZIP 格式的存储器类型插件）。

采用现代化的**前后端分离架构**：
- **后端**: FastAPI + SQLAlchemy (异步)
- **前端**: Vue 3 + Vite + Tailwind CSS

## 技术栈

### 后端 (backend/)
- **框架**: FastAPI 0.104+
- **数据库**: SQLite + SQLAlchemy 2.0 (异步)
- **验证**: Pydantic 2.x
- **异步IO**: aiofiles, aiosqlite
- **服务器**: Uvicorn

### 前端 (frontend/)
- **框架**: Vue 3.3+ (Composition API)
- **构建工具**: Vite 5
- **样式**: Tailwind CSS 3
- **状态管理**: Pinia
- **HTTP 客户端**: Axios
- **路由**: Vue Router 4

### 部署
- Docker + Docker Compose
- Nginx 反向代理

## 目录结构

```
PluginServer/
├── backend/                  # 后端服务
│   ├── app/
│   │   ├── main.py          # FastAPI 应用入口
│   │   ├── config.py        # 配置管理 (Pydantic Settings)
│   │   ├── database.py      # 异步数据库连接
│   │   ├── api/             # API 路由层
│   │   │   ├── plugins.py   # 插件 CRUD API
│   │   │   ├── versions.py  # 版本管理 API
│   │   │   └── system.py    # 系统 API (健康检查)
│   │   ├── models/          # SQLAlchemy ORM 模型
│   │   │   ├── plugin.py    # Plugin 模型
│   │   │   └── version.py   # PluginVersion 模型
│   │   ├── schemas/         # Pydantic 数据模式
│   │   │   ├── plugin.py
│   │   │   ├── version.py
│   │   │   └── common.py
│   │   ├── services/        # 业务逻辑层
│   │   │   ├── plugin_service.py
│   │   │   ├── version_service.py
│   │   │   └── file_service.py
│   │   └── utils/           # 工具函数
│   │       ├── hash.py      # SHA256 哈希计算
│   │       └── validators.py # 文件验证
│   ├── requirements.txt
│   └── Dockerfile
├── frontend/                 # 前端应用
│   ├── src/
│   │   ├── App.vue          # 根组件
│   │   ├── main.js          # 入口文件
│   │   ├── views/
│   │   │   └── PluginList.vue
│   │   ├── components/
│   │   │   ├── UploadDialog.vue
│   │   │   └── VersionDialog.vue
│   │   ├── services/
│   │   │   ├── api.js       # Axios 实例
│   │   │   └── pluginService.js
│   │   ├── stores/
│   │   │   └── plugin.js    # Pinia Store
│   │   └── router/
│   │       └── index.js
│   ├── package.json
│   ├── vite.config.js
│   ├── tailwind.config.js
│   └── nginx.conf
├── Plugins/                  # 插件存储目录
├── data/                     # 数据目录 (数据库、上传文件)
├── start.py                  # 一键启动脚本
├── docker-compose.yml
├── Dockerfile.backend
└── Dockerfile.frontend
```

## 数据模型

### Plugin (插件)
| 字段 | 类型 | 说明 |
|------|------|------|
| id | Integer | 主键 |
| name | String | 唯一标识符 (反向域名格式) |
| display_name | String | 显示名称 |
| version_number | String | 当前版本号 |
| description | String | 描述 |
| author | String | 作者 |
| license | String | 许可证 |
| homepage | String | 主页 URL |
| main_dll | String | 主 DLL 文件名 |
| entry_class | String | 入口类完全限定名 |
| is_enabled | Boolean | 是否启用 |
| is_deprecated | Boolean | 是否过时 |
| current_version_id | Integer | 当前版本 FK |
| created_at | DateTime | 创建时间 |
| updated_at | DateTime | 更新时间 |

### PluginVersion (插件版本)
| 字段 | 类型 | 说明 |
|------|------|------|
| id | Integer | 主键 |
| plugin_id | Integer | 所属插件 FK |
| version | String | 版本号 |
| file_name | String | 文件名 |
| file_path | String | 存储路径 |
| file_size | Integer | 文件大小 (字节) |
| file_hash | String | SHA256 哈希 |
| changelog | Text | 更新日志 |
| dependencies | Text | 依赖 (JSON) |
| engines | Text | 引擎要求 (JSON) |
| is_deprecated | Boolean | 是否过时 |
| download_count | Integer | 下载次数 |
| created_at | DateTime | 创建时间 |

## API 设计规范

### HTTP 方法规则

**只使用 `GET` 和 `POST` 两种方法，禁止使用 PATCH、PUT、DELETE 等其他方法。**

| 方法 | 使用场景 |
|------|----------|
| `GET` | 无参数的查询操作 |
| `POST` | 有参数的所有操作（查询、创建、更新、删除） |

### URL 规则

1. **URL 路径中不携带动态参数**（禁止 `/api/plugins/{id}` 形式）
2. 参数通过 POST 请求体 (JSON Body) 传递
3. URL 采用 `/api/{资源}/{动作}` 格式

### 请求格式

| 类型 | Content-Type | 说明 |
|------|--------------|------|
| JSON 数据 | `application/json` | 普通参数传递 |
| 文件上传 | `multipart/form-data` | 包含文件的请求 |

### 响应格式

所有响应均为 JSON 格式：

```json
// 成功响应
{
    "success": true,
    "data": { ... },
    "message": "操作成功"
}

// 错误响应
{
    "success": false,
    "error": "错误信息",
    "message": "操作失败"
}
```

## API 端点

### 插件管理
| 方法 | 端点 | 说明 | 请求体 |
|------|------|------|--------|
| GET | `/api/plugins/list` | 获取插件列表 | 无 |
| POST | `/api/plugins/detail` | 获取插件详情 | `{"id": 1}` |
| POST | `/api/plugins/upload` | 上传新插件 | `multipart/form-data` |
| POST | `/api/plugins/enable` | 启用插件 | `{"id": 1}` |
| POST | `/api/plugins/disable` | 禁用插件 | `{"id": 1}` |
| POST | `/api/plugins/deprecate` | 标记过时 | `{"id": 1}` |
| POST | `/api/plugins/delete` | 删除插件 | `{"id": 1}` |
| POST | `/api/plugins/download` | 下载当前版本 | `{"id": 1}` |
| POST | `/api/plugins/versions` | 获取版本列表 | `{"id": 1}` |

### 版本管理
| 方法 | 端点 | 说明 | 请求体 |
|------|------|------|--------|
| POST | `/api/versions/detail` | 获取版本详情 | `{"id": 1}` |
| POST | `/api/versions/download` | 下载指定版本 | `{"id": 1}` |
| POST | `/api/versions/deprecate` | 标记版本过时 | `{"id": 1}` |

### 系统
| 方法 | 端点 | 说明 | 请求体 |
|------|------|------|--------|
| GET | `/api/health` | 健康检查 | 无 |

### 请求示例

```javascript
// 获取插件列表 (GET 无参数)
GET /api/plugins/list

// 获取插件详情 (POST 带参数)
POST /api/plugins/detail
Content-Type: application/json
{"id": 1}

// 上传插件 (POST 文件上传)
POST /api/plugins/upload
Content-Type: multipart/form-data
file: <plugin.zip>

// 启用插件 (POST 带参数)
POST /api/plugins/enable
Content-Type: application/json
{"id": 1}

// 删除插件 (POST 带参数)
POST /api/plugins/delete
Content-Type: application/json
{"id": 1}
```

## plugin.json 格式

ZIP 插件包必须在**根目录**包含 `plugin.json` 文件：

```json
{
    "name": "com.example.myplugin",      // 必需: 唯一标识符
    "version": "1.0.0",                   // 必需: 语义化版本号
    "main": "MyPlugin.dll",               // 必需: 主 DLL 文件名
    "entryClass": "MyPlugin.PluginEntry", // 必需: 入口类完全限定名
    "displayName": "我的插件",             // 可选: 显示名称
    "description": "插件描述",             // 可选: 描述
    "author": "作者名",                    // 可选: 作者
    "license": "MIT",                     // 可选: 许可证
    "homepage": "https://example.com",    // 可选: 主页
    "changelog": "更新内容",               // 可选: 更新日志
    "dependencies": {},                   // 可选: 依赖
    "engines": ""                         // 可选: 引擎要求
}
```

## 开发命令

### 后端开发
```bash
cd backend
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
# API 文档: http://localhost:8000/docs
```

### 前端开发
```bash
cd frontend
npm install
npm run dev
# 访问: http://localhost:3000
```

### 一键启动 (开发)
```bash
python start.py
```

### Docker 部署
```bash
docker-compose up -d
# 访问: http://localhost:3000
```

## 配置项

### 后端配置 (backend/app/config.py)
| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| APP_NAME | "MicroDock Plugin Server" | 应用名称 |
| APP_VERSION | "2.0.0" | 版本号 |
| DEBUG | True | 调试模式 |
| HOST | "0.0.0.0" | 监听地址 |
| PORT | 8000 | 监听端口 |
| DATABASE_URL | "sqlite+aiosqlite:///./data/plugins.db" | 数据库连接 |
| UPLOAD_DIR | "./data/uploads" | 上传目录 |
| MAX_UPLOAD_SIZE | 100MB | 最大上传大小 |
| ALLOWED_EXTENSIONS | {".zip"} | 允许的扩展名 |
| CORS_ORIGINS | ["http://localhost:3000", ...] | CORS 白名单 |

### 前端代理配置 (frontend/vite.config.js)
```javascript
server: {
    port: 3000,
    proxy: {
        '/api': {
            target: 'http://localhost:8000',
            changeOrigin: true
        }
    }
}
```

## 代码规范

### 后端
- 使用 async/await 异步编程
- Service 层处理业务逻辑，API 层只做路由
- 使用 Pydantic 进行数据验证
- 异常使用 HTTPException 抛出
- 文件操作使用 aiofiles
- **API 只使用 GET 和 POST 方法**
- **URL 中不携带动态参数**

### 前端
- 使用 Vue 3 Composition API (`<script setup>`)
- 组件使用 PascalCase 命名
- 状态管理通过 Pinia Store
- API 调用封装在 services 目录
- 样式使用 Tailwind CSS 工具类

## 注意事项

1. **文件存储**: 上传的插件文件存储在 `data/uploads/{plugin_id}/` 目录
2. **版本唯一性**: 同一插件的版本号不能重复 (数据库唯一约束)
3. **删除级联**: 删除插件会同时删除所有版本和文件
4. **哈希校验**: 使用 SHA256 计算文件哈希确保完整性

## 常见问题

### Q: 如何添加新的 API 端点？
1. 在 `backend/app/api/` 创建或修改路由文件
2. 在 `backend/app/services/` 添加业务逻辑
3. 在 `backend/app/schemas/` 定义请求/响应模式
4. 在 `backend/app/main.py` 注册路由 (如果是新文件)
5. **遵循 API 设计规范：只用 GET/POST，URL 不带参数**

### Q: 如何添加新的前端页面？
1. 在 `frontend/src/views/` 创建 Vue 组件
2. 在 `frontend/src/router/index.js` 添加路由
3. 如需全局状态，在 `frontend/src/stores/` 创建 Store

### Q: 如何修改数据库模型？
1. 修改 `backend/app/models/` 中的模型定义
2. 删除 `data/plugins.db` (开发环境)
3. 重启后端服务自动创建新表

## 相关项目

- **MicroDock 主程序**: `../MicroDock/`
- **插件接口定义**: `../MicroDock.Plugin/IPluginContext.cs`
- **插件服务**: `../MicroDock/Service/PluginService.cs`
