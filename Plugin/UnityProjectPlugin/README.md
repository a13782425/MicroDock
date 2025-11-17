# Unity 项目管理插件

## 功能概述

UnityProjectPlugin 是 MicroDock 的一个插件，用于管理和快速打开 Unity 项目。支持分组管理、快速搜索、一键打开等功能。

## 主要功能

### 1. 项目管理
- ✅ 添加 Unity 项目到列表
- ✅ 表格化展示项目信息（项目名、分组、Unity版本、最后打开时间）
- ✅ 搜索过滤项目（支持按项目名或分组名搜索）
- ✅ 一键打开项目（使用匹配的 Unity 版本）
- ✅ 编辑项目（修改项目名称、设置分组）
- ✅ 删除项目记录

### 2. 分组管理 🆕
- ✅ 创建自定义分组
- ✅ 编辑分组名称（自动更新关联项目）
- ✅ 删除分组（检查是否被使用）
- ✅ 项目编辑时可选择或输入新分组
- ✅ 显示每个分组的项目数量

### 3. Unity 版本管理
- ✅ 添加 Unity Editor 版本
- ✅ 管理多个 Unity 版本
- ✅ 自动从路径提取版本号
- ✅ 删除不需要的版本
- ✅ 打开项目时自动匹配对应版本

### 4. 工具方法
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
2. 点击顶部"添加项目"按钮
3. 选择 Unity 项目文件夹（包含 Assets 和 ProjectSettings 的文件夹）
4. 项目将添加到表格中，并自动识别 Unity 版本

### 编辑项目 🆕

1. 找到要编辑的项目
2. 点击操作列的下拉箭头，选择"编辑"
3. 在弹出的编辑窗口中：
   - 修改项目名称
   - 选择已有分组或输入新分组名称
4. 点击"保存"完成编辑

### 打开项目

**方式一：直接打开**
- 点击项目行的"打开"按钮

**打开逻辑**：
- 插件会自动匹配项目的 Unity 版本
- 如果找不到匹配版本，使用配置的第一个 Unity 版本
- 项目打开后会自动更新"最后打开时间"

### 删除项目

1. 找到要删除的项目
2. 点击操作列的下拉箭头，选择"删除"
3. 注意：这只删除插件中的记录，不会删除实际项目文件

### 管理分组 🆕

1. 进入 MicroDock 的"设置"页面
2. 找到"Unity 项目管理"插件设置区域
3. 在"分组管理"部分可以：
   - **添加分组**：点击"添加分组"按钮，输入分组名称
   - **编辑分组**：点击分组旁的"编辑"按钮，修改名称
   - **删除分组**：点击分组旁的"删除"按钮（只能删除未使用的分组）

### 搜索项目

在顶部搜索框中输入关键字：
- 支持按项目名称搜索
- 支持按分组名称搜索
- 搜索不区分大小写，实时过滤

## 数据存储 🆕

插件数据存储在 JSON 文件中，位于插件数据文件夹：
- `projects.json` - 存储所有项目信息（名称、路径、分组、Unity版本、最后打开时间）
- `groups.json` - 存储分组信息（分组名、创建时间）
- `versions.json` - 存储 Unity 版本配置（版本号、Editor路径）

**数据迁移**：如果检测到旧的数据库存储，插件会自动迁移到新的 JSON 文件格式。

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

### v2.0.0 (2025-11-14) 🎉
- ✅ **重大重构**：完全重构UI为表格列表布局
- ✅ **分组功能**：支持项目分组管理
- ✅ **编辑功能**：支持编辑项目名称和分组
- ✅ **搜索增强**：支持按项目名和分组名搜索
- ✅ **数据迁移**：从数据库存储迁移到 JSON 文件
- ✅ **UI 优化**：使用 SplitButton 整合操作菜单
- ✅ **设置整合**：Unity版本管理和分组管理统一界面

### v1.0.0 (2025-11-13)
- ✅ 初始版本
- ✅ 项目列表管理
- ✅ Unity 版本管理
- ✅ 打开项目功能
- ✅ 判断项目有效性工具

## 已知限制

1. 不支持切换项目使用的 Unity 版本（项目版本从 ProjectVersion.txt 自动读取）
2. 删除分组时需要先将使用该分组的项目移到其他分组
3. 删除项目和分组操作暂无确认对话框（规划中）

## 未来计划

- [ ] 项目排序功能（按名称、最后打开时间等排序）
- [ ] 项目统计信息（大小、文件数等）
- [ ] Unity 版本自动扫描（从常见安装路径）
- [ ] 项目备注功能
- [ ] 确认对话框（删除操作）
- [ ] 项目导入/导出（备份配置）

## 许可证

MIT License

## 作者

MicroDock Team

