using Avalonia.Controls;
using MicroDock.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TodoListPlugin.Models;
using TodoListPlugin.Views;

namespace TodoListPlugin
{
    /// <summary>
    /// 待办清单插件
    /// </summary>
    public class TodoListPlugin : BaseMicroDockPlugin
    {
        private string _dataFolder = string.Empty;
        private List<TodoColumn> _columns = new List<TodoColumn>();
        private List<TodoItem> _items = new List<TodoItem>();
        private List<CustomFieldTemplate> _fieldTemplates = new List<CustomFieldTemplate>();
        private List<PriorityGroup> _priorities = new List<PriorityGroup>();
        private List<TagGroup> _tags = new List<TagGroup>();
        private TodoListTabView? _tabView;
        private ColumnSettingsView? _columnSettingsView;
        private FieldTemplateSettingsView? _fieldTemplateSettingsView;
        private Timer? _reminderTimer;

        /// <summary>
        /// JSON 序列化选项
        /// </summary>
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public override IMicroTab[] Tabs
        {
            get
            {
                if (_tabView == null)
                {
                    _tabView = new TodoListTabView(this);
                }
                return new IMicroTab[] { _tabView };
            }
        }

        public override object? GetSettingsControl()
        {
            // 创建设置容器
            StackPanel container = new StackPanel
            {
                Spacing = 24,
                Margin = new Avalonia.Thickness(0, 0, 0, 12)
            };

            // 页签管理
            if (_columnSettingsView == null)
            {
                _columnSettingsView = new ColumnSettingsView(this);
            }
            container.Children.Add(_columnSettingsView);

            // 分隔线
            Avalonia.Controls.Border separator = new Avalonia.Controls.Border
            {
                Height = 1,
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(200, 200, 200)),
                Opacity = 0.3,
                Margin = new Avalonia.Thickness(0, 12, 0, 12)
            };
            container.Children.Add(separator);

            // 字段模板管理
            if (_fieldTemplateSettingsView == null)
            {
                _fieldTemplateSettingsView = new FieldTemplateSettingsView(this);
            }
            container.Children.Add(_fieldTemplateSettingsView);

            return container;
        }

        public override void OnInit()
        {
            base.OnInit();

            LogInfo("待办清单插件初始化中...");

            // 初始化数据文件夹路径
            _dataFolder = Context?.DataPath ?? string.Empty;
            if (string.IsNullOrEmpty(_dataFolder))
            {
                LogError("无法获取插件数据文件夹路径");
                return;
            }

            // 确保数据文件夹存在
            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
                LogInfo($"创建数据文件夹: {_dataFolder}");
            }

            // 加载数据
            LoadColumnsFromFile();
            LoadItemsFromFile();
            LoadFieldTemplatesFromFile();
            LoadPrioritiesFromFile();
            LoadTagsFromFile();

            // 如果没有列，创建默认列
            if (_columns.Count == 0)
            {
                CreateDefaultColumns();
            }

            // 确保默认字段存在
            EnsureDefaultFields();

