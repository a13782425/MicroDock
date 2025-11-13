# Unity 项目管理插件

## 功能概述

UnityProjectPlugin 是 MicroDock 的一个插件，用于管理和快速打开 Unity 项目。

## 主要功能

### 1. 项目管理
- ✅ 添加 Unity 项目到列表
- ✅ 浏览项目详细信息（名称、路径、Unity 版本、最后打开时间）
- ✅ 搜索过滤项目
- ✅ 双击或按钮打开项目
- ✅ 删除项目记录

### 2. Unity 版本管理
- ✅ 添加 Unity Editor 版本
- ✅ 管理多个 Unity 版本
- ✅ 自动从路径提取版本号
- ✅ 删除不需要的版本

### 3. 工具方法
- ✅ `unity.is_project` - 判断文件夹是否为 Unity 项目
- ✅ `unity.open_project` - 打开 Unity 项目

## 使用方法

### 添加 Unity 版本（首次使用必须）

1. 打开 MicroDock 主应用
2. 进入"设置"页面
3. 找到"Unity 项目管理"插件设置区域
4. 点击"添加版本"按钮
5. 选择 Unity Editor 可执行文件（通常位于 `C:\Program Files\Unity\Hub\Editor\版本号\Editor\Unity.exe`）
6. 版本将自动从 Unity.exe 的产品版本信息中识别（如 `2021.3.39f1_fb3b7b32f191` 会提取为 `2021.3.39f1`）

### 添加项目

1. 在 MicroDock 中找到"Unity 项目"标签页
2. 点击"添加项目"按钮
3. 选择 Unity 项目文件夹（包含 Assets 和 ProjectSettings 的文件夹）
4. 项目将添加到列表中，并自动识别 Unity 版本

### 打开项目

**方式一：双击打开**
- 在项目列表中双击项目

**方式二：选择后打开**
1. 在项目列表中选择一个项目
2. 查看右侧详情面板
3. 点击"打开项目"按钮

**打开逻辑**：
- 插件会自动匹配项目的 Unity 版本
- 如果找不到匹配版本，使用配置的第一个 Unity 版本
- 项目打开后会更新"最后打开时间"

### 删除项目

1. 在项目列表中选择要删除的项目
2. 点击"删除项目"按钮
3. 注意：这只删除插件中的记录，不会删除实际项目文件

## 数据存储

- 项目列表存储在 MicroDock 数据库中（键：`projects`）
- Unity 版本列表存储在 MicroDock 数据库中（键：`unity_versions`）
- 数据格式：JSON

## 工具方法使用

### unity.is_project

判断指定文件夹是否是 Unity 项目。

**参数**：
- `path` (string): 项目文件夹路径

**返回值**：JSON 字符串
```json
{
  "success": true,
  "isProject": true,
  "message": "是 Unity 项目"
}
```

### unity.open_project

使用指定版本的 Unity 打开项目。

**参数**：
- `projectPath` (string): 项目路径
- `editorPath` (string, 可选): Unity Editor 路径

**返回值**：JSON 字符串
```json
{
  "success": true,
  "message": "Unity 项目已打开"
}
```

## 技术细节

- **框架**: .NET 8.0
- **UI**: Avalonia 11.3.7
- **数据序列化**: System.Text.Json
- **插件接口**: MicroDock.Plugin

## 文件结构

```
UnityProjectPlugin/
├── plugin.json                    # 插件配置
├── UnityProjectPlugin.cs          # 主插件类
├── Models/
│   ├── UnityProject.cs           # 项目数据模型
│   └── UnityVersion.cs           # 版本数据模型
├── ViewModels/
│   ├── UnityProjectTabViewModel.cs
│   └── UnityVersionSettingsViewModel.cs
└── Views/
    ├── UnityProjectTabView.axaml
    ├── UnityProjectTabView.axaml.cs
    ├── UnityVersionSettingsView.axaml
    └── UnityVersionSettingsView.axaml.cs
```

## 版本历史

### v1.0.0 (2025-11-13)
- ✅ 初始版本
- ✅ 项目列表管理
- ✅ Unity 版本管理
- ✅ 打开项目功能
- ✅ 判断项目有效性工具

## 已知限制

1. 当前不支持编辑项目信息
2. 不支持切换项目使用的 Unity 版本（项目版本从 ProjectVersion.txt 自动读取）
3. 不支持修改项目平台配置

## 未来计划

- [ ] 支持项目信息编辑
- [ ] 项目最近打开历史
- [ ] 项目标签/分组功能
- [ ] 项目统计信息（大小、文件数等）
- [ ] Unity 版本自动扫描
- [ ] 项目备注功能

## 许可证

MIT License

## 作者

MicroDock Team

