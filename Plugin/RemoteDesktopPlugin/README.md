# RemoteDesktopPlugin - 远程桌面管理插件

## 功能特性

### 核心功能
- ✅ **连接管理**: 添加、编辑、删除远程桌面连接
- ✅ **分组支持**: 支持连接分组管理，方便分类管理
- ✅ **一键连接**: 点击即可启动远程桌面连接
- ✅ **搜索功能**: 支持按连接名或分组名搜索
- ✅ **分组视图**: 可切换分组视图或平铺视图
- ✅ **RDP文件生成**: 自动生成标准RDP配置文件
- ✅ **连接历史**: 记录最后连接时间

### 数据管理
- ✅ **JSON存储**: 使用JSON文件存储连接和分组数据
- ✅ **自动保存**: 连接信息变更后自动保存
- ✅ **数据验证**: 完善的输入验证和错误处理

### 用户体验
- ✅ **现代UI**: 基于FluentAvaloniaUI的现代化界面
- ✅ **响应式设计**: 支持最小360×360窗口尺寸
- ✅ **空状态提示**: 友好的空状态提示界面
- ✅ **工具栏**: 便捷的搜索和添加按钮
- ✅ **连接卡片**: 直观显示连接信息和操作

## 技术架构

### 项目结构
```
RemoteDesktopPlugin/
├── RemoteDesktopPlugin.cs          # 插件主类
├── Models/
│   ├── RemoteConnection.cs         # 连接数据模型
│   └── ProjectGroup.cs             # 分组数据模型
├── Views/
│   ├── RemoteDesktopTabView.axaml  # 主标签页UI
│   ├── RemoteDesktopTabView.axaml.cs
│   ├── ConnectionCard.axaml        # 连接卡片UI
│   └── ConnectionCard.axaml.cs
├── Assets/                         # 资源文件目录
├── plugin.json                     # 插件配置文件
└── RemoteDesktopPlugin.csproj      # 项目文件
```

### 核心类
- **RemoteDesktopPlugin**: 插件主类，继承自BaseMicroDockPlugin
- **RemoteConnection**: 远程连接数据模型
- **ProjectGroup**: 分组数据模型
- **RemoteDesktopTabView**: 主标签页视图
- **ConnectionCard**: 连接卡片组件

### 数据模型

#### RemoteConnection
- `Id`: 唯一标识符
- `Name`: 连接名称
- `Host`: 主机地址
- `Port`: 端口号（默认3389）
- `Username`: 用户名
- `Password`: 密码（明文存储，建议加密）
- `Domain`: 域名（可选）
- `GroupName`: 分组名称（可选）
- `Description`: 描述信息（可选）
- `LastConnected`: 最后连接时间
- `CreatedAt`: 创建时间

#### ProjectGroup
- `Id`: 唯一标识符
- `Name`: 分组名称
- `CreatedAt`: 创建时间

## 使用方法

### 添加连接
1. 点击"添加连接"按钮
2. 在弹出的表单中填写连接信息：
   - 连接名称：用于标识连接
   - 主机地址：远程桌面地址
   - 端口：RDP端口（默认3389）
   - 用户名：登录用户名
   - 密码：登录密码
   - 域名：可选，用于域用户登录
   - 分组：可选，用于分类管理
   - 描述：可选，添加备注信息
3. 点击"添加"完成添加

### 连接远程桌面
1. 在连接列表中找到目标连接
2. 点击"连接"按钮
3. 系统会自动生成RDP文件并启动远程桌面连接

### 管理分组
1. 点击连接卡片上的分组标签
2. 可以：
   - 选择已有分组
   - 创建新分组
   - 删除未使用的分组

### 搜索连接
在搜索框中输入关键词，可按连接名或分组名进行搜索。

### 切换视图
点击"分组"按钮可切换分组视图和平铺视图。

## 开发信息

### 依赖
- .NET 8.0
- Avalonia 11.3.8
- FluentAvaloniaUI 2.4.0
- System.Text.Json 8.0.5
- MicroDock.Plugin (项目引用)

### 编译要求
- Visual Studio 2022 或 JetBrains Rider
- .NET 8.0 SDK

### 构建命令
```bash
dotnet build -c Release
```

### 部署说明
插件编译后需要复制到 MicroDock 的 Plugins/RemoteDesktopPlugin 目录。

## 安全注意事项

⚠️ **当前实现中的安全风险**：
- 密码以明文形式存储在JSON文件中
- 建议在生产环境中实现密码加密存储

## 未来改进

### 待实现功能
- [ ] 密码加密存储
- [x] 编辑连接功能
- [ ] 连接状态检测
- [ ] 批量连接操作
- [ ] 连接导出/导入
- [ ] 快速连接模板

### 性能优化
- [ ] 图标缓存
- [x] 响应式网格布局
- [ ] 延迟加载

## 版本历史

### v1.1.0 (2025-11-17)
- ✅ **重大修复**: 解决 "Could not find parent name scope" 加载错误
- ✅ 采用 MVVM 架构，提升代码质量和可维护性
- ✅ 实现编辑连接功能
- ✅ 添加用户反馈机制（错误提示、确认对话框）
- ✅ 修复数据模型（LastConnected 支持"从未连接"状态）
- ✅ 添加响应式网格布局
- ✅ 修复 Domain 分隔符显示逻辑
- ✅ 创建独立的添加/编辑连接对话框

### v1.0.0 (2025-11-17)
- ✅ 初始版本发布
- ✅ 实现基本的连接管理功能
- ✅ 实现分组管理功能
- ✅ 实现UI界面
- ✅ 支持搜索和视图切换
- ✅ 支持RDP文件生成

## 许可证

MIT License

## 作者

MicroDock Team
