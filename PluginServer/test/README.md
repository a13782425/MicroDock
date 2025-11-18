# 测试插件文件

这个目录包含用于测试插件上传和解析功能的示例文件。

## 文件说明

- `plugin.json` - 标准的插件配置文件示例
- `test-plugin.zip` - 完整的测试插件ZIP包（需手动创建）

## 创建测试ZIP包

你可以使用以下命令创建测试插件ZIP包：

```bash
# 在test目录中创建一个模拟的插件文件
echo "这是一个模拟的插件DLL文件" > TestPlugin.dll

# 创建ZIP包
zip test-plugin.zip plugin.json TestPlugin.dll
```

## plugin.json 格式说明

```json
{
  "name": "插件唯一标识符",
  "displayName": "插件显示名称",
  "version": "版本号",
  "description": "插件描述",
  "author": "作者名称",
  "license": "许可证",
  "homepage": "主页URL",
  "keywords": ["关键词数组"],
  "entry": "入口文件",
  "dependencies": ["依赖列表"],
  "minAppVersion": "最低应用版本",
  "category": "插件分类"
}
```

### 必需字段
- `name`: 插件的唯一标识符
- `displayName`: 用户看到的插件名称
- `version`: 插件版本号

### 可选字段
- `description`: 插件功能描述
- `author`: 作者信息
- `license`: 许可证类型
- `homepage`: 项目主页
- `keywords`: 关键词标签
- `entry`: 插件入口文件
- `dependencies`: 依赖的其他插件
- `minAppVersion`: 最低支持的应用版本
- `category`: 插件分类

## 测试用例

### 1. 正常情况测试
- 包含完整plugin.json的ZIP文件
- 验证元数据是否正确提取

### 2. 错误情况测试
- 缺少plugin.json的ZIP文件
- plugin.json格式错误
- 缺少必需字段

### 3. 边界情况测试
- 大文件上传（>50MB）
- 空ZIP文件
- 密码保护的ZIP文件