# AI JSON 响应格式指南

## 概述

AIChatPlugin 现在支持 JSON 结构化响应，让 AI 返回的内容更可靠、更易解析。

## JSON 格式

### 基本结构

```json
{
  "think": "思考过程（可选）",
  "content": "主要回复内容，支持 Markdown 和占位符",
  "attachments": {
    "id1": {
      "type": "mermaid",
      "content": "图表代码"
    },
    "id2": {
      "type": "code",
      "language": "python",
      "content": "代码内容"
    }
  }
}
```

### 字段说明

- **think** (可选): 思考过程，会显示在折叠的"思考过程"区域
- **content** (必需): 主要回复内容，支持 Markdown 格式和占位符
- **attachments** (可选): 附件字典，包含图表、代码等

## 占位符语法

在 `content` 中使用占位符引用附件：

- `{{mermaid:id}}`: 引用 Mermaid 图表
- `{{code:id}}`: 引用代码块
- `{{latex:id}}`: 引用 LaTeX 公式
- `{{image:id}}`: 引用图片

## 支持的附件类型

### 1. Mermaid 图表

```json
{
  "type": "mermaid",
  "content": "flowchart TD\n    A[开始] --> B[结束]"
}
```

### 2. 代码块

```json
{
  "type": "code",
  "language": "python",
  "content": "def hello():\n    print('Hello')"
}
```

### 3. LaTeX 公式

```json
{
  "type": "latex",
  "content": "E = mc^2"
}
```

### 4. 图片

```json
{
  "type": "image",
  "content": "https://example.com/image.png",
  "title": "示例图片"
}
```

## 完整示例

### 示例 1：纯文本回复

```json
{
  "content": "这是一个简单的回复，不需要图表或代码。"
}
```

### 示例 2：带思考过程的回复

```json
{
  "think": "用户询问天气，我需要提供简洁的回答。",
  "content": "今天天气晴朗，温度 25°C。"
}
```

### 示例 3：包含流程图

```json
{
  "think": "用户需要了解登录流程，我将画一个流程图。",
  "content": "用户登录流程如下：\n\n{{mermaid:login_flow}}\n\n这是标准的认证流程。",
  "attachments": {
    "login_flow": {
      "type": "mermaid",
      "content": "flowchart TD\n    A[输入账号密码] --> B{验证}\n    B -->|成功| C[登录成功]\n    B -->|失败| D[提示错误]"
    }
  }
}
```

### 示例 4：图表 + 代码混合

```json
{
  "think": "用户需要了解算法，我将先画流程图，再给出代码实现。",
  "content": "冒泡排序的流程：\n\n{{mermaid:bubble_flow}}\n\nPython 实现：\n\n{{code:bubble_impl}}\n\n时间复杂度为 O(n²)。",
  "attachments": {
    "bubble_flow": {
      "type": "mermaid",
      "content": "flowchart TD\n    A[开始] --> B[比较相邻元素]\n    B --> C{需要交换?}\n    C -->|是| D[交换]\n    C -->|否| E[继续]\n    D --> E\n    E --> F{完成?}\n    F -->|否| B\n    F -->|是| G[结束]"
    },
    "bubble_impl": {
      "type": "code",
      "language": "python",
      "content": "def bubble_sort(arr):\n    n = len(arr)\n    for i in range(n):\n        for j in range(0, n-i-1):\n            if arr[j] > arr[j+1]:\n                arr[j], arr[j+1] = arr[j+1], arr[j]\n    return arr"
    }
  }
}
```

## System Prompt 建议

在配置 AI 时，添加以下系统提示词：

```
你是一个智能助手。请严格按照以下 JSON 格式返回你的回复：

{
  "think": "你的思考过程（可选）",
  "content": "你的回复内容，使用 Markdown 格式。如果需要插入图表或代码，使用占位符：{{mermaid:id}} 或 {{code:id}}",
  "attachments": {
    "id": {
      "type": "mermaid" 或 "code",
      "content": "实际内容",
      "language": "代码语言（仅 code 类型需要）"
    }
  }
}

注意：
1. 必须返回有效的 JSON
2. content 字段必需，think 和 attachments 可选
3. 占位符格式：{{type:id}}
4. 如果不需要图表/代码，直接在 content 中写文本即可
```

## 错误处理

### 解析失败

如果 JSON 格式错误，会显示：

```
⚠️ JSON 解析失败

错误: JSON 格式错误: Unexpected character at position 45

原始内容:
```
{这里显示原始内容}
```
```

### 常见错误

1. **缺少 content 字段**: 必须提供 content
2. **JSON 格式错误**: 检查括号、引号、逗号
3. **占位符未找到**: 确保 attachments 中有对应的 id

## 最佳实践

1. ✅ **保持 JSON 简洁**: 只在需要时使用 attachments
2. ✅ **使用有意义的 ID**: 如 `login_flow` 而不是 `id1`
3. ✅ **合理使用 think**: 只在需要展示推理过程时使用
4. ✅ **Markdown 格式化**: content 中使用 Markdown 增强可读性
5. ✅ **代码指定语言**: code 类型务必指定 language

## 兼容性说明

- ✅ 支持 Markdown 代码块包裹的 JSON（```json ... ```）
- ✅ 支持 JSON 注释（会被自动忽略）
- ✅ 支持尾随逗号
- ✅ 大小写不敏感的字段名

## 未来扩展

计划支持的附件类型：
- `table`: 表格数据
- `chart`: 图表配置（Chart.js）
- `audio`: 音频文件
- `video`: 视频文件

---

**更新日期**: 2025-11-19
**版本**: 1.0.0

