# MicroDock PluginServer API 完整文档

## 目录

- [1. 概述](#1-概述)
- [2. 快速开始](#2-快速开始)
- [3. 认证与安全](#3-认证与安全)
- [4. API 参考](#4-api-参考)
  - [4.1 认证 API](#41-认证-api)
  - [4.2 插件管理 API](#42-插件管理-api)
  - [4.3 版本管理 API](#43-版本管理-api)
  - [4.4 备份管理 API](#44-备份管理-api)
  - [4.5 系统 API](#45-系统-api)
- [5. 数据模型](#5-数据模型)
- [6. 开发指南](#6-开发指南)
- [7. 部署与配置](#7-部署与配置)
- [8. 故障排除](#8-故障排除)
- [9. 更新日志](#9-更新日志)

---

## 1. 概述

### 1.1 项目介绍

MicroDock PluginServer 是一个功能完整的插件管理服务器，采用现代化的前后端分离架构。该服务器提供插件的完整生命周期管理，包括上传、版本控制、启用/禁用、下载等核心功能，同时支持用户数据备份和恢复。

### 1.2 技术架构

**后端技术栈：**
- **框架**: FastAPI 0.104+
- **数据库**: SQLite + SQLAlchemy 2.0 (异步)
- **验证**: Pydantic 2.x
- **异步IO**: aiofiles, aiosqlite
- **认证**: JWT (JSON Web Token)
- **文件存储**: 本地文件系统

**前端技术栈：**
- **框架**: Vue 3.3+ (Composition API)
- **构建工具**: Vite 5
- **样式**: Tailwind CSS 3
- **状态管理**: Pinia
- **HTTP 客户端**: Axios
- **路由**: Vue Router 4

### 1.3 API 设计原则

MicroDock PluginServer 遵循以下 API 设计原则：

1. **HTTP 方法限制**: 仅使用 `GET` 和 `POST` 两种 HTTP 方法
2. **URL 设计**: URL 路径中不携带动态参数，所有参数通过请求体传递
3. **统一响应格式**: 所有 API 响应都采用 ApiResponse 包装格式
4. **版本控制**: 支持插件的多版本管理，采用语义化版本号
5. **安全优先**: 基于 JWT 的管理员权限控制，文件上传安全验证

### 1.4 统一响应格式

所有 API 端点（除文件下载外）都使用统一的 `ApiResponse` 格式：

```json
{
  "success": boolean,
  "message": "操作结果描述",
  "data": <响应数据>
}
```

**字段说明**:
- `success`: 操作是否成功（`true`/`false`）
- `message`: 操作结果的详细描述信息
- `data`: 响应数据（成功时包含具体数据，失败时为 `null`）

**成功响应示例**:
```json
{
  "success": true,
  "message": "操作成功",
  "data": {
    "name": "com.example.myplugin",
    "display_name": "我的插件"
  }
}
```

**错误响应示例**:
```json
{
  "success": false,
  "message": "插件不存在",
  "data": null
}
```

**注意**: 文件下载接口（如插件下载、备份下载）直接返回文件流，不使用 ApiResponse 格式。

### 1.5 核心功能

- **插件管理**: 插件的上传、启用、禁用、删除、下载
- **版本控制**: 插件的多版本管理和版本回溯
- **备份恢复**: 用户数据备份和恢复功能
- **权限控制**: 基于角色的访问控制
- **文件安全**: 文件大小限制、类型验证、哈希校验
- **监控健康**: 系统健康检查和状态监控

---

## 2. 快速开始

### 2.1 环境要求

- Python 3.8+
- Node.js 16+
- SQLite 3

### 2.2 快速启动

```bash
# 克隆项目
git clone <repository-url>
cd PluginServer

# 后端启动
cd backend
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000

# 前端启动 (新终端)
cd frontend
npm install
npm run dev

# 或使用一键启动脚本
python start.py
```

### 2.3 访问地址

- **前端界面**: http://localhost:3000
- **API 文档**: http://localhost:8000/docs (Swagger UI)
- **API 规范**: http://localhost:8000/openapi.json
- **健康检查**: http://localhost:8000/api/health

### 2.4 默认管理员账号

```json
{
  "username": "admin",
  "password": "admin"
}
```

> ⚠️ **安全警告**: 生产环境中请务必修改默认密码！

### 2.5 第一个 API 调用

```bash
# 获取管理员令牌
curl -X POST http://localhost:8000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "admin"}'

# 获取插件列表
curl -X GET http://localhost:8000/api/plugins/list
```

---

## 3. 认证与安全

### 3.1 JWT 认证机制

MicroDock PluginServer 使用 JWT (JSON Web Token) 进行身份认证。认证流程如下：

1. **获取令牌**: 管理员使用用户名和密码登录，获取 JWT 令牌
2. **使用令牌**: 在需要管理员权限的 API 请求中携带令牌
3. **令牌验证**: 服务器验证令牌的有效性和权限
4. **令牌过期**: 令牌默认有效期为 24 小时

### 3.2 管理员登录

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin"
}
```

**响应示例：**
```json
{
  "success": true,
  "message": "登录成功",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "bearer",
  "expires_in": 86400
}
```

### 3.3 使用令牌

在需要管理员权限的 API 请求中，需要在请求头中携带 JWT 令牌：

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 3.4 权限分级

#### 公开端点 (无需认证)
- `GET /api/plugins/list` - 获取插件列表
- `POST /api/plugins/detail` - 获取插件详情
- `POST /api/plugins/upload` - 上传插件 (使用 plugin_key)
- `POST /api/plugins/download` - 下载插件
- `POST /api/plugins/versions` - 获取版本列表
- `POST /api/plugins/version/detail` - 获取版本详情
- `POST /api/plugins/version/download` - 下载指定版本
- `POST /api/backups/upload` - 上传备份
- `POST /api/backups/list` - 获取用户备份列表
- `POST /api/backups/download` - 下载备份
- `GET /api/health` - 健康检查
- `GET /` - 根路径信息

#### 管理员端点 (需要认证)
- `POST /api/auth/logout` - 管理员登出
- `POST /api/plugins/enable` - 启用插件
- `POST /api/plugins/disable` - 禁用插件
- `POST /api/plugins/deprecate` - 标记插件过时
- `POST /api/plugins/delete` - 删除插件
- `POST /api/plugins/version/deprecate` - 标记版本过时
- `GET /api/backups/list-all` - 获取所有备份列表
- `POST /api/backups/delete` - 删除备份

#### 可选认证端点
- `GET /api/auth/me` - 获取认证状态 (可选认证)

### 3.5 安全配置

#### 环境变量配置

```python
# 管理员认证
ADMIN_USERNAME: str = "admin"             # 管理员用户名
ADMIN_PASSWORD: str = "admin"             # 管理员密码
JWT_SECRET_KEY: str = "默认密钥"           # JWT签名密钥
JWT_EXPIRE_MINUTES: int = 1440            # Token过期时间（分钟）

# 文件上传安全
MAX_UPLOAD_SIZE: int = 100 * 1024 * 1024  # 最大上传大小 (100MB)
ALLOWED_EXTENSIONS: Set[str] = {".zip"}   # 允许的文件扩展名

# CORS 配置
CORS_ORIGINS: List[str] = [
    "http://localhost:3000",
    "http://localhost:3001",
    "http://127.0.0.1:3000"
]
```

### 3.6 安全最佳实践

1. **修改默认密码**: 生产环境中务必修改默认管理员密码
2. **强密钥**: 使用强随机字符串作为 JWT 密钥
3. **HTTPS**: 生产环境使用 HTTPS 传输
4. **文件验证**: 严格验证上传文件的类型和大小
5. **定期更新**: 定期更新依赖包，修复安全漏洞

---

## 4. API 参考

### 4.1 认证 API

#### 4.1.1 管理员登录

获取管理员访问令牌。

**端点**: `POST /api/auth/login`
**权限**: 无需认证
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| username | string | 是 | 管理员用户名 |
| password | string | 是 | 管理员密码 |

**请求示例**:
```json
{
  "username": "admin",
  "password": "admin"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "token_type": "bearer",
    "expires_in": 86400
  }
}
```

**错误响应**:
- `401 Unauthorized`: 用户名或密码错误
- `422 Unprocessable Entity`: 请求参数验证失败

#### 4.1.2 获取认证状态

获取当前用户的认证信息和权限状态。

**端点**: `GET /api/auth/me`
**权限**: 可选认证
**参数**: 无

**响应示例** (管理员已登录):
```json
{
  "success": true,
  "message": "认证状态获取成功",
  "data": {
    "is_logged_in": true,
    "username": "admin",
    "is_admin": true
  }
}
```

**响应示例** (未登录):
```json
{
  "success": true,
  "message": "认证状态获取成功",
  "data": {
    "is_logged_in": false,
    "username": null,
    "is_admin": false
  }
}
```

#### 4.1.3 管理员登出

登出当前会话，使 JWT 令牌失效（客户端删除令牌）。

**端点**: `POST /api/auth/logout`
**权限**: 需要管理员认证
**参数**: 无

**响应示例**:
```json
{
  "success": true,
  "message": "登出成功",
  "data": null
}
```

### 4.2 插件管理 API

#### 4.2.1 获取插件列表

获取系统中所有插件的基本信息列表。

**端点**: `GET /api/plugins/list`
**权限**: 无需认证
**参数**: 无

**响应示例**:
```json
{
  "success": true,
  "message": "插件列表获取成功",
  "data": [
    {
      "name": "com.example.myplugin",
      "display_name": "我的插件",
      "description": "这是一个示例插件",
      "author": "开发者姓名",
      "license": "MIT",
      "homepage": "https://example.com/myplugin",
      "main_dll": "MyPlugin.dll",
      "entry_class": "MyPlugin.PluginEntry",
      "current_version": "1.0.0",
      "is_enabled": true,
      "is_deprecated": false,
      "created_at": "2023-12-01T14:30:22Z",
      "updated_at": "2023-12-01T14:30:22Z"
    }
  ]
}
```

#### 4.2.2 获取插件详情

根据插件名称获取插件的详细信息，包括插件基本信息和所有版本列表。

**端点**: `POST /api/plugins/detail`
**权限**: 无需认证
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| name | string | 是 | 插件唯一标识符 (反向域名格式) |

**请求示例**:
```json
{
  "name": "com.example.myplugin"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "插件详情获取成功",
  "data": {
    "plugin": {
      "name": "com.example.myplugin",
      "display_name": "我的插件",
      "description": "这是一个示例插件",
      "author": "开发者姓名",
      "license": "MIT",
      "homepage": "https://example.com/myplugin",
      "main_dll": "MyPlugin.dll",
      "entry_class": "MyPlugin.PluginEntry",
      "current_version": "1.0.0",
      "is_enabled": true,
      "is_deprecated": false,
      "created_at": "2023-12-01T14:30:22Z",
      "updated_at": "2023-12-01T14:30:22Z"
    },
    "versions": [
      {
        "plugin_name": "com.example.myplugin",
        "version": "1.0.0",
        "file_name": "myplugin@1.0.0.zip",
        "file_path": "./data/uploads/com.example.myplugin/myplugin@1.0.0.zip",
        "file_size": 1048576,
        "file_hash": "a1b2c3d4e5f6...",
        "changelog": "修复了一些bug，添加了新功能",
        "dependencies": {"com.other.plugin": ">=1.0.0"},
        "engines": {"MicroDock": ">=2.0.0"},
        "is_deprecated": false,
        "download_count": 42,
        "created_at": "2023-12-01T14:30:22Z"
      }
    ]
  }
}
```

#### 4.2.3 上传新插件

上传新的插件 ZIP 文件到服务器。

**端点**: `POST /api/plugins/upload`
**权限**: 无需认证 (使用 plugin_key 验证)
**Content-Type**: `multipart/form-data`
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| file | file | 是 | 插件 ZIP 文件 (最大 100MB) |
| plugin_key | string | 是 | 插件上传密钥，用于验证上传权限 |

**插件 ZIP 包要求**:
1. 必须在根目录包含 `plugin.json` 配置文件
2. 必须包含主 DLL 文件
3. 文件大小不超过 100MB
4. 仅支持 `.zip` 格式

**plugin.json 格式**:
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

**响应示例**:
```json
{
  "success": true,
  "message": "插件上传成功",
  "data": {
    "name": "com.example.myplugin",
    "display_name": "我的插件",
    "description": "这是一个示例插件",
    "author": "开发者姓名",
    "license": "MIT",
    "homepage": "https://example.com/myplugin",
    "main_dll": "MyPlugin.dll",
    "entry_class": "MyPlugin.PluginEntry",
    "current_version": "1.0.0",
    "is_enabled": true,
    "is_deprecated": false,
    "created_at": "2023-12-01T14:30:22Z",
    "updated_at": "2023-12-01T14:30:22Z"
  }
}
```

**错误响应**:
- `400 Bad Request`: 文件格式不支持、plugin.json 缺失或格式错误
- `409 Conflict`: 插件版本已存在
- `413 Request Entity Too Large`: 文件大小超过限制
- `422 Unprocessable Entity`: 请求参数验证失败

#### 4.2.4 启用插件

启用指定的插件，使其在系统中可用。

**端点**: `POST /api/plugins/enable`
**权限**: 需要管理员认证
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| name | string | 是 | 插件唯一标识符 |

**请求示例**:
```json
{
  "name": "com.example.myplugin"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "插件启用成功",
  "data": {
    "name": "com.example.myplugin",
    "display_name": "我的插件",
    "description": "这是一个示例插件",
    "author": "开发者姓名",
    "license": "MIT",
    "homepage": "https://example.com/myplugin",
    "main_dll": "MyPlugin.dll",
    "entry_class": "MyPlugin.PluginEntry",
    "current_version": "1.0.0",
    "is_enabled": true,
    "is_deprecated": false,
    "created_at": "2023-12-01T14:30:22Z",
    "updated_at": "2023-12-01T14:35:22Z"
  }
}
```

#### 4.2.5 禁用插件

禁用指定的插件，使其在系统中不可用。

**端点**: `POST /api/plugins/disable`
**权限**: 需要管理员认证
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| name | string | 是 | 插件唯一标识符 |

**请求示例**:
```json
{
  "name": "com.example.myplugin"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "插件禁用成功",
  "data": {
    "name": "com.example.myplugin",
    "display_name": "我的插件",
    "description": "这是一个示例插件",
    "author": "开发者姓名",
    "license": "MIT",
    "homepage": "https://example.com/myplugin",
    "main_dll": "MyPlugin.dll",
    "entry_class": "MyPlugin.PluginEntry",
    "current_version": "1.0.0",
    "is_enabled": false,
    "is_deprecated": false,
    "created_at": "2023-12-01T14:30:22Z",
    "updated_at": "2023-12-01T14:35:22Z"
  }
}
```

#### 4.2.6 标记插件过时

将指定插件标记为过时状态。过时的插件仍然可以下载，但建议用户使用更新的版本。

**端点**: `POST /api/plugins/deprecate`
**权限**: 需要管理员认证
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| name | string | 是 | 插件唯一标识符 |

**请求示例**:
```json
{
  "name": "com.example.myplugin"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "插件标记过时成功",
  "data": {
    "name": "com.example.myplugin",
    "display_name": "我的插件",
    "description": "这是一个示例插件",
    "author": "开发者姓名",
    "license": "MIT",
    "homepage": "https://example.com/myplugin",
    "main_dll": "MyPlugin.dll",
    "entry_class": "MyPlugin.PluginEntry",
    "current_version": "1.0.0",
    "is_enabled": true,
    "is_deprecated": true,
    "created_at": "2023-12-01T14:30:22Z",
    "updated_at": "2023-12-01T14:35:22Z"
  }
}
```

#### 4.2.7 删除插件

完全删除指定的插件及其所有版本文件。此操作不可逆，请谨慎使用。

**端点**: `POST /api/plugins/delete`
**权限**: 需要管理员认证
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| name | string | 是 | 插件唯一标识符 |

**请求示例**:
```json
{
  "name": "com.example.myplugin"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "插件删除成功"
}
```

> ⚠️ **警告**: 删除插件将同时删除：
> - 插件的所有版本记录
> - 所有相关的 ZIP 文件
> - 下载统计数据

#### 4.2.8 下载插件当前版本

下载指定插件的当前版本 ZIP 文件。下载后会自动统计下载次数。

**端点**: `POST /api/plugins/download`
**权限**: 无需认证
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| name | string | 是 | 插件唯一标识符 |

**请求示例**:
```json
{
  "name": "com.example.myplugin"
}
```

**响应**: 文件流 (application/zip)
**Headers**:
```
Content-Disposition: attachment; filename="myplugin@1.0.0.zip"
Content-Type: application/zip
Content-Length: 1048576
```

### 4.3 版本管理 API

#### 4.3.1 获取插件版本列表

获取指定插件的所有版本信息，包括版本号、文件大小、创建时间、下载次数等。

**端点**: `POST /api/plugins/versions`
**权限**: 无需认证
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| name | string | 是 | 插件唯一标识符 |

**请求示例**:
```json
{
  "name": "com.example.myplugin"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "版本列表获取成功",
  "data": [
    {
      "plugin_name": "com.example.myplugin",
      "version": "1.0.0",
      "file_name": "myplugin@1.0.0.zip",
      "file_path": "./data/uploads/com.example.myplugin/myplugin@1.0.0.zip",
      "file_size": 1048576,
      "file_hash": "a1b2c3d4e5f6...",
      "changelog": "修复了一些bug，添加了新功能",
      "dependencies": {"com.other.plugin": ">=1.0.0"},
      "engines": {"MicroDock": ">=2.0.0"},
      "is_deprecated": false,
      "download_count": 42,
      "created_at": "2023-12-01T14:30:22Z"
    },
    {
      "plugin_name": "com.example.myplugin",
      "version": "0.9.0",
      "file_name": "myplugin@0.9.0.zip",
      "file_path": "./data/uploads/com.example.myplugin/myplugin@0.9.0.zip",
      "file_size": 983040,
      "file_hash": "b2c3d4e5f6a1...",
      "changelog": "初始版本发布",
      "dependencies": {},
      "engines": {"MicroDock": ">=1.5.0"},
      "is_deprecated": true,
      "download_count": 15,
      "created_at": "2023-11-15T10:20:30Z"
    }
  ]
}
```

#### 4.3.2 获取版本详情

获取指定插件版本的详细信息，包括文件信息、更新日志、依赖关系等。

**端点**: `POST /api/plugins/version/detail`
**权限**: 无需认证
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| name | string | 是 | 插件唯一标识符 |
| version | string | 是 | 插件版本号 (语义化版本) |

**请求示例**:
```json
{
  "name": "com.example.myplugin",
  "version": "1.0.0"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "版本详情获取成功",
  "data": {
    "plugin_name": "com.example.myplugin",
    "version": "1.0.0",
    "file_name": "myplugin@1.0.0.zip",
    "file_path": "./data/uploads/com.example.myplugin/myplugin@1.0.0.zip",
    "file_size": 1048576,
    "file_hash": "a1b2c3d4e5f6...",
    "changelog": "修复了一些bug，添加了新功能\n\n主要改进:\n- 修复了内存泄漏问题\n- 添加了新的用户界面\n- 提高了性能",
    "dependencies": {
      "com.other.plugin": ">=1.0.0",
      "com.utils.library": "^0.5.0"
    },
    "engines": {
      "MicroDock": ">=2.0.0"
    },
    "is_deprecated": false,
    "download_count": 42,
    "created_at": "2023-12-01T14:30:22Z"
  }
}
```

#### 4.3.3 标记版本过时

将指定插件版本标记为过时状态。

**端点**: `POST /api/plugins/version/deprecate`
**权限**: 需要管理员认证
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| name | string | 是 | 插件唯一标识符 |
| version | string | 是 | 插件版本号 |

**请求示例**:
```json
{
  "name": "com.example.myplugin",
  "version": "0.9.0"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "版本标记过时成功",
  "data": {
    "plugin_name": "com.example.myplugin",
    "version": "0.9.0",
    "file_name": "myplugin@0.9.0.zip",
    "file_path": "./data/uploads/com.example.myplugin/myplugin@0.9.0.zip",
    "file_size": 983040,
    "file_hash": "b2c3d4e5f6a1...",
    "changelog": "初始版本发布",
    "dependencies": {},
    "engines": {"MicroDock": ">=1.5.0"},
    "is_deprecated": true,
    "download_count": 15,
    "created_at": "2023-11-15T10:20:30Z"
  }
}
```

#### 4.3.4 下载指定版本

下载指定插件的特定版本 ZIP 文件。下载后会自动统计该版本的下载次数。

**端点**: `POST /api/plugins/version/download`
**权限**: 无需认证
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| name | string | 是 | 插件唯一标识符 |
| version | string | 是 | 插件版本号 |

**请求示例**:
```json
{
  "name": "com.example.myplugin",
  "version": "1.0.0"
}
```

**响应**: 文件流 (application/zip)
**Headers**:
```
Content-Disposition: attachment; filename="myplugin@1.0.0.zip"
Content-Type: application/zip
Content-Length: 1048576
```

### 4.4 备份管理 API

#### 4.4.1 上传备份文件

用户上传备份文件到服务器。支持程序备份和插件备份两种类型。

**端点**: `POST /api/backups/upload`
**权限**: 无需认证 (使用 user_key 验证)
**Content-Type**: `multipart/form-data`
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| file | file | 是 | 备份文件 (最大 100MB) |
| user_key | string | 是 | 用户密钥，用于标识用户 |
| backup_type | string | 是 | 备份类型: "program" 或 "plugin" |
| description | string | 否 | 备份描述 (可选) |

**备份类型说明**:
- `program`: MicroDock 主程序配置和数据备份
- `plugin`: 插件相关数据和配置备份

**请求示例**:
```http
POST /api/backups/upload
Content-Type: multipart/form-data

file: <backup_file.zip>
user_key: "user123abc"
backup_type: "program"
description: "程序配置备份"
```

**响应示例**:
```json
{
  "success": true,
  "message": "备份文件上传成功",
  "data": {
    "id": 123,
    "user_key": "user123abc",
    "backup_type": "program",
    "file_name": "backup.zip",
    "file_path": "./data/backups/user123abc/program/1701420622_backup.zip",
    "file_size": 2097152,
    "file_hash": "f1e2d3c4b5a6...",
    "description": "程序配置备份",
    "created_at": "2023-12-01T14:30:22Z"
  }
}
```

#### 4.4.2 获取用户备份列表

根据用户密钥获取该用户的所有备份文件列表。

**端点**: `POST /api/backups/list`
**权限**: 无需认证
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| user_key | string | 是 | 用户密钥 |

**请求示例**:
```json
{
  "user_key": "user123abc"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "备份列表获取成功",
  "data": {
    "total": 3,
    "backups": [
      {
        "id": 123,
        "user_key": "user123abc",
        "backup_type": "program",
        "file_name": "backup.zip",
        "file_path": "./data/backups/user123abc/program/1701420622_backup.zip",
        "file_size": 2097152,
        "file_hash": "f1e2d3c4b5a6...",
        "description": "程序配置备份",
        "created_at": "2023-12-01T14:30:22Z"
      },
      {
        "id": 124,
        "user_key": "user123abc",
        "backup_type": "plugin",
        "file_name": "plugins_backup.zip",
        "file_path": "./data/backups/user123abc/plugin/1701425882_plugins_backup.zip",
        "file_size": 5242880,
        "file_hash": "a7b8c9d0e1f2...",
        "description": "插件配置备份",
        "created_at": "2023-12-01T15:58:02Z"
      }
    ]
  }
}
```

#### 4.4.3 获取所有备份列表

获取系统中所有用户的备份文件列表。

**端点**: `GET /api/backups/list-all`
**权限**: 需要管理员认证
**参数**: 无

**响应示例**:
```json
{
  "success": true,
  "message": "备份列表获取成功",
  "data": {
    "total": 15,
    "backups": [
      {
        "id": 123,
        "user_key": "user123abc",
        "backup_type": "program",
        "file_name": "backup.zip",
        "file_path": "./data/backups/user123abc/program/1701420622_backup.zip",
        "file_size": 2097152,
        "file_hash": "f1e2d3c4b5a6...",
        "description": "程序配置备份",
        "created_at": "2023-12-01T14:30:22Z"
      },
      {
        "id": 124,
        "user_key": "user456def",
        "backup_type": "plugin",
        "file_name": "plugins_backup.zip",
        "file_path": "./data/backups/user456def/plugin/1701425882_plugins_backup.zip",
        "file_size": 5242880,
        "file_hash": "b8c9d0e1f2a3...",
        "description": "插件配置备份",
        "created_at": "2023-12-01T15:58:02Z"
      }
    ]
  }
}
```

#### 4.4.4 下载备份文件

根据用户密钥和备份 ID 下载指定的备份文件。

**端点**: `POST /api/backups/download`
**权限**: 无需认证
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| user_key | string | 是 | 用户密钥 |
| id | integer | 是 | 备份文件 ID |

**请求示例**:
```json
{
  "user_key": "user123abc",
  "id": 123
}
```

**响应**: 文件流 (application/octet-stream)
**Headers**:
```
Content-Disposition: attachment; filename="backup_program_20231201_143022.zip"
Content-Type: application/octet-stream
Content-Length: 2097152
```

#### 4.4.5 删除备份文件

删除指定的备份文件。此操作不可逆，请谨慎使用。

**端点**: `POST /api/backups/delete`
**权限**: 需要管理员认证
**参数**:

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| user_key | string | 是 | 用户密钥 |
| id | integer | 是 | 备份文件 ID |

**请求示例**:
```json
{
  "user_key": "user123abc",
  "id": 123
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "备份文件删除成功"
}
```

### 4.5 系统 API

#### 4.5.1 健康检查

检查服务器运行状态和数据库连接状态，用于监控和负载均衡。

**端点**: `GET /api/health`
**权限**: 无需认证
**参数**: 无

**响应示例**:
```json
{
  "success": true,
  "data": {
    "status": "healthy",
    "version": "2.0.0",
    "database": "connected"
  },
  "message": "服务器运行正常"
}
```

#### 4.5.2 根路径信息

获取 API 服务器的基本信息和访问入口。

**端点**: `GET /`
**权限**: 无需认证
**参数**: 无

**响应示例**:
```json
{
  "success": true,
  "data": {
    "message": "MicroDock PluginServer API",
    "version": "2.0.0",
    "docs": "/docs",
    "health": "/api/health"
  },
  "message": "API服务运行中"
}
```

---

## 5. 数据模型

### 5.1 Plugin (插件模型)

插件的基本信息和状态管理。

**表名**: `plugins`
**主键**: `name` (插件唯一标识符)

| 字段名 | 类型 | 说明 | 示例 |
|--------|------|------|------|
| name | string (主键) | 插件唯一标识符，采用反向域名格式 | "com.example.myplugin" |
| display_name | string | 插件显示名称 | "我的插件" |
| description | string | 插件描述信息 | "这是一个示例插件" |
| author | string | 插件作者 | "开发者姓名" |
| license | string | 许可证类型 | "MIT" |
| homepage | string | 插件主页 URL | "https://example.com/myplugin" |
| main_dll | string | 主 DLL 文件名 | "MyPlugin.dll" |
| entry_class | string | 入口类完全限定名 | "MyPlugin.PluginEntry" |
| current_version | string | 当前版本号 (语义化版本) | "1.0.0" |
| is_enabled | boolean | 是否启用 | true |
| is_deprecated | boolean | 是否已标记为过时 | false |
| upload_key | string | 上传密钥，用于防止恶意更新 | "my_secret_key_123" |
| created_at | datetime | 创建时间 | "2023-12-01T14:30:22Z" |
| updated_at | datetime | 更新时间 | "2023-12-01T14:35:22Z" |

**约束关系**:
- 与 PluginVersion 为一对多关系
- 删除插件时会级联删除所有相关版本

### 5.2 PluginVersion (插件版本模型)

插件的具体版本信息和文件管理。

**表名**: `plugin_versions`
**联合主键**: `(plugin_name, version)`

| 字段名 | 类型 | 说明 | 示例 |
|--------|------|------|------|
| plugin_name | string (联合主键) | 所属插件名称，外键引用 Plugin.name | "com.example.myplugin" |
| version | string (联合主键) | 版本号，采用语义化版本规范 | "1.0.0" |
| file_name | string | 存储文件名，格式: `插件名@版本.zip` | "myplugin@1.0.0.zip" |
| file_path | string | 文件在服务器上的存储路径 | "./data/uploads/com.example.myplugin/myplugin@1.0.0.zip" |
| file_size | integer | 文件大小 (字节) | 1048576 |
| file_hash | string | 文件的 SHA256 哈希值，用于完整性校验 | "a1b2c3d4e5f6..." |
| changelog | text | 版本更新日志 | "修复了一些bug，添加了新功能" |
| dependencies | text (JSON) | 依赖关系，存储为 JSON 格式 | `{"com.other.plugin": ">=1.0.0"}` |
| engines | text (JSON) | 引擎要求，存储为 JSON 格式 | `{"MicroDock": ">=2.0.0"}` |
| is_deprecated | boolean | 是否已标记为过时 | false |
| download_count | integer | 下载次数统计 | 42 |
| created_at | datetime | 版本创建时间 | "2023-12-01T14:30:22Z" |

**约束关系**:
- plugin_name 为外键，引用 Plugin.name
- 联合主键确保同一插件的版本号不能重复
- 删除插件时会级联删除所有版本

### 5.3 Backup (备份模型)

用户备份文件的管理和存储。

**表名**: `backups`
**主键**: `id` (自增整数)

| 字段名 | 类型 | 说明 | 示例 |
|--------|------|------|------|
| id | integer (主键) | 备份记录唯一标识符 | 123 |
| user_key | string | 用户密钥，用于标识用户 | "user123abc" |
| backup_type | string | 备份类型：program(程序) 或 plugin(插件) | "program" |
| file_name | string | 原始文件名 | "backup.zip" |
| file_path | string | 文件在服务器上的存储路径 | "./data/backups/user123abc/program/1701420622_backup.zip" |
| file_size | integer | 文件大小 (字节) | 2097152 |
| file_hash | string | 文件的 SHA256 哈希值 | "f1e2d3c4b5a6..." |
| description | string | 备份描述信息 | "程序配置备份" |
| created_at | datetime | 备份创建时间 | "2023-12-01T14:30:22Z" |

**文件存储结构**:
```
./data/backups/
├── {user_key}/
│   ├── program/
│   │   ├── {timestamp}_{filename}.zip
│   │   └── ...
│   └── plugin/
│       ├── {timestamp}_{filename}.zip
│       └── ...
└── ...
```

### 5.4 数据关系图

```
Plugin (1) ──────── (N) PluginVersion
    │                           │
    │                           ├── file_name
    ├── name (PK)               ├── file_path
    ├── display_name            ├── file_size
    ├── description             ├── file_hash
    ├── author                  ├── changelog
    ├── current_version         ├── dependencies (JSON)
    ├── is_enabled              ├── engines (JSON)
    └── is_deprecated           └── download_count

Backup
├── id (PK)
├── user_key
├── backup_type
├── file_name
├── file_path
├── file_size
├── file_hash
├── description
└── created_at
```

### 5.5 JSON 数据格式

#### 5.5.1 Dependencies (依赖关系)
```json
{
  "com.other.plugin": ">=1.0.0",
  "com.utils.library": "^0.5.0",
  "com.core.framework": "~2.1.0"
}
```

**版本约束语法**:
- `>=1.0.0`: 大于等于 1.0.0
- `^0.5.0`: 兼容版本 (>=0.5.0, <0.6.0)
- `~2.1.0`: 兼容补丁 (>=2.1.0, <2.2.0)
- `*`: 任意版本

#### 5.5.2 Engines (引擎要求)
```json
{
  "MicroDock": ">=2.0.0",
  ".NET": ">=6.0.0"
}
```

---

## 6. 开发指南

### 6.1 快速集成指南

#### 6.1.1 JavaScript/TypeScript 集成

**1. 安装 Axios**
```bash
npm install axios
```

**2. 创建 API 客户端**
```typescript
// src/services/api.ts
import axios, { AxiosInstance, AxiosRequestConfig } from 'axios';

class MicroDockAPI {
  private client: AxiosInstance;
  private token: string | null = null;

  constructor(baseURL: string = 'http://localhost:8000') {
    this.client = axios.create({
      baseURL,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // 请求拦截器 - 自动添加认证头
    this.client.interceptors.request.use((config) => {
      if (this.token) {
        config.headers.Authorization = `Bearer ${this.token}`;
      }
      return config;
    });

    // 响应拦截器 - 统一错误处理
    this.client.interceptors.response.use(
      (response) => response.data,
      (error) => {
        if (error.response?.status === 401) {
          this.token = null;
          // 处理认证失败，如跳转登录页面
        }
        return Promise.reject(error.response?.data || error.message);
      }
    );
  }

  // 认证方法
  async login(username: string, password: string) {
    const response = await this.client.post('/api/auth/login', {
      username,
      password,
    });

    if (response.success) {
      this.token = response.token;
      localStorage.setItem('microdock_token', response.token);
    }

    return response;
  }

  logout() {
    this.token = null;
    localStorage.removeItem('microdock_token');
    return this.client.post('/api/auth/logout');
  }

  // 插件管理方法
  async getPlugins() {
    return this.client.get('/api/plugins/list');
  }

  async getPluginDetail(name: string) {
    return this.client.post('/api/plugins/detail', { name });
  }

  async uploadPlugin(file: File, pluginKey: string) {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('plugin_key', pluginKey);

    return this.client.post('/api/plugins/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  }

  async enablePlugin(name: string) {
    return this.client.post('/api/plugins/enable', { name });
  }

  async disablePlugin(name: string) {
    return this.client.post('/api/plugins/disable', { name });
  }

  async downloadPlugin(name: string) {
    const response = await this.client.post('/api/plugins/download', { name }, {
      responseType: 'blob',
    });

    // 创建下载链接
    const url = window.URL.createObjectURL(new Blob([response]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `${name}.zip`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);

    return response;
  }

  // 版本管理方法
  async getPluginVersions(name: string) {
    return this.client.post('/api/plugins/versions', { name });
  }

  async getVersionDetail(name: string, version: string) {
    return this.client.post('/api/plugins/version/detail', { name, version });
  }

  async downloadVersion(name: string, version: string) {
    const response = await this.client.post('/api/plugins/version/download', {
      name,
      version
    }, {
      responseType: 'blob',
    });

    // 创建下载链接
    const url = window.URL.createObjectURL(new Blob([response]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `${name}@${version}.zip`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);

    return response;
  }

  // 备份管理方法
  async uploadBackup(file: File, userKey: string, backupType: string, description?: string) {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('user_key', userKey);
    formData.append('backup_type', backupType);
    if (description) {
      formData.append('description', description);
    }

    return this.client.post('/api/backups/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  }

  async getBackupList(userKey: string) {
    return this.client.post('/api/backups/list', { user_key: userKey });
  }

  async downloadBackup(userKey: string, backupId: number) {
    const response = await this.client.post('/api/backups/download', {
      user_key: userKey,
      id: backupId
    }, {
      responseType: 'blob',
    });

    // 创建下载链接
    const url = window.URL.createObjectURL(new Blob([response]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `backup_${backupId}.zip`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);

    return response;
  }

  // 系统方法
  async getHealthStatus() {
    return this.client.get('/api/health');
  }

  // 从本地存储恢复 token
  restoreToken() {
    const token = localStorage.getItem('microdock_token');
    if (token) {
      this.token = token;
    }
  }
}

// 导出单例
export const microdockAPI = new MicroDockAPI();
```

**3. 使用示例**
```typescript
import { microdockAPI } from './services/api';

// 登录
async function handleLogin() {
  try {
    const result = await microdockAPI.login('admin', 'admin');
    console.log('登录成功:', result);
  } catch (error) {
    console.error('登录失败:', error);
  }
}

// 获取插件列表
async function loadPlugins() {
  try {
    const result = await microdockAPI.getPlugins();
    console.log('插件列表:', result.data);
  } catch (error) {
    console.error('获取插件列表失败:', error);
  }
}

// 上传插件
async function handlePluginUpload(file: File, pluginKey: string) {
  try {
    const result = await microdockAPI.uploadPlugin(file, pluginKey);
    console.log('插件上传成功:', result);
  } catch (error) {
    console.error('插件上传失败:', error);
  }
}

// 下载插件
async function handlePluginDownload(pluginName: string) {
  try {
    await microdockAPI.downloadPlugin(pluginName);
    console.log('插件下载成功');
  } catch (error) {
    console.error('插件下载失败:', error);
  }
}

// 应用启动时恢复 token
microdockAPI.restoreToken();
```

#### 6.1.2 Python 集成

**1. 安装依赖**
```bash
pip install requests aiohttp
```

**2. 创建 API 客户端**
```python
# microdock_client.py
import requests
import json
from typing import Optional, Dict, Any, Union
from pathlib import Path

class MicroDockClient:
    def __init__(self, base_url: str = "http://localhost:8000"):
        self.base_url = base_url.rstrip('/')
        self.session = requests.Session()
        self.session.headers.update({
            'Content-Type': 'application/json',
            'User-Agent': 'MicroDock-Python-Client/1.0'
        })
        self.token: Optional[str] = None

    def _make_request(self, method: str, endpoint: str,
                     data: Optional[Dict[str, Any]] = None,
                     files: Optional[Dict[str, Any]] = None,
                     return_response: bool = False) -> Union[Dict[str, Any], requests.Response]:
        """发送 HTTP 请求"""
        url = f"{self.base_url}{endpoint}"

        kwargs = {}
        if data and not files:
            kwargs['json'] = data
        elif files:
            kwargs['files'] = files
            kwargs['data'] = data

        if self.token:
            self.session.headers.update({
                'Authorization': f'Bearer {self.token}'
            })

        response = self.session.request(method, url, **kwargs)

        if return_response:
            return response

        # 对于文件下载，返回原始响应
        if response.headers.get('content-type', '').startswith('application/'):
            return response

        try:
            return response.json()
        except json.JSONDecodeError:
            return {'success': False, 'message': 'Invalid JSON response', 'data': response.text}

    # 认证方法
    def login(self, username: str, password: str) -> Dict[str, Any]:
        """管理员登录"""
        response = self._make_request('POST', '/api/auth/login', {
            'username': username,
            'password': password
        })

        if response.get('success') and 'token' in response:
            self.token = response['token']

        return response

    def logout(self) -> Dict[str, Any]:
        """管理员登出"""
        response = self._make_request('POST', '/api/auth/logout')
        self.token = None
        return response

    def get_auth_status(self) -> Dict[str, Any]:
        """获取认证状态"""
        return self._make_request('GET', '/api/auth/me')

    # 插件管理方法
    def get_plugins(self) -> Dict[str, Any]:
        """获取插件列表"""
        return self._make_request('GET', '/api/plugins/list')

    def get_plugin_detail(self, name: str) -> Dict[str, Any]:
        """获取插件详情"""
        return self._make_request('POST', '/api/plugins/detail', {'name': name})

    def upload_plugin(self, file_path: Union[str, Path], plugin_key: str) -> Dict[str, Any]:
        """上传插件"""
        file_path = Path(file_path)
        if not file_path.exists():
            return {'success': False, 'message': f'文件不存在: {file_path}'}

        with open(file_path, 'rb') as f:
            files = {'file': (file_path.name, f, 'application/zip')}
            data = {'plugin_key': plugin_key}
            return self._make_request('POST', '/api/plugins/upload', data=data, files=files)

    def enable_plugin(self, name: str) -> Dict[str, Any]:
        """启用插件"""
        return self._make_request('POST', '/api/plugins/enable', {'name': name})

    def disable_plugin(self, name: str) -> Dict[str, Any]:
        """禁用插件"""
        return self._make_request('POST', '/api/plugins/disable', {'name': name})

    def deprecate_plugin(self, name: str) -> Dict[str, Any]:
        """标记插件过时"""
        return self._make_request('POST', '/api/plugins/deprecate', {'name': name})

    def delete_plugin(self, name: str) -> Dict[str, Any]:
        """删除插件"""
        return self._make_request('POST', '/api/plugins/delete', {'name': name})

    def download_plugin(self, name: str, save_path: Optional[Union[str, Path]] = None) -> requests.Response:
        """下载插件"""
        response = self._make_request('POST', '/api/plugins/download',
                                     {'name': name}, return_response=True)

        if save_path and response.status_code == 200:
            save_path = Path(save_path)
            # 从响应头获取文件名
            content_disposition = response.headers.get('content-disposition', '')
            if 'filename=' in content_disposition:
                filename = content_disposition.split('filename=')[1].strip('"')
            else:
                filename = f"{name}.zip"

            with open(save_path / filename, 'wb') as f:
                f.write(response.content)

        return response

    # 版本管理方法
    def get_plugin_versions(self, name: str) -> Dict[str, Any]:
        """获取插件版本列表"""
        return self._make_request('POST', '/api/plugins/versions', {'name': name})

    def get_version_detail(self, name: str, version: str) -> Dict[str, Any]:
        """获取版本详情"""
        return self._make_request('POST', '/api/plugins/version/detail', {
            'name': name,
            'version': version
        })

    def deprecate_version(self, name: str, version: str) -> Dict[str, Any]:
        """标记版本过时"""
        return self._make_request('POST', '/api/plugins/version/deprecate', {
            'name': name,
            'version': version
        })

    def download_version(self, name: str, version: str,
                         save_path: Optional[Union[str, Path]] = None) -> requests.Response:
        """下载指定版本"""
        response = self._make_request('POST', '/api/plugins/version/download',
                                     {'name': name, 'version': version},
                                     return_response=True)

        if save_path and response.status_code == 200:
            save_path = Path(save_path)
            content_disposition = response.headers.get('content-disposition', '')
            if 'filename=' in content_disposition:
                filename = content_disposition.split('filename=')[1].strip('"')
            else:
                filename = f"{name}@{version}.zip"

            with open(save_path / filename, 'wb') as f:
                f.write(response.content)

        return response

    # 备份管理方法
    def upload_backup(self, file_path: Union[str, Path], user_key: str,
                     backup_type: str, description: Optional[str] = None) -> Dict[str, Any]:
        """上传备份"""
        file_path = Path(file_path)
        if not file_path.exists():
            return {'success': False, 'message': f'文件不存在: {file_path}'}

        if backup_type not in ['program', 'plugin']:
            return {'success': False, 'message': '备份类型必须是 program 或 plugin'}

        with open(file_path, 'rb') as f:
            files = {'file': (file_path.name, f, 'application/octet-stream')}
            data = {
                'user_key': user_key,
                'backup_type': backup_type
            }
            if description:
                data['description'] = description

            return self._make_request('POST', '/api/backups/upload', data=data, files=files)

    def get_backup_list(self, user_key: str) -> Dict[str, Any]:
        """获取用户备份列表"""
        return self._make_request('POST', '/api/backups/list', {'user_key': user_key})

    def get_all_backup_list(self) -> Dict[str, Any]:
        """获取所有备份列表（需要管理员权限）"""
        return self._make_request('GET', '/api/backups/list-all')

    def download_backup(self, user_key: str, backup_id: int,
                       save_path: Optional[Union[str, Path]] = None) -> requests.Response:
        """下载备份"""
        response = self._make_request('POST', '/api/backups/download',
                                     {'user_key': user_key, 'id': backup_id},
                                     return_response=True)

        if save_path and response.status_code == 200:
            save_path = Path(save_path)
            content_disposition = response.headers.get('content-disposition', '')
            if 'filename=' in content_disposition:
                filename = content_disposition.split('filename=')[1].strip('"')
            else:
                filename = f"backup_{backup_id}.zip"

            with open(save_path / filename, 'wb') as f:
                f.write(response.content)

        return response

    def delete_backup(self, user_key: str, backup_id: int) -> Dict[str, Any]:
        """删除备份（需要管理员权限）"""
        return self._make_request('POST', '/api/backups/delete', {
            'user_key': user_key,
            'id': backup_id
        })

    # 系统方法
    def get_health_status(self) -> Dict[str, Any]:
        """获取健康状态"""
        return self._make_request('GET', '/api/health')

    def get_server_info(self) -> Dict[str, Any]:
        """获取服务器信息"""
        return self._make_request('GET', '/')

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.session.close()
```

**3. 使用示例**
```python
# example_usage.py
from microdock_client import MicroDockClient
from pathlib import Path

def main():
    # 创建客户端
    with MicroDockClient() as client:
        # 登录
        login_result = client.login('admin', 'admin')
        print("登录结果:", login_result)

        if login_result.get('success'):
            # 获取插件列表
            plugins = client.get_plugins()
            print("插件列表:", plugins)

            # 上传插件
            plugin_file = Path("my_plugin.zip")
            if plugin_file.exists():
                upload_result = client.upload_plugin(plugin_file, "my_secret_key")
                print("上传结果:", upload_result)

            # 下载插件
            download_result = client.download_plugin("com.example.myplugin",
                                                    save_path=Path("./downloads"))
            print("下载状态码:", download_result.status_code)

            # 获取健康状态
            health = client.get_health_status()
            print("健康状态:", health)

if __name__ == "__main__":
    main()
```

### 6.2 错误处理最佳实践

#### 6.2.1 HTTP 状态码处理

```typescript
// 统一错误处理
class APIError extends Error {
  constructor(
    public status: number,
    public message: string,
    public error?: string
  ) {
    super(message);
    this.name = 'APIError';
  }
}

const handleAPIResponse = (response: any) => {
  if (!response.success) {
    throw new APIError(
      response.status || 500,
      response.message || '操作失败',
      response.error
    );
  }
  return response.data;
};

// 使用示例
try {
  const plugins = await microdockAPI.getPlugins();
  const pluginData = handleAPIResponse(plugins);
  console.log('插件数据:', pluginData);
} catch (error) {
  if (error instanceof APIError) {
    switch (error.status) {
      case 401:
        console.error('认证失败，请重新登录');
        // 跳转登录页面
        break;
      case 403:
        console.error('权限不足');
        break;
      case 404:
        console.error('资源不存在');
        break;
      case 409:
        console.error('资源冲突:', error.message);
        break;
      default:
        console.error('API 错误:', error.message);
    }
  } else {
    console.error('未知错误:', error);
  }
}
```

#### 6.2.2 文件上传错误处理

```typescript
const uploadPluginWithValidation = async (file: File, pluginKey: string) => {
  // 文件大小验证
  const MAX_SIZE = 100 * 1024 * 1024; // 100MB
  if (file.size > MAX_SIZE) {
    throw new Error('文件大小超过 100MB 限制');
  }

  // 文件类型验证
  if (!file.name.endsWith('.zip')) {
    throw new Error('仅支持 ZIP 格式的文件');
  }

  try {
    const response = await microdockAPI.uploadPlugin(file, pluginKey);
    return handleAPIResponse(response);
  } catch (error) {
    if (error instanceof APIError) {
      switch (error.status) {
        case 400:
          throw new Error(`文件验证失败: ${error.error}`);
        case 409:
          throw new Error('插件版本已存在，请先删除旧版本或使用新版本号');
        case 413:
          throw new Error('文件过大，请确保文件小于 100MB');
        default:
          throw error;
      }
    }
    throw error;
  }
};
```

### 6.3 文件操作最佳实践

#### 6.3.1 插件包创建

```python
# create_plugin_package.py
import json
import zipfile
from pathlib import Path
from typing import Dict, Any, Optional

class PluginPackageBuilder:
    def __init__(self, plugin_name: str, version: str):
        self.plugin_name = plugin_name
        self.version = version
        self.files = []
        self.plugin_config = {
            "name": plugin_name,
            "version": version,
            "main": "",
            "entryClass": ""
        }

    def set_main_dll(self, dll_name: str, entry_class: str):
        """设置主 DLL 信息"""
        self.plugin_config["main"] = dll_name
        self.plugin_config["entryClass"] = entry_class
        return self

    def set_metadata(self, display_name: str = "", description: str = "",
                    author: str = "", license: str = "",
                    homepage: str = "", changelog: str = ""):
        """设置插件元数据"""
        if display_name:
            self.plugin_config["displayName"] = display_name
        if description:
            self.plugin_config["description"] = description
        if author:
            self.plugin_config["author"] = author
        if license:
            self.plugin_config["license"] = license
        if homepage:
            self.plugin_config["homepage"] = homepage
        if changelog:
            self.plugin_config["changelog"] = changelog
        return self

    def set_dependencies(self, dependencies: Dict[str, str]):
        """设置依赖关系"""
        self.plugin_config["dependencies"] = dependencies
        return self

    def set_engines(self, engines: Dict[str, str]):
        """设置引擎要求"""
        self.plugin_config["engines"] = engines
        return self

    def add_file(self, file_path: Path, arcname: Optional[str] = None):
        """添加文件到插件包"""
        if not file_path.exists():
            raise FileNotFoundError(f"文件不存在: {file_path}")

        self.files.append({
            'path': file_path,
            'arcname': arcname or file_path.name
        })
        return self

    def add_directory(self, dir_path: Path, arcname: Optional[str] = None):
        """添加目录到插件包"""
        if not dir_path.exists() or not dir_path.is_dir():
            raise ValueError(f"目录不存在或不是有效目录: {dir_path}")

        for file_path in dir_path.rglob('*'):
            if file_path.is_file():
                relative_path = file_path.relative_to(dir_path)
                arc_path = f"{arcname or dir_path.name}/{relative_path}"
                self.add_file(file_path, arc_path)

        return self

    def build(self, output_path: Path) -> Path:
        """构建插件包"""
        # 验证必需配置
        if not self.plugin_config["main"]:
            raise ValueError("必须设置主 DLL 文件名")
        if not self.plugin_config["entryClass"]:
            raise ValueError("必须设置入口类名")

        # 确保输出目录存在
        output_path.parent.mkdir(parents=True, exist_ok=True)

        # 创建 ZIP 文件
        with zipfile.ZipFile(output_path, 'w', zipfile.ZIP_DEFLATED) as zf:
            # 添加 plugin.json
            plugin_json_content = json.dumps(self.plugin_config, indent=2, ensure_ascii=False)
            zf.writestr('plugin.json', plugin_json_content)

            # 添加文件
            for file_info in self.files:
                zf.write(file_info['path'], file_info['arcname'])

        return output_path

# 使用示例
def create_example_plugin():
    builder = PluginPackageBuilder("com.example.myplugin", "1.0.0")

    # 设置基本信息
    builder.set_main_dll("MyPlugin.dll", "MyPlugin.PluginEntry")

    # 设置元数据
    builder.set_metadata(
        display_name="我的插件",
        description="这是一个示例插件",
        author="开发者姓名",
        license="MIT",
        homepage="https://example.com/myplugin",
        changelog="初始版本发布"
    )

    # 设置依赖和引擎要求
    builder.set_dependencies({
        "com.other.plugin": ">=1.0.0"
    })
    builder.set_engines({
        "MicroDock": ">=2.0.0"
    })

    # 添加文件
    plugin_dir = Path("./plugin_files")
    if plugin_dir.exists():
        builder.add_file(plugin_dir / "MyPlugin.dll")
        builder.add_file(plugin_dir / "README.md")
        builder.add_directory(plugin_dir / "resources", "resources")

    # 构建插件包
    output_file = Path("./myplugin@1.0.0.zip")
    result_path = builder.build(output_file)

    print(f"插件包已创建: {result_path}")
    return result_path

if __name__ == "__main__":
    create_example_plugin()
```

### 6.4 性能优化建议

#### 6.4.1 前端优化

```typescript
// 1. 使用缓存
const pluginCache = new Map<string, any>();

const getCachedPlugins = async () => {
  const cacheKey = 'plugins_list';
  const cached = pluginCache.get(cacheKey);

  // 缓存 5 分钟
  if (cached && Date.now() - cached.timestamp < 5 * 60 * 1000) {
    return cached.data;
  }

  const plugins = await microdockAPI.getPlugins();
  pluginCache.set(cacheKey, {
    data: plugins,
    timestamp: Date.now()
  });

  return plugins;
};

// 2. 分页加载
const getPluginsPaginated = async (page: number = 1, pageSize: number = 20) => {
  const plugins = await getCachedPlugins();
  const start = (page - 1) * pageSize;
  const end = start + pageSize;

  return {
    data: plugins.data?.slice(start, end) || [],
    total: plugins.data?.length || 0,
    page,
    pageSize,
    totalPages: Math.ceil((plugins.data?.length || 0) / pageSize)
  };
};

// 3. 防抖搜索
const debounce = <T extends (...args: any[]) => any>(
  func: T,
  delay: number
): ((...args: Parameters<T>) => void) => {
  let timeoutId: NodeJS.Timeout;

  return (...args: Parameters<T>) => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => func(...args), delay);
  };
};

const searchPlugins = debounce(async (query: string) => {
  if (!query.trim()) {
    return getCachedPlugins();
  }

  const plugins = await getCachedPlugins();
  const filtered = plugins.data?.filter(plugin =>
    plugin.display_name.toLowerCase().includes(query.toLowerCase()) ||
    plugin.name.toLowerCase().includes(query.toLowerCase()) ||
    plugin.description?.toLowerCase().includes(query.toLowerCase())
  ) || [];

  return { success: true, data: filtered, message: '搜索完成' };
}, 300);
```

#### 6.4.2 后端优化

```python
# 1. 数据库查询优化
from sqlalchemy import select, func, and_
from sqlalchemy.orm import selectinload

class PluginService:
    async def get_plugins_paginated(self, skip: int = 0, limit: int = 20,
                                   search: Optional[str] = None):
        """分页获取插件列表"""
        query = select(Plugin)

        # 搜索过滤
        if search:
            search_filter = or_(
                Plugin.display_name.ilike(f"%{search}%"),
                Plugin.name.ilike(f"%{search}%"),
                Plugin.description.ilike(f"%{search}%")
            )
            query = query.where(search_filter)

        # 分页
        query = query.offset(skip).limit(limit).order_by(Plugin.updated_at.desc())

        result = await self.db.execute(query)
        plugins = result.scalars().all()

        return plugins

    async def get_plugins_with_version_count(self):
        """获取插件及其版本数量"""
        query = select(
            Plugin,
            func.count(PluginVersion.version).label('version_count')
        ).outerjoin(PluginVersion).group_by(Plugin.name)

        result = await self.db.execute(query)
        return result.all()

# 2. 缓存装饰器
from functools import wraps
import hashlib
import json

def cache_result(expire_time: int = 300):
    def decorator(func):
        cache = {}

        @wraps(func)
        async def wrapper(*args, **kwargs):
            # 生成缓存键
            key_data = json.dumps([str(arg) for arg in args], sort_keys=True)
            key_data += json.dumps(kwargs, sort_keys=True)
            cache_key = hashlib.md5(key_data.encode()).hexdigest()

            # 检查缓存
            if cache_key in cache:
                timestamp, result = cache[cache_key]
                if time.time() - timestamp < expire_time:
                    return result

            # 执行函数并缓存结果
            result = await func(*args, **kwargs)
            cache[cache_key] = (time.time(), result)

            return result

        return wrapper
    return decorator

# 使用示例
@cache_result(expire_time=600)  # 缓存 10 分钟
async def get_plugin_statistics():
    """获取插件统计信息"""
    total_plugins = await self.db.execute(select(func.count(Plugin.name)))
    total_versions = await self.db.execute(select(func.count(PluginVersion.version)))
    enabled_plugins = await self.db.execute(
        select(func.count(Plugin.name)).where(Plugin.is_enabled == True)
    )

    return {
        'total_plugins': total_plugins.scalar(),
        'total_versions': total_versions.scalar(),
        'enabled_plugins': enabled_plugins.scalar(),
    }
```

---

## 7. 部署与配置

### 7.1 环境配置

#### 7.1.1 开发环境配置

**后端配置文件 (backend/app/config.py)**

```python
from pydantic_settings import BaseSettings
from typing import List, Set
from pathlib import Path

class Settings(BaseSettings):
    # 应用基本配置
    APP_NAME: str = "MicroDock Plugin Server"
    APP_VERSION: str = "2.0.0"
    DEBUG: bool = True

    # 服务器配置
    HOST: str = "0.0.0.0"
    PORT: int = 8000

    # 数据库配置
    DATABASE_URL: str = "sqlite+aiosqlite:///./data/plugins.db"

    # 文件存储配置
    UPLOAD_DIR: Path = Path("./data/uploads")
    BACKUP_DIR: Path = Path("./data/backups")
    TEMP_DIR: Path = Path("./data/temp")
    MAX_UPLOAD_SIZE: int = 100 * 1024 * 1024  # 100MB
    ALLOWED_EXTENSIONS: Set[str] = {".zip"}

    # 认证配置
    ADMIN_USERNAME: str = "admin"
    ADMIN_PASSWORD: str = "admin"
    JWT_SECRET_KEY: str = "your-secret-key-change-in-production"
    JWT_ALGORITHM: str = "HS256"
    JWT_EXPIRE_MINUTES: int = 1440  # 24 hours

    # CORS 配置
    CORS_ORIGINS: List[str] = [
        "http://localhost:3000",
        "http://localhost:3001",
        "http://127.0.0.1:3000",
        "http://127.0.0.1:3001"
    ]

    # 日志配置
    LOG_LEVEL: str = "INFO"
    LOG_FILE: str = "./logs/app.log"

    class Config:
        env_file = ".env"
        case_sensitive = True

settings = Settings()
```

**环境变量文件 (.env)**

```bash
# 应用配置
APP_NAME=MicroDock Plugin Server
APP_VERSION=2.0.0
DEBUG=true

# 服务器配置
HOST=0.0.0.0
PORT=8000

# 数据库配置
DATABASE_URL=sqlite+aiosqlite:///./data/plugins.db

# 文件存储配置
UPLOAD_DIR=./data/uploads
BACKUP_DIR=./data/backups
TEMP_DIR=./data/temp
MAX_UPLOAD_SIZE=104857600
ALLOWED_EXTENSIONS=.zip

# 认证配置 - 生产环境请务必修改！
ADMIN_USERNAME=admin
ADMIN_PASSWORD=admin
JWT_SECRET_KEY=your-very-secure-secret-key-change-this-in-production
JWT_ALGORITHM=HS256
JWT_EXPIRE_MINUTES=1440

# CORS 配置
CORS_ORIGINS=http://localhost:3000,http://localhost:3001,http://127.0.0.1:3000,http://127.0.0.1:3001

# 日志配置
LOG_LEVEL=INFO
LOG_FILE=./logs/app.log
```

#### 7.1.2 生产环境配置

**生产环境 .env 文件**

```bash
# 应用配置
APP_NAME=MicroDock Plugin Server
APP_VERSION=2.0.0
DEBUG=false

# 服务器配置
HOST=0.0.0.0
PORT=8000

# 数据库配置
DATABASE_URL=sqlite+aiosqlite:///./data/plugins.db

# 文件存储配置
UPLOAD_DIR=/app/data/uploads
BACKUP_DIR=/app/data/backups
TEMP_DIR=/app/data/temp
MAX_UPLOAD_SIZE=104857600
ALLOWED_EXTENSIONS=.zip

# 认证配置 - 请使用强密码和密钥！
ADMIN_USERNAME=your_admin_username
ADMIN_PASSWORD=your_very_secure_password
JWT_SECRET_KEY=your-very-secure-jwt-secret-key-minimum-32-characters
JWT_ALGORITHM=HS256
JWT_EXPIRE_MINUTES=480  # 8 hours

# CORS 配置 - 请修改为实际的前端域名
CORS_ORIGINS=https://yourdomain.com,https://api.yourdomain.com

# 日志配置
LOG_LEVEL=WARNING
LOG_FILE=/app/logs/app.log
```

### 7.2 Docker 部署

#### 7.2.1 Dockerfile (Backend)

```dockerfile
# backend/Dockerfile
FROM python:3.11-slim

# 设置工作目录
WORKDIR /app

# 安装系统依赖
RUN apt-get update && apt-get install -y \
    gcc \
    && rm -rf /var/lib/apt/lists/*

# 复制依赖文件
COPY requirements.txt .

# 安装 Python 依赖
RUN pip install --no-cache-dir -r requirements.txt

# 复制应用代码
COPY . .

# 创建必要的目录
RUN mkdir -p /app/data/uploads /app/data/backups /app/data/temp /app/logs

# 设置权限
RUN chmod 755 /app/data

# 暴露端口
EXPOSE 8000

# 启动命令
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
```

#### 7.2.2 Dockerfile (Frontend)

```dockerfile
# frontend/Dockerfile
FROM node:18-alpine as build

# 设置工作目录
WORKDIR /app

# 复制 package 文件
COPY package*.json ./

# 安装依赖
RUN npm ci --only=production

# 复制源代码
COPY . .

# 构建应用
RUN npm run build

# 生产镜像
FROM nginx:alpine

# 复制构建结果
COPY --from=build /app/dist /usr/share/nginx/html

# 复制 nginx 配置
COPY nginx.conf /etc/nginx/conf.d/default.conf

# 暴露端口
EXPOSE 80

# 启动 nginx
CMD ["nginx", "-g", "daemon off;"]
```

#### 7.2.3 Docker Compose

```yaml
# docker-compose.yml
version: '3.8'

services:
  backend:
    build:
      context: ./backend
      dockerfile: Dockerfile
    container_name: microdock-backend
    restart: unless-stopped
    ports:
      - "8000:8000"
    environment:
      - DATABASE_URL=sqlite+aiosqlite:///./data/plugins.db
      - UPLOAD_DIR=/app/data/uploads
      - BACKUP_DIR=/app/data/backups
      - TEMP_DIR=/app/data/temp
      - LOG_FILE=/app/logs/app.log
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
    networks:
      - microdock-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8000/api/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    container_name: microdock-frontend
    restart: unless-stopped
    ports:
      - "80:80"
    depends_on:
      - backend
    networks:
      - microdock-network

  nginx:
    image: nginx:alpine
    container_name: microdock-nginx
    restart: unless-stopped
    ports:
      - "443:443"
      - "8080:80"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/conf.d/default.conf
      - ./nginx/ssl:/etc/nginx/ssl
    depends_on:
      - backend
      - frontend
    networks:
      - microdock-network

networks:
  microdock-network:
    driver: bridge

volumes:
  data:
    driver: local
  logs:
    driver: local
```

#### 7.2.4 Nginx 配置

```nginx
# nginx/nginx.conf
upstream backend {
    server backend:8000;
}

upstream frontend {
    server frontend:80;
}

server {
    listen 80;
    server_name localhost;

    # 前端静态文件
    location / {
        proxy_pass http://frontend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # API 请求
    location /api/ {
        proxy_pass http://backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # 文件上传大小限制
        client_max_body_size 100M;

        # 超时设置
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # Swagger 文档
    location /docs {
        proxy_pass http://backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # 健康检查
    location /health {
        access_log off;
        return 200 "healthy\n";
        add_header Content-Type text/plain;
    }
}

# HTTPS 配置 (生产环境)
server {
    listen 443 ssl http2;
    server_name yourdomain.com;

    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512:ECDHE-RSA-AES256-GCM-SHA384:DHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;

    # 其他配置同上...
}
```

### 7.3 部署脚本

#### 7.3.1 自动化部署脚本

```bash
#!/bin/bash
# deploy.sh

set -e

echo "开始部署 MicroDock PluginServer..."

# 检查 Docker 是否安装
if ! command -v docker &> /dev/null; then
    echo "错误: Docker 未安装"
    exit 1
fi

if ! command -v docker-compose &> /dev/null; then
    echo "错误: Docker Compose 未安装"
    exit 1
fi

# 设置环境
ENVIRONMENT=${1:-production}
echo "部署环境: $ENVIRONMENT"

# 创建必要的目录
mkdir -p data/uploads data/backups data/temp logs

# 设置权限
chmod 755 data
chmod 755 data/uploads data/backups data/temp
chmod 755 logs

# 复制环境配置文件
if [ "$ENVIRONMENT" = "production" ]; then
    cp .env.production .env
    echo "使用生产环境配置"
elif [ "$ENVIRONMENT" = "development" ]; then
    cp .env.development .env
    echo "使用开发环境配置"
else
    echo "使用默认配置"
fi

# 停止现有服务
echo "停止现有服务..."
docker-compose down

# 拉取最新代码
echo "拉取最新代码..."
git pull origin main

# 构建镜像
echo "构建 Docker 镜像..."
docker-compose build --no-cache

# 启动服务
echo "启动服务..."
docker-compose up -d

# 等待服务启动
echo "等待服务启动..."
sleep 30

# 健康检查
echo "执行健康检查..."
for i in {1..10}; do
    if curl -f http://localhost:8000/api/health &> /dev/null; then
        echo "✅ 后端服务健康"
        break
    else
        echo "⏳ 等待后端服务启动... ($i/10)"
        sleep 10
    fi
done

for i in {1..10}; do
    if curl -f http://localhost &> /dev/null; then
        echo "✅ 前端服务健康"
        break
    else
        echo "⏳ 等待前端服务启动... ($i/10)"
        sleep 10
    fi
done

# 显示服务状态
echo "服务状态:"
docker-compose ps

echo "🎉 部署完成!"
echo "前端地址: http://localhost"
echo "API 地址: http://localhost/api"
echo "API 文档: http://localhost/docs"
```

#### 7.3.2 备份脚本

```bash
#!/bin/bash
# backup.sh

set -e

BACKUP_DIR="./backups"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_NAME="microdock_backup_$DATE"

echo "开始备份 MicroDock PluginServer..."

# 创建备份目录
mkdir -p "$BACKUP_DIR/$BACKUP_NAME"

# 备份数据库
echo "备份数据库..."
docker exec microdock-backend cp /app/data/plugins.db "./backups/$BACKUP_NAME/plugins.db"

# 备份上传文件
echo "备份上传文件..."
docker cp microdock-backend:/app/data/uploads "./backups/$BACKUP_NAME/uploads"

# 备份配置文件
echo "备份配置文件..."
cp .env "$BACKUP_DIR/$BACKUP_NAME/"
cp docker-compose.yml "$BACKUP_DIR/$BACKUP_NAME/"

# 创建压缩包
echo "创建压缩包..."
cd "$BACKUP_DIR"
tar -czf "$BACKUP_NAME.tar.gz" "$BACKUP_NAME"
rm -rf "$BACKUP_NAME"

echo "✅ 备份完成: $BACKUP_DIR/$BACKUP_NAME.tar.gz"
```

#### 7.3.3 恢复脚本

```bash
#!/bin/bash
# restore.sh

set -e

if [ -z "$1" ]; then
    echo "用法: ./restore.sh <backup_file.tar.gz>"
    exit 1
fi

BACKUP_FILE="$1"
TEMP_DIR="./temp_restore"

echo "开始恢复 MicroDock PluginServer..."
echo "备份文件: $BACKUP_FILE"

# 停止服务
echo "停止服务..."
docker-compose down

# 创建临时目录
mkdir -p "$TEMP_DIR"

# 解压备份文件
echo "解压备份文件..."
tar -xzf "$BACKUP_FILE" -C "$TEMP_DIR"

# 获取备份目录名
BACKUP_DIR=$(ls "$TEMP_DIR")

# 恢复数据库
echo "恢复数据库..."
if [ -f "$TEMP_DIR/$BACKUP_DIR/plugins.db" ]; then
    cp "$TEMP_DIR/$BACKUP_DIR/plugins.db" "./data/"
    echo "✅ 数据库恢复完成"
else
    echo "⚠️ 未找到数据库文件"
fi

# 恢复上传文件
echo "恢复上传文件..."
if [ -d "$TEMP_DIR/$BACKUP_DIR/uploads" ]; then
    rm -rf "./data/uploads"
    cp -r "$TEMP_DIR/$BACKUP_DIR/uploads" "./data/"
    echo "✅ 上传文件恢复完成"
else
    echo "⚠️ 未找到上传文件"
fi

# 恢复配置文件
echo "恢复配置文件..."
if [ -f "$TEMP_DIR/$BACKUP_DIR/.env" ]; then
    cp "$TEMP_DIR/$BACKUP_DIR/.env" "./.env"
    echo "✅ 环境配置恢复完成"
else
    echo "⚠️ 未找到环境配置文件"
fi

# 清理临时文件
rm -rf "$TEMP_DIR"

# 启动服务
echo "启动服务..."
docker-compose up -d

# 等待服务启动
echo "等待服务启动..."
sleep 30

# 健康检查
echo "执行健康检查..."
for i in {1..10}; do
    if curl -f http://localhost:8000/api/health &> /dev/null; then
        echo "✅ 服务恢复成功"
        break
    else
        echo "⏳ 等待服务启动... ($i/10)"
        sleep 10
    fi
done

echo "🎉 恢复完成!"
```

### 7.4 监控和日志

#### 7.4.1 应用监控

```python
# backend/app/monitoring.py
import time
import psutil
from functools import wraps
from fastapi import Request
import logging

logger = logging.getLogger(__name__)

# 性能监控装饰器
def monitor_performance(func):
    @wraps(func)
    async def wrapper(*args, **kwargs):
        start_time = time.time()
        try:
            result = await func(*args, **kwargs)
            execution_time = time.time() - start_time
            logger.info(f"{func.__name__} 执行时间: {execution_time:.2f}s")
            return result
        except Exception as e:
            execution_time = time.time() - start_time
            logger.error(f"{func.__name__} 执行失败 (耗时: {execution_time:.2f}s): {e}")
            raise
    return wrapper

# 系统资源监控
def get_system_stats():
    """获取系统资源使用情况"""
    try:
        cpu_percent = psutil.cpu_percent(interval=1)
        memory = psutil.virtual_memory()
        disk = psutil.disk_usage('/')

        return {
            "cpu_percent": cpu_percent,
            "memory_percent": memory.percent,
            "memory_used_gb": memory.used / (1024**3),
            "memory_total_gb": memory.total / (1024**3),
            "disk_percent": disk.percent,
            "disk_used_gb": disk.used / (1024**3),
            "disk_total_gb": disk.total / (1024**3),
        }
    except Exception as e:
        logger.error(f"获取系统统计信息失败: {e}")
        return {}

# 请求监控中间件
async def log_requests(request: Request, call_next):
    start_time = time.time()

    # 记录请求信息
    logger.info(f"请求: {request.method} {request.url}")

    try:
        response = await call_next(request)
        process_time = time.time() - start_time

        # 记录响应信息
        logger.info(f"响应: {response.status_code} (耗时: {process_time:.2f}s)")

        # 添加响应头
        response.headers["X-Process-Time"] = str(process_time)

        return response
    except Exception as e:
        process_time = time.time() - start_time
        logger.error(f"请求处理失败 (耗时: {process_time:.2f}s): {e}")
        raise
```

#### 7.4.2 日志配置

```python
# backend/app/logging_config.py
import logging
import logging.config
import os
from pathlib import Path

def setup_logging():
    """配置应用日志"""
    # 确保日志目录存在
    log_dir = Path("./logs")
    log_dir.mkdir(exist_ok=True)

    # 日志配置
    config = {
        "version": 1,
        "disable_existing_loggers": False,
        "formatters": {
            "default": {
                "format": "%(asctime)s - %(name)s - %(levelname)s - %(message)s",
                "datefmt": "%Y-%m-%d %H:%M:%S"
            },
            "detailed": {
                "format": "%(asctime)s - %(name)s - %(levelname)s - %(module)s:%(lineno)d - %(message)s",
                "datefmt": "%Y-%m-%d %H:%M:%S"
            },
            "json": {
                "()": "pythonjsonlogger.jsonlogger.JsonFormatter",
                "format": "%(asctime)s %(name)s %(levelname)s %(module)s %(lineno)d %(message)s"
            }
        },
        "handlers": {
            "console": {
                "class": "logging.StreamHandler",
                "level": os.getenv("LOG_LEVEL", "INFO"),
                "formatter": "default",
                "stream": "ext://sys.stdout"
            },
            "file": {
                "class": "logging.handlers.RotatingFileHandler",
                "level": os.getenv("LOG_LEVEL", "INFO"),
                "formatter": "detailed",
                "filename": os.getenv("LOG_FILE", "./logs/app.log"),
                "maxBytes": 10485760,  # 10MB
                "backupCount": 5,
                "encoding": "utf8"
            },
            "file_json": {
                "class": "logging.handlers.RotatingFileHandler",
                "level": os.getenv("LOG_LEVEL", "INFO"),
                "formatter": "json",
                "filename": "./logs/app.json",
                "maxBytes": 10485760,  # 10MB
                "backupCount": 5,
                "encoding": "utf8"
            },
            "error_file": {
                "class": "logging.handlers.RotatingFileHandler",
                "level": "ERROR",
                "formatter": "detailed",
                "filename": "./logs/error.log",
                "maxBytes": 10485760,  # 10MB
                "backupCount": 5,
                "encoding": "utf8"
            }
        },
        "loggers": {
            "": {  # root logger
                "level": os.getenv("LOG_LEVEL", "INFO"),
                "handlers": ["console", "file", "error_file"]
            },
            "uvicorn": {
                "level": "INFO",
                "handlers": ["console", "file"],
                "propagate": False
            },
            "sqlalchemy.engine": {
                "level": "WARNING",
                "handlers": ["file"],
                "propagate": False
            },
            "fastapi": {
                "level": "INFO",
                "handlers": ["file"],
                "propagate": False
            }
        }
    }

    # 应用配置
    logging.config.dictConfig(config)
```

---

## 8. 故障排除

### 8.1 常见错误码

| 错误码 | 含义 | 常见原因 | 解决方案 |
|--------|------|----------|----------|
| 400 Bad Request | 请求参数错误 | 请求格式不正确、参数缺失 | 检查请求参数格式和必填字段 |
| 401 Unauthorized | 认证失败 | Token 无效或已过期、用户名密码错误 | 重新登录获取新 Token |
| 403 Forbidden | 权限不足 | 非管理员用户访问管理员接口 | 使用管理员账号登录 |
| 404 Not Found | 资源不存在 | 插件、版本或备份不存在 | 检查资源标识符是否正确 |
| 409 Conflict | 资源冲突 | 插件版本已存在 | 使用不同的版本号或删除现有版本 |
| 413 Request Entity Too Large | 文件过大 | 上传文件超过 100MB 限制 | 压缩文件或增加大小限制 |
| 422 Unprocessable Entity | 参数验证失败 | 参数格式或值不符合要求 | 检查参数类型和格式 |
| 500 Internal Server Error | 服务器内部错误 | 数据库连接失败、文件系统错误 | 检查服务器日志和配置 |

### 8.2 插件上传问题

#### 8.2.1 plugin.json 格式错误

**错误信息**: `文件验证失败：plugin.json 缺失或格式错误`

**排查步骤**:
1. 检查 ZIP 包根目录是否包含 `plugin.json`
2. 验证 JSON 格式是否正确
3. 确认必需字段是否完整

**正确格式示例**:
```json
{
  "name": "com.example.myplugin",
  "version": "1.0.0",
  "main": "MyPlugin.dll",
  "entryClass": "MyPlugin.PluginEntry"
}
```

#### 8.2.2 文件大小超限

**错误信息**: `文件大小超过 100MB 限制`

**解决方案**:
1. 压缩插件文件
2. 移除不必要的资源文件
3. 如需更大的文件，修改 `MAX_UPLOAD_SIZE` 配置

#### 8.2.3 版本冲突

**错误信息**: `插件版本 1.0.0 已存在`

**解决方案**:
1. 使用新的版本号（遵循语义化版本规范）
2. 或者先删除现有版本再上传

### 8.3 认证问题

#### 8.3.1 Token 过期

**错误信息**: `令牌无效或已过期`

**解决方案**:
1. 重新调用 `/api/auth/login` 获取新 Token
2. 检查客户端 Token 存储机制
3. 考虑实现 Token 自动刷新机制

#### 8.3.2 权限不足

**错误信息**: `需要管理员权限`

**解决方案**:
1. 使用管理员账号登录
2. 检查请求头中是否正确携带 Authorization
3. 确认 Token 格式为 `Bearer <token>`

### 8.4 数据库问题

#### 8.4.1 数据库连接失败

**错误信息**: `数据库连接失败`

**排查步骤**:
1. 检查数据库文件是否存在
2. 确认文件权限是否正确
3. 验证数据库 URL 配置

**解决方案**:
```bash
# 创建数据库目录
mkdir -p ./data

# 检查权限
chmod 755 ./data
```

#### 8.4.2 数据库锁定

**错误信息**: `数据库文件被锁定`

**解决方案**:
1. 重启应用服务
2. 检查是否有其他进程占用数据库
3. 确认数据库文件完整性

### 8.5 文件系统问题

#### 8.5.1 文件路径错误

**错误信息**: `文件不存在或路径错误`

**排查步骤**:
1. 检查 UPLOAD_DIR 和 BACKUP_DIR 配置
2. 确认目录权限
3. 验证文件是否存在

#### 8.5.2 磁盘空间不足

**错误信息**: `磁盘空间不足`

**解决方案**:
1. 清理临时文件和旧版本
2. 扩展磁盘空间
3. 实现自动清理机制

### 8.6 性能问题

#### 8.6.1 响应时间过长

**排查步骤**:
1. 检查服务器资源使用情况
2. 分析慢查询日志
3. 检查网络连接

**优化建议**:
1. 添加数据库索引
2. 实现分页查询
3. 使用缓存机制

#### 8.6.2 内存占用过高

**排查步骤**:
1. 监控内存使用情况
2. 检查内存泄漏
3. 分析大文件处理

**解决方案**:
1. 优化文件读取方式
2. 使用流式处理
3. 实现内存监控和自动回收

### 8.7 日志分析

#### 8.7.1 启用详细日志

```python
# 在配置文件中设置
LOG_LEVEL = "DEBUG"

# 或在代码中动态设置
import logging
logging.getLogger().setLevel(logging.DEBUG)
```

#### 8.7.2 常用日志命令

```bash
# 查看应用日志
tail -f ./logs/app.log

# 查看错误日志
tail -f ./logs/error.log

# 搜索特定错误
grep "ERROR" ./logs/app.log

# 分析 API 请求
grep "请求:" ./logs/app.log | tail -20
```

#### 8.7.3 Docker 日志

```bash
# 查看容器日志
docker logs microdock-backend

# 实时查看日志
docker logs -f microdock-backend

# 查看最近的日志
docker logs --tail 100 microdock-backend
```

### 8.8 调试技巧

#### 8.8.1 API 调试

```bash
# 使用 curl 测试 API
curl -X POST http://localhost:8000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "admin"}' \
  -v

# 测试文件上传
curl -X POST http://localhost:8000/api/plugins/upload \
  -F "file=@myplugin.zip" \
  -F "plugin_key=my_secret_key" \
  -v
```

#### 8.8.2 数据库调试

```python
# 检查数据库连接
async def test_database():
    try:
        async with get_db() as db:
            result = await db.execute("SELECT 1")
            print("数据库连接正常")
    except Exception as e:
        print(f"数据库连接失败: {e}")

# 检查表结构
async def check_tables():
    async with get_db() as db:
        result = await db.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = result.fetchall()
        print("数据库表:", [table[0] for table in tables])
```

---

## 9. 更新日志

### 版本 2.0.0 (2023-12-01)

#### 新增功能
- ✨ 完整的 JWT 认证系统
- ✨ 插件多版本管理
- ✨ 用户备份管理功能
- ✨ 管理员权限控制
- ✨ 文件完整性校验 (SHA256)
- ✨ API 文档生成系统

#### 改进
- 🔧 优化 API 响应格式
- 🔧 增强文件上传安全性
- 🔧 改进错误处理机制
- 🔧 添加详细的日志记录
- 🔧 支持大文件上传 (100MB)

#### 安全修复
- 🔒 修复文件路径遍历漏洞
- 🔒 加强输入参数验证
- 🔒 实现文件类型检查
- 🔒 添加速率限制准备

#### API 变更
- 🔄 `POST /api/auth/login` - 新增管理员登录
- 🔄 `POST /api/auth/logout` - 新增管理员登出
- 🔄 `GET /api/auth/me` - 新增认证状态检查
- 🔄 `POST /api/backups/*` - 新增备份管理 API
- 🔄 `POST /api/plugins/version/*` - 新增版本管理 API

#### 配置变更
- ⚙️ 新增 JWT 认证配置项
- ⚙️ 新增文件存储路径配置
- ⚙️ 新增 CORS 跨域配置
- ⚙️ 新增日志级别配置

### 版本 1.0.0 (2023-11-01)

#### 初始发布
- 🎉 基础插件上传/下载功能
- 🎉 简单的插件列表查看
- 🎉 基础的文件管理
- 🎉 SQLite 数据存储

#### 已知限制
- ❌ 无用户认证系统
- ❌ 无版本管理功能
- ❌ 无备份管理
- ❌ 安全性较弱

---

## 附录

### A. API 快速参考

| 端点 | 方法 | 描述 | 权限 |
|------|------|------|------|
| `/api/auth/login` | POST | 管理员登录 | 公开 |
| `/api/auth/logout` | POST | 管理员登出 | 管理员 |
| `/api/auth/me` | GET | 获取认证状态 | 可选认证 |
| `/api/plugins/list` | GET | 获取插件列表 | 公开 |
| `/api/plugins/detail` | POST | 获取插件详情 | 公开 |
| `/api/plugins/upload` | POST | 上传插件 | 公开 (需 plugin_key) |
| `/api/plugins/enable` | POST | 启用插件 | 管理员 |
| `/api/plugins/disable` | POST | 禁用插件 | 管理员 |
| `/api/plugins/deprecate` | POST | 标记插件过时 | 管理员 |
| `/api/plugins/delete` | POST | 删除插件 | 管理员 |
| `/api/plugins/download` | POST | 下载插件 | 公开 |
| `/api/plugins/versions` | POST | 获取版本列表 | 公开 |
| `/api/plugins/version/detail` | POST | 获取版本详情 | 公开 |
| `/api/plugins/version/deprecate` | POST | 标记版本过时 | 管理员 |
| `/api/plugins/version/download` | POST | 下载指定版本 | 公开 |
| `/api/backups/upload` | POST | 上传备份 | 公开 (需 user_key) |
| `/api/backups/list` | POST | 获取用户备份列表 | 公开 |
| `/api/backups/list-all` | GET | 获取所有备份列表 | 管理员 |
| `/api/backups/download` | POST | 下载备份 | 公开 |
| `/api/backups/delete` | POST | 删除备份 | 管理员 |
| `/api/health` | GET | 健康检查 | 公开 |
| `/` | GET | 服务器信息 | 公开 |

### B. HTTP 状态码说明

| 状态码 | 含义 | 说明 |
|--------|------|------|
| 200 OK | 请求成功 | 请求处理成功 |
| 400 Bad Request | 请求错误 | 参数格式错误或验证失败 |
| 401 Unauthorized | 认证失败 | Token 无效或过期 |
| 403 Forbidden | 权限不足 | 缺少管理员权限 |
| 404 Not Found | 资源不存在 | 插件、版本或备份未找到 |
| 409 Conflict | 资源冲突 | 版本已存在 |
| 413 Request Entity Too Large | 文件过大 | 上传文件超过限制 |
| 422 Unprocessable Entity | 参数验证失败 | 参数类型或格式错误 |
| 500 Internal Server Error | 服务器错误 | 内部处理错误 |

### C. 文件格式规范

#### C.1 plugin.json 规范

```json
{
  "name": "com.example.myplugin",           // 必需: 唯一标识符
  "version": "1.0.0",                        // 必需: 语义化版本号
  "main": "MyPlugin.dll",                   // 必需: 主 DLL 文件名
  "entryClass": "MyPlugin.PluginEntry",     // 必需: 入口类完全限定名
  "displayName": "我的插件",                  // 可选: 显示名称
  "description": "插件描述",                  // 可选: 描述
  "author": "作者名",                         // 可选: 作者
  "license": "MIT",                          // 可选: 许可证
  "homepage": "https://example.com",         // 可选: 主页
  "changelog": "更新内容",                    // 可选: 更新日志
  "dependencies": {                          // 可选: 依赖关系
    "com.other.plugin": ">=1.0.0"
  },
  "engines": {                              // 可选: 引擎要求
    "MicroDock": ">=2.0.0"
  }
}
```

#### C.2 语义化版本规范

版本号格式：`MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]`

- `MAJOR`: 主版本号，不兼容的 API 修改
- `MINOR`: 次版本号，向下兼容的功能性新增
- `PATCH`: 修订号，向下兼容的问题修正
- `PRERELEASE`: 预发布版本标识 (alpha, beta, rc)
- `BUILD`: 构建信息

**示例**:
- `1.0.0` - 正式版本
- `1.1.0` - 功能更新
- `1.1.1` - 错误修复
- `2.0.0-alpha.1` - 预发布版本

### D. 安全检查清单

#### D.1 部署前检查

- [ ] 修改默认管理员密码
- [ ] 更改 JWT 密钥为强随机字符串
- [ ] 配置正确的 CORS 源
- [ ] 启用 HTTPS (生产环境)
- [ ] 配置文件权限
- [ ] 设置适当的日志级别
- [ ] 验证文件上传限制
- [ ] 检查数据库备份策略

#### D.2 运行时监控

- [ ] 监控磁盘空间使用情况
- [ ] 检查异常登录尝试
- [ ] 监控 API 调用频率
- [ ] 定期检查错误日志
- [ ] 验证文件完整性
- [ ] 监控系统资源使用
- [ ] 检查备份任务执行情况

---

**文档版本**: 2.0.0
**最后更新**: 2023-12-01
**维护团队**: MicroDock 开发团队

如有问题或建议，请联系: support@microdock.com