            // 启动提醒定时器（每分钟检查一次）
            _reminderTimer = new Timer(CheckReminders, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));

            LogInfo($"已加载 {_columns.Count} 个页签、{_items.Count} 个待办事项、{_fieldTemplates.Count} 个字段模板、{_priorities.Count} 个优先级和 {_tags.Count} 个标签");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            // 停止并释放定时器
            if (_reminderTimer != null)
            {
                _reminderTimer.Dispose();
                _reminderTimer = null;
            }

            LogInfo("待办清单插件已销毁");
        }

        #region 数据加载和保存

        /// <summary>
        /// 从文件加载页签列表
        /// </summary>
        private void LoadColumnsFromFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "columns.json");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    _columns = JsonSerializer.Deserialize<List<TodoColumn>>(json, _jsonOptions) ?? new List<TodoColumn>();
                    LogInfo($"从文件加载了 {_columns.Count} 个页签");
                }
                else
                {
                    _columns = new List<TodoColumn>();
                    LogInfo("页签文件不存在，使用空列表");
                }
            }
            catch (Exception ex)
            {
                LogError("从文件加载页签列表失败", ex);
                _columns = new List<TodoColumn>();
            }
        }

        /// <summary>
        /// 保存页签列表到文件
        /// </summary>
        private void SaveColumnsToFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "columns.json");
                string json = JsonSerializer.Serialize(_columns, _jsonOptions);
                File.WriteAllText(filePath, json);
                LogDebug("页签列表已保存到文件");
            }
            catch (Exception ex)
            {
                LogError("保存页签列表到文件失败", ex);
            }
        }

        /// <summary>
        /// 从文件加载待办事项列表
        /// </summary>
        private void LoadItemsFromFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "items.json");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    _items = JsonSerializer.Deserialize<List<TodoItem>>(json, _jsonOptions) ?? new List<TodoItem>();
                    LogInfo($"从文件加载了 {_items.Count} 个待办事项");
                }
                else
                {
                    _items = new List<TodoItem>();
                    LogInfo("待办事项文件不存在，使用空列表");
                }
            }
            catch (Exception ex)
            {
                LogError("从文件加载待办事项列表失败", ex);
                _items = new List<TodoItem>();
            }
        }

        /// <summary>
        /// 保存待办事项列表到文件
        /// </summary>
        private void SaveItemsToFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "items.json");
                string json = JsonSerializer.Serialize(_items, _jsonOptions);
                File.WriteAllText(filePath, json);
                LogDebug("待办事项列表已保存到文件");
            }
            catch (Exception ex)
            {
                LogError("保存待办事项列表到文件失败", ex);
            }
        }

        /// <summary>
        /// 从文件加载字段模板列表
        /// </summary>
        private void LoadFieldTemplatesFromFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "fieldTemplates.json");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    _fieldTemplates = JsonSerializer.Deserialize<List<CustomFieldTemplate>>(json, _jsonOptions) ?? new List<CustomFieldTemplate>();
                    LogInfo($"从文件加载了 {_fieldTemplates.Count} 个字段模板");
                }
                else
                {
                    _fieldTemplates = new List<CustomFieldTemplate>();
                    LogInfo("字段模板文件不存在，使用空列表");
                }
            }
            catch (Exception ex)
            {
                LogError("从文件加载字段模板列表失败", ex);
                _fieldTemplates = new List<CustomFieldTemplate>();
            }
        }

        /// <summary>
        /// 保存字段模板列表到文件
        /// </summary>
        private void SaveFieldTemplatesToFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "fieldTemplates.json");
                string json = JsonSerializer.Serialize(_fieldTemplates, _jsonOptions);
                File.WriteAllText(filePath, json);
                LogDebug("字段模板列表已保存到文件");
            }
            catch (Exception ex)
            {
                LogError("保存字段模板列表到文件失败", ex);
            }
        }

        /// <summary>
        /// 从文件加载优先级列表
        /// </summary>
        private void LoadPrioritiesFromFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "priorities.json");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    _priorities = JsonSerializer.Deserialize<List<PriorityGroup>>(json, _jsonOptions) ?? new List<PriorityGroup>();
                    LogInfo($"从文件加载了 {_priorities.Count} 个优先级");
                }
                else
                {
                    _priorities = new List<PriorityGroup>();
                    LogInfo("优先级文件不存在，使用空列表");
                }
            }
            catch (Exception ex)
            {
                LogError("从文件加载优先级列表失败", ex);
                _priorities = new List<PriorityGroup>();
            }
        }

        /// <summary>
        /// 保存优先级列表到文件
        /// </summary>
        private void SavePrioritiesToFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "priorities.json");
                string json = JsonSerializer.Serialize(_priorities, _jsonOptions);
                File.WriteAllText(filePath, json);
                LogDebug("优先级列表已保存到文件");
            }
            catch (Exception ex)
            {
                LogError("保存优先级列表到文件失败", ex);
            }
        }

        /// <summary>
        /// 从文件加载标签列表
        /// </summary>
        private void LoadTagsFromFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "tags.json");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    _tags = JsonSerializer.Deserialize<List<TagGroup>>(json, _jsonOptions) ?? new List<TagGroup>();
                    LogInfo($"从文件加载了 {_tags.Count} 个标签");
                }
                else
                {
                    _tags = new List<TagGroup>();
                    LogInfo("标签文件不存在，使用空列表");
                }
            }
            catch (Exception ex)
            {
                LogError("从文件加载标签列表失败", ex);
                _tags = new List<TagGroup>();
            }
        }

        /// <summary>
        /// 保存标签列表到文件
        /// </summary>
        private void SaveTagsToFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "tags.json");
                string json = JsonSerializer.Serialize(_tags, _jsonOptions);
                File.WriteAllText(filePath, json);
                LogDebug("标签列表已保存到文件");
            }
            catch (Exception ex)
            {
                LogError("保存标签列表到文件失败", ex);
            }
        }

        /// <summary>
        /// 创建默认页签
        /// </summary>
        private void CreateDefaultColumns()
        {
            _columns.Add(new TodoColumn { Name = "未开始", Order = 0, Color = "#FF6B9BD1" });
            _columns.Add(new TodoColumn { Name = "进行中", Order = 1, Color = "#FFFFA500" });
            _columns.Add(new TodoColumn { Name = "完成", Order = 2, Color = "#FF90EE90" });
            SaveColumnsToFile();
            LogInfo("已创建默认页签");
        }

        #endregion

        #region 页签管理 API

        /// <summary>
        /// 添加页签
        /// </summary>
        public void AddColumn(string name, string? color = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("页签名称不能为空", nameof(name));
            }

            // 检查是否已存在同名页签
            if (_columns.Any(c => c.Name == name))
            {
                throw new InvalidOperationException($"页签 '{name}' 已存在");
            }

            TodoColumn column = new TodoColumn
            {
                Name = name,
                Order = _columns.Count,
                Color = color
            };

            _columns.Add(column);
            SaveColumnsToFile();

            LogInfo($"已添加页签: {name}");
        }

        /// <summary>
        /// 更新页签
        /// </summary>
        public void UpdateColumn(string id, string newName, string? newColor = null)
        {
            TodoColumn? column = _columns.FirstOrDefault(c => c.Id == id);
            if (column == null)
            {
                throw new InvalidOperationException("页签不存在");
            }

            // 检查新名称是否与其他页签重复
            if (_columns.Any(c => c.Id != id && c.Name == newName))
            {
                throw new InvalidOperationException($"页签 '{newName}' 已存在");
            }

            column.Name = newName;
            column.Color = newColor;
            SaveColumnsToFile();

            LogInfo($"已更新页签: {newName}");
        }

        /// <summary>
        /// 删除页签
        /// </summary>
        public void DeleteColumn(string id)
        {
            TodoColumn? column = _columns.FirstOrDefault(c => c.Id == id);
            if (column == null)
            {
                throw new InvalidOperationException("页签不存在");
            }

            // 检查是否有待办事项使用该页签
            int itemCount = _items.Count(i => i.ColumnId == id);
            if (itemCount > 0)
            {
                throw new InvalidOperationException($"无法删除页签 '{column.Name}'，还有 {itemCount} 个待办事项在该页签中");
            }

            _columns.Remove(column);
            SaveColumnsToFile();

            LogInfo($"已删除页签: {column.Name}");
        }

        /// <summary>
        /// 获取所有页签
        /// </summary>
        public List<TodoColumn> GetColumns()
        {
            return new List<TodoColumn>(_columns.OrderBy(c => c.Order));
        }

        /// <summary>
        /// 重新排序页签
        /// </summary>
        public void ReorderColumns(List<string> columnIds)
        {
            for (int i = 0; i < columnIds.Count; i++)
            {
                TodoColumn? column = _columns.FirstOrDefault(c => c.Id == columnIds[i]);
                if (column != null)
                {
                    column.Order = i;
                }
            }
            SaveColumnsToFile();
            LogInfo("页签顺序已更新");
        }

        #endregion

        #region 待办事项管理 API

        /// <summary>
        /// 添加待办事项
        /// </summary>
        public TodoItem AddTodo(string title, string columnId, string description = "")
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("标题不能为空", nameof(title));
            }

            if (!_columns.Any(c => c.Id == columnId))
            {
                throw new ArgumentException("指定的页签不存在", nameof(columnId));
            }

            TodoItem item = new TodoItem
            {
                Title = title,
                Description = description,
                ColumnId = columnId,
                CreatedTime = DateTime.Now
            };

            // 为新待办事项初始化自定义字段默认值
            foreach (CustomFieldTemplate template in _fieldTemplates)
            {
                if (!string.IsNullOrEmpty(template.DefaultValue))
                {
                    item.CustomFields[template.Id] = template.DefaultValue;
                }
            }

            _items.Add(item);
            SaveItemsToFile();

            LogInfo($"已添加待办事项: {title}");
            return item;
        }

        /// <summary>
        /// 更新待办事项
        /// </summary>
        public void UpdateTodo(TodoItem item)
        {
            TodoItem? existingItem = _items.FirstOrDefault(i => i.Id == item.Id);
            if (existingItem == null)
            {
                throw new InvalidOperationException("待办事项不存在");
            }

            // 更新所有属性
            int index = _items.IndexOf(existingItem);
            _items[index] = item;
            SaveItemsToFile();

            LogInfo($"已更新待办事项: {item.Title}");
        }

        /// <summary>
        /// 删除待办事项
        /// </summary>
        public void DeleteTodo(string id)
        {
            TodoItem? item = _items.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                _items.Remove(item);
                SaveItemsToFile();
                LogInfo($"已删除待办事项: {item.Title}");
            }
        }

        /// <summary>
        /// 移动待办事项到另一个页签
        /// </summary>
        public void MoveTodo(string itemId, string targetColumnId)
        {
            TodoItem? item = _items.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
            {
                throw new InvalidOperationException("待办事项不存在");
            }

            if (!_columns.Any(c => c.Id == targetColumnId))
            {
                throw new ArgumentException("目标页签不存在", nameof(targetColumnId));
            }

            item.ColumnId = targetColumnId;
            SaveItemsToFile();

            LogInfo($"已移动待办事项 '{item.Title}' 到新页签");
        }

        /// <summary>
        /// 获取指定页签的待办事项
        /// </summary>
        public List<TodoItem> GetTodosByColumn(string columnId)
        {
            return _items.Where(i => i.ColumnId == columnId).OrderByDescending(i => i.CreatedTime).ToList();
        }

        /// <summary>
        /// 获取所有待办事项
        /// </summary>
        public List<TodoItem> GetAllTodos()
        {
            return new List<TodoItem>(_items);
        }

        #endregion

        #region 字段模板管理 API

        /// <summary>
        /// 添加字段模板
        /// </summary>
        public void AddFieldTemplate(string name, FieldType fieldType, bool required = false, string? defaultValue = null, bool isFilterable = false)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("字段名称不能为空", nameof(name));
            }

            // 检查是否已存在同名字段
            if (_fieldTemplates.Any(f => f.Name == name))
            {
                throw new InvalidOperationException($"字段 '{name}' 已存在");
            }

            CustomFieldTemplate template = new CustomFieldTemplate
            {
                Name = name,
                FieldType = fieldType,
                Required = required,
                DefaultValue = defaultValue,
                IsFilterable = isFilterable,
                Order = _fieldTemplates.Count
            };

            _fieldTemplates.Add(template);
            SaveFieldTemplatesToFile();

            LogInfo($"已添加字段模板: {name}");
        }

        /// <summary>
        /// 更新字段模板
        /// </summary>
        public void UpdateFieldTemplate(string id, string newName, FieldType fieldType, bool required, string? defaultValue, bool isFilterable)
        {
            CustomFieldTemplate? template = _fieldTemplates.FirstOrDefault(f => f.Id == id);
            if (template == null)
            {
                throw new InvalidOperationException("字段模板不存在");
            }

            // 检查新名称是否与其他字段重复
            if (_fieldTemplates.Any(f => f.Id != id && f.Name == newName))
            {
                throw new InvalidOperationException($"字段 '{newName}' 已存在");
            }

            template.Name = newName;
            template.FieldType = fieldType;
            template.Required = required;
            template.DefaultValue = defaultValue;
            template.IsFilterable = isFilterable;
            SaveFieldTemplatesToFile();

            LogInfo($"已更新字段模板: {newName}");
        }

        /// <summary>
        /// 删除字段模板
        /// </summary>
        public void DeleteFieldTemplate(string id)
        {
            CustomFieldTemplate? template = _fieldTemplates.FirstOrDefault(f => f.Id == id);
            if (template == null)
            {
                throw new InvalidOperationException("字段模板不存在");
            }

            // 从所有待办事项中移除该字段的值
            foreach (TodoItem item in _items)
            {
                if (item.CustomFields.ContainsKey(id))
                {
                    item.CustomFields.Remove(id);
                }
            }

            _fieldTemplates.Remove(template);
            SaveFieldTemplatesToFile();
            SaveItemsToFile();

            LogInfo($"已删除字段模板: {template.Name}");
        }

        /// <summary>
        /// 获取所有字段模板
        /// </summary>
        public List<CustomFieldTemplate> GetFieldTemplates()
        {
            return new List<CustomFieldTemplate>(_fieldTemplates.OrderBy(f => f.Order));
        }

        /// <summary>
        /// 获取可筛选的字段模板
        /// </summary>
        public List<CustomFieldTemplate> GetFilterableTemplates()
        {
            return _fieldTemplates.Where(f => f.IsFilterable).OrderBy(f => f.Order).ToList();
        }

        #endregion

    #region 提醒系统

    /// <summary>
    /// 检查并触发提醒
    /// </summary>
    private void CheckReminders(object? state)
    {
        try
        {
            // 快速检查：如果没有任何启用提醒的事项，直接返回
            if (!_items.Any(i => i.IsReminderEnabled && 
                                i.ReminderIntervalType != ReminderInterval.None))
            {
                return;
            }

            DateTime now = DateTime.Now;
            List<TodoItem> itemsToNotify = new List<TodoItem>();

            foreach (var item in _items.Where(i => i.IsReminderEnabled))
            {
                if (ShouldTriggerReminder(item, now))
                {
                    itemsToNotify.Add(item);
                    item.LastReminderTime = now;
                }
            }

            if (itemsToNotify.Count > 0)
            {
                LogInfo($"发现 {itemsToNotify.Count} 个待办事项需要提醒");

                foreach (TodoItem item in itemsToNotify)
                {
                    try
                    {
                        // 显示系统通知
                        Context?.ShowSystemNotification(
                            "待办提醒",
                            $"{item.Title}\n{item.Description}");

                        LogInfo($"已触发提醒: {item.Title}");
                    }
                    catch (Exception ex)
                    {
                        LogError($"触发提醒失败: {item.Title}", ex);
                    }
                }

                // 保存状态
                SaveItemsToFile();
            }
        }
        catch (Exception ex)
        {
            LogError("检查提醒时发生错误", ex);
        }
    }

    /// <summary>
    /// 判断是否应该触发提醒
    /// </summary>
    private bool ShouldTriggerReminder(TodoItem item, DateTime now)
    {
        // 首次提醒：如果从未提醒过，立即提醒
        if (!item.LastReminderTime.HasValue)
            return true;

        TimeSpan elapsed = now - item.LastReminderTime.Value;

        return item.ReminderIntervalType switch
        {
            ReminderInterval.Every15Minutes => elapsed.TotalMinutes >= 15,
            ReminderInterval.Every30Minutes => elapsed.TotalMinutes >= 30,
            ReminderInterval.Every1Hour => elapsed.TotalHours >= 1,
            ReminderInterval.Every2Hours => elapsed.TotalHours >= 2,
            ReminderInterval.EveryDayAt9AM => 
                now.Hour == 9 && now.Minute < 5 && 
                (now.Date > item.LastReminderTime.Value.Date),
            _ => false
        };
    }

    #endregion

        #region 筛选辅助方法

        /// <summary>
        /// 检查待办事项是否匹配筛选条件
        /// </summary>
        public bool MatchesFilter(TodoItem item, FilterCondition filter)
        {
            if (!item.CustomFields.TryGetValue(filter.FieldId, out string? value))
            {
                return false;
            }

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return filter.FieldType switch
            {
                FieldType.Text => MatchTextFilter(value, filter.Value, filter.Operator),
                FieldType.Number => MatchNumberFilter(value, filter.Value, filter.Operator),
                FieldType.Date => MatchDateFilter(value, filter.Value, filter.Operator),
                FieldType.Bool => MatchBoolFilter(value, filter.Value),
                _ => false
            };
        }

        private bool MatchTextFilter(string value, string filterValue, FilterOperator op)
        {
            return op switch
            {
                FilterOperator.Contains => value.Contains(filterValue, StringComparison.OrdinalIgnoreCase),
                FilterOperator.Equals => value.Equals(filterValue, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        private bool MatchNumberFilter(string value, string filterValue, FilterOperator op)
        {
            if (!double.TryParse(value, out double numValue) || !double.TryParse(filterValue, out double numFilter))
            {
                return false;
            }

            return op switch
            {
                FilterOperator.Equals => Math.Abs(numValue - numFilter) < 0.0001,
                FilterOperator.GreaterThan => numValue > numFilter,
                FilterOperator.LessThan => numValue < numFilter,
                FilterOperator.GreaterOrEqual => numValue >= numFilter,
                FilterOperator.LessOrEqual => numValue <= numFilter,
                _ => false
            };
        }

        private bool MatchDateFilter(string value, string filterValue, FilterOperator op)
        {
            if (!DateTime.TryParse(value, out DateTime dateValue) || !DateTime.TryParse(filterValue, out DateTime dateFilter))
            {
                return false;
            }

            // 只比较日期部分
            dateValue = dateValue.Date;
            dateFilter = dateFilter.Date;

            return op switch
            {
                FilterOperator.Equals => dateValue == dateFilter,
                FilterOperator.GreaterThan => dateValue > dateFilter,
                FilterOperator.LessThan => dateValue < dateFilter,
                FilterOperator.GreaterOrEqual => dateValue >= dateFilter,
                FilterOperator.LessOrEqual => dateValue <= dateFilter,
                _ => false
            };
        }

        private bool MatchBoolFilter(string value, string filterValue)
        {
            return value.Equals(filterValue, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region 默认字段

        /// <summary>
        /// 确保默认字段存在
        /// </summary>
        private void EnsureDefaultFields()
        {
            List<CustomFieldTemplate> requiredFields = new List<CustomFieldTemplate>
            {
                new CustomFieldTemplate
                {
                    Id = "builtin-title",
                    Name = "标题",
                    FieldType = FieldType.Text,
                    IsFilterable = true,
                    IsDefault = true,
                    Order = -2
                },
                new CustomFieldTemplate
                {
                    Id = "builtin-description",
                    Name = "简述",
                    FieldType = FieldType.Text,
                    IsFilterable = true,
                    IsDefault = true,
                    Order = -1
                },
                new CustomFieldTemplate
                {
                    Id = "default-priority",
                    Name = "优先级",
                    FieldType = FieldType.Text,
                    IsFilterable = true,
                    IsDefault = true,
                    Order = 0
                },
                new CustomFieldTemplate
                {
                    Id = "default-tags",
                    Name = "标签",
                    FieldType = FieldType.Text,
                    IsFilterable = true,
                    IsDefault = true,
                    Order = 1
                }
            };

            bool hasChanges = false;
            foreach (CustomFieldTemplate requiredField in requiredFields)
            {
                if (!_fieldTemplates.Any(f => f.Id == requiredField.Id))
                {
                    _fieldTemplates.Add(requiredField);
                    hasChanges = true;
                    LogInfo($"添加默认字段: {requiredField.Name}");
                }
            }

            if (hasChanges)
            {
                SaveFieldTemplatesToFile();
                LogInfo("默认字段已更新");
            }
        }

        #endregion

        #region 优先级管理 API

        /// <summary>
        /// 添加优先级
        /// </summary>
        public void AddPriority(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("优先级名称不能为空", nameof(name));
            }

            // 检查是否已存在同名优先级
            if (_priorities.Any(p => p.Name == name))
            {
                throw new InvalidOperationException($"优先级 '{name}' 已存在");
            }

            PriorityGroup priority = new PriorityGroup
            {
                Name = name
            };

            _priorities.Add(priority);
            SavePrioritiesToFile();

            LogInfo($"已添加优先级: {name}");
        }

        /// <summary>
        /// 更新优先级
        /// </summary>
        public void UpdatePriority(string id, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("优先级名称不能为空", nameof(newName));
            }

            PriorityGroup? priority = _priorities.FirstOrDefault(p => p.Id == id);
            if (priority == null)
            {
                throw new InvalidOperationException("优先级不存在");
            }

            // 检查新名称是否与其他优先级重复
            if (_priorities.Any(p => p.Id != id && p.Name == newName))
            {
                throw new InvalidOperationException($"优先级 '{newName}' 已存在");
            }

            string oldName = priority.Name;
            priority.Name = newName;

            // 更新使用该优先级的所有待办事项
            foreach (TodoItem item in _items.Where(i => i.PriorityName == oldName))
            {
                item.PriorityName = newName;
            }

            SavePrioritiesToFile();
            SaveItemsToFile();

            LogInfo($"已更新优先级: {oldName} -> {newName}");
        }

        /// <summary>
        /// 删除优先级
        /// </summary>
        public void DeletePriority(string id)
        {
            PriorityGroup? priority = _priorities.FirstOrDefault(p => p.Id == id);
            if (priority == null)
            {
                throw new InvalidOperationException("优先级不存在");
            }

            // 检查是否有待办事项使用该优先级
            int usageCount = GetPriorityUsageCount(priority.Name);
            if (usageCount > 0)
            {
                throw new InvalidOperationException($"无法删除优先级 '{priority.Name}'，还有 {usageCount} 个待办事项使用该优先级");
            }

            _priorities.Remove(priority);
            SavePrioritiesToFile();

            LogInfo($"已删除优先级: {priority.Name}");
        }

        /// <summary>
        /// 获取所有优先级
        /// </summary>
        public List<PriorityGroup> GetPriorities() => new List<PriorityGroup>(_priorities);

        /// <summary>
        /// 获取优先级使用数量
        /// </summary>
        public int GetPriorityUsageCount(string priorityName)
        {
            return _items.Count(i => i.PriorityName == priorityName);
        }

        #endregion

        #region 标签管理 API

        /// <summary>
        /// 添加标签
        /// </summary>
        public void AddTag(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("标签名称不能为空", nameof(name));
            }

            // 检查是否已存在同名标签
            if (_tags.Any(t => t.Name == name))
            {
                throw new InvalidOperationException($"标签 '{name}' 已存在");
            }

            TagGroup tag = new TagGroup
            {
                Name = name
            };

            _tags.Add(tag);
            SaveTagsToFile();

            LogInfo($"已添加标签: {name}");
        }

        /// <summary>
        /// 更新标签
        /// </summary>
        public void UpdateTag(string id, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("标签名称不能为空", nameof(newName));
            }

            TagGroup? tag = _tags.FirstOrDefault(t => t.Id == id);
            if (tag == null)
            {
                throw new InvalidOperationException("标签不存在");
            }

            // 检查新名称是否与其他标签重复
            if (_tags.Any(t => t.Id != id && t.Name == newName))
            {
                throw new InvalidOperationException($"标签 '{newName}' 已存在");
            }

            string oldName = tag.Name;
            tag.Name = newName;

            // 更新使用该标签的所有待办事项
            foreach (TodoItem item in _items)
            {
                if (item.Tags.Contains(oldName))
                {
                    int index = item.Tags.IndexOf(oldName);
                    item.Tags[index] = newName;
                }
            }

            SaveTagsToFile();
            SaveItemsToFile();

            LogInfo($"已更新标签: {oldName} -> {newName}");
        }

        /// <summary>
        /// 删除标签
        /// </summary>
        public void DeleteTag(string id)
        {
            TagGroup? tag = _tags.FirstOrDefault(t => t.Id == id);
            if (tag == null)
            {
                throw new InvalidOperationException("标签不存在");
            }

            // 检查是否有待办事项使用该标签
            int usageCount = GetTagUsageCount(tag.Name);
            if (usageCount > 0)
            {
                throw new InvalidOperationException($"无法删除标签 '{tag.Name}'，还有 {usageCount} 个待办事项使用该标签");
            }

            _tags.Remove(tag);
            SaveTagsToFile();

            LogInfo($"已删除标签: {tag.Name}");
        }

        /// <summary>
        /// 获取所有标签
        /// </summary>
        public List<TagGroup> GetTags() => new List<TagGroup>(_tags);

        /// <summary>
        /// 获取标签使用数量
        /// </summary>
        public int GetTagUsageCount(string tagName)
        {
            return _items.Count(i => i.Tags.Contains(tagName));
        }

        #endregion
    }
}

