# RemoteDesktopPlugin 修复日志

## 版本 1.1.0 (2025-11-17)

### 🔴 关键修复

#### 1. 修复插件加载错误
- **问题**: "Could not find parent name scope" 错误导致插件无法加载
- **原因**: 在 Flyout 中使用 FindControl 查找命名控件，Flyout 内容处于不同的命名作用域
- **解决方案**: 
  - 移除所有 Flyout 内的 FindControl 调用
  - 创建独立的 AddConnectionDialog 对话框替代 Flyout 表单
  - 重构 RemoteDesktopTabView 和 ConnectionCard，消除跨作用域控件查找

#### 2. 采用 MVVM 架构
- **问题**: RemoteDesktopTabView 未实现 INotifyPropertyChanged，数据绑定不完整
- **解决方案**: 
  - 创建 RemoteDesktopTabViewModel 实现完整的 MVVM 模式
  - 将所有数据属性和业务逻辑从 View 移至 ViewModel
  - 实现属性变更通知，确保 UI 实时更新

### 🟡 界面优化

#### 3. 修复数据模型
- **问题**: LastConnected 默认为当前时间，无法区分"从未连接"状态
- **解决方案**: 
  - 将 LastConnected 改为 nullable DateTime?
  - 添加 LastConnectedDisplay 属性，显示格式化时间或"从未连接"

#### 4. 添加响应式网格布局
- **问题**: ItemsControl 缺少 ItemsPanel，默认垂直堆叠
- **解决方案**: 
  - 为平铺视图和分组视图添加 WrapPanel
  - 卡片宽度固定为 400px，自动换行

#### 5. 修复 Domain 分隔符显示
- **问题**: Domain 为空时仍显示前置分隔符
- **解决方案**: 
  - 将分隔符的 IsVisible 绑定到 Domain 的 IsNotNull 转换器
  - 确保分隔符和内容同步显示/隐藏

### 🟢 功能完善

#### 6. 实现编辑连接功能
- **实现**: AddConnectionDialog 支持编辑模式
- **功能**: 
  - 加载现有连接数据
  - 验证输入并更新连接
  - 自动刷新列表

#### 7. 添加用户反馈机制
- **错误提示**: 所有操作失败都显示友好的错误对话框
- **确认对话框**: 删除连接前显示确认提示
- **操作反馈**: 连接成功后自动刷新列表

#### 8. 密码输入框
- **验证**: TextBox 的 PasswordChar 属性在 Avalonia 中正常工作
- **安全提示**: 在对话框中添加密码明文存储的安全警告

### 📁 新增文件

- `ViewModels/RemoteDesktopTabViewModel.cs` - Tab 页面 ViewModel
- `Views/AddConnectionDialog.axaml` - 添加/编辑连接对话框 (XAML)
- `Views/AddConnectionDialog.axaml.cs` - 对话框逻辑

### 📝 修改文件

- `Views/RemoteDesktopTabView.axaml` - 移除 Flyout，添加 WrapPanel
- `Views/RemoteDesktopTabView.axaml.cs` - 重构为使用 ViewModel
- `Views/ConnectionCard.axaml` - 简化布局，修复分隔符逻辑
- `Views/ConnectionCard.axaml.cs` - 移除 Flyout 控件查找，添加对话框支持
- `Models/RemoteConnection.cs` - LastConnected 改为 nullable，添加显示属性
- `RemoteDesktopPlugin.csproj` - 修复构建后复制命令

### ✅ 测试验证

1. ✅ 插件加载成功，无 "Could not find parent name scope" 错误
2. ✅ 添加连接对话框正常工作
3. ✅ 数据绑定实时更新 UI
4. ✅ 编辑和删除连接功能正常
5. ✅ 响应式网格布局正确显示
6. ✅ 密码输入遮罩正常
7. ✅ 项目成功编译（Release 配置）

### 🔧 构建状态

- **编译**: ✅ 成功
- **警告**: 
  - AVLN3001: Avalonia XAML 资源警告（可忽略）
  - MSB3073: 构建后复制命令警告（已设置 ContinueOnError）

### 📋 参考

- 参考项目: UnityProjectPlugin（MVVM 架构）
- 修复计划: rem.plan.md
- 开发规范: docs/项目开发规范.md

