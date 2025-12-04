using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TodoListPlugin.Models;

namespace TodoListPlugin.Services
{
    /// <summary>
    /// 待办数据服务
    /// 数据结构：settings.json + projects.json + items/{projectId}.json
    /// </summary>
    public class TodoDataService
    {
        private readonly string _dataFolder;
        private readonly string _itemsFolder;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly SemaphoreSlim _saveLock = new(1, 1);

        private PluginSettings _settings = new();
        private List<Project> _projects = new();
        private Dictionary<string, List<TodoItem>> _itemsByProject = new();

        // 防抖保存
        private CancellationTokenSource? _saveSettingsCts;
        private CancellationTokenSource? _saveProjectsCts;
        private Dictionary<string, CancellationTokenSource> _saveItemsCts = new();

        /// <summary>
        /// 数据变更事件
        /// </summary>
        public event Action? DataChanged;

        public TodoDataService(string dataFolder)
        {
            _dataFolder = dataFolder;
            _itemsFolder = Path.Combine(dataFolder, "items");
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            // 确保数据文件夹存在
            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }
            if (!Directory.Exists(_itemsFolder))
            {
                Directory.CreateDirectory(_itemsFolder);
            }
        }

        #region 数据加载

        /// <summary>
        /// 加载所有数据
        /// </summary>
        public async Task LoadAllDataAsync()
        {
            await LoadSettingsAsync();
            await LoadProjectsAsync();
            await LoadAllItemsAsync();
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "settings.json");
                if (File.Exists(filePath))
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    _settings = JsonSerializer.Deserialize<PluginSettings>(json, _jsonOptions) ?? PluginSettings.CreateDefault();
                    
                    // 确保内置字段模板存在
                    EnsureBuiltinFieldTemplates();
                }
                else
                {
                    _settings = PluginSettings.CreateDefault();
                    await SaveSettingsAsync();
                }
            }
            catch
            {
                _settings = PluginSettings.CreateDefault();
            }
        }

        /// <summary>
        /// 确保内置字段模板存在，如果缺失则添加
        /// </summary>
        private void EnsureBuiltinFieldTemplates()
        {
            var builtinFields = new[]
            {
                new CustomFieldTemplate { Id = "builtin_description", Name = "描述", FieldType = FieldType.Text, IsDefault = true, ShowOnCard = true, Order = 0 },
                new CustomFieldTemplate { Id = "builtin_priority", Name = "优先级", FieldType = FieldType.Select, IsDefault = true, ShowOnCard = true, Order = 1 },
                new CustomFieldTemplate { Id = "builtin_duedate", Name = "截止日期", FieldType = FieldType.Date, IsDefault = true, ShowOnCard = true, Order = 2 },
                new CustomFieldTemplate { Id = "builtin_tags", Name = "标签", FieldType = FieldType.Text, IsDefault = true, ShowOnCard = false, Order = 3 }
            };

            bool changed = false;
            foreach (var builtin in builtinFields)
            {
                if (!_settings.FieldTemplates.Any(f => f.Id == builtin.Id))
                {
                    _settings.FieldTemplates.Insert(0, builtin);
                    changed = true;
                }
            }

            // 如果有变化，按Order重新排序
            if (changed)
            {
                _settings.FieldTemplates = _settings.FieldTemplates.OrderBy(f => f.IsDefault ? 0 : 1).ThenBy(f => f.Order).ToList();
                _ = SaveSettingsAsync();
            }
        }

        private async Task LoadProjectsAsync()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "projects.json");
                if (File.Exists(filePath))
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    _projects = JsonSerializer.Deserialize<List<Project>>(json, _jsonOptions) ?? new List<Project>();
                }
                else
                {
                    _projects = new List<Project>();
                }
            }
            catch
            {
                _projects = new List<Project>();
            }
        }

        private async Task LoadAllItemsAsync()
        {
            _itemsByProject.Clear();
            foreach (var project in _projects)
            {
                await LoadProjectItemsAsync(project.Id);
            }
        }

        private async Task LoadProjectItemsAsync(string projectId)
        {
            try
            {
                string filePath = Path.Combine(_itemsFolder, $"{projectId}.json");
                if (File.Exists(filePath))
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    var items = JsonSerializer.Deserialize<List<TodoItem>>(json, _jsonOptions) ?? new List<TodoItem>();
                    _itemsByProject[projectId] = items;
                }
                else
                {
                    _itemsByProject[projectId] = new List<TodoItem>();
                }
            }
            catch
            {
                _itemsByProject[projectId] = new List<TodoItem>();
            }
        }

        #endregion

        #region 设置管理

        /// <summary>
        /// 获取插件设置
        /// </summary>
        public PluginSettings Settings => _settings;

        /// <summary>
        /// 获取插件设置
        /// </summary>
        public PluginSettings GetSettings() => _settings;

        /// <summary>
        /// 获取所有状态列
        /// </summary>
        public IReadOnlyList<StatusColumn> GetStatusColumns() => _settings.StatusColumns.OrderBy(s => s.Order).ToList();

        /// <summary>
        /// 获取默认状态列
        /// </summary>
        public StatusColumn? GetDefaultStatusColumn() => _settings.StatusColumns.FirstOrDefault(s => s.IsDefault) ?? _settings.StatusColumns.FirstOrDefault();

        /// <summary>
        /// 添加状态列
        /// </summary>
        public void AddStatusColumn(StatusColumn column)
        {
            column.Order = _settings.StatusColumns.Count;
            _settings.StatusColumns.Add(column);
            SaveSettingsDebounced();
            DataChanged?.Invoke();
        }

        /// <summary>
        /// 更新状态列
        /// </summary>
        public void UpdateStatusColumn(StatusColumn column)
        {
            var existing = _settings.StatusColumns.FirstOrDefault(s => s.Id == column.Id);
            if (existing != null)
            {
                existing.Name = column.Name;
                existing.Color = column.Color;
                existing.Icon = column.Icon;
                existing.IsDefault = column.IsDefault;
                
                // 如果设为默认，取消其他的默认状态
                if (column.IsDefault)
                {
                    foreach (var s in _settings.StatusColumns.Where(x => x.Id != column.Id))
                    {
                        s.IsDefault = false;
                    }
                }
                
                SaveSettingsDebounced();
                DataChanged?.Invoke();
            }
        }

        /// <summary>
        /// 删除状态列
        /// </summary>
        public void DeleteStatusColumn(string id)
        {
            // 检查是否有待办事项使用此状态
            var hasItems = _itemsByProject.Values.SelectMany(items => items).Any(i => i.StatusColumnId == id);
            if (hasItems)
            {
                throw new InvalidOperationException("无法删除此状态，还有待办事项使用此状态");
            }

            var column = _settings.StatusColumns.FirstOrDefault(s => s.Id == id);
            if (column != null)
            {
                _settings.StatusColumns.Remove(column);
                SaveSettingsDebounced();
                DataChanged?.Invoke();
            }
        }

        /// <summary>
        /// 重新排序状态列
        /// </summary>
        public void ReorderStatusColumns(List<string> columnIds)
        {
            for (int i = 0; i < columnIds.Count; i++)
            {
                var column = _settings.StatusColumns.FirstOrDefault(s => s.Id == columnIds[i]);
                if (column != null)
                {
                    column.Order = i;
                }
            }
            SaveSettingsDebounced();
            DataChanged?.Invoke();
        }

        /// <summary>
        /// 获取所有优先级
        /// </summary>
        public IReadOnlyList<PriorityGroup> GetPriorities() => _settings.Priorities.ToList();

        /// <summary>
        /// 添加优先级
        /// </summary>
        public void AddPriority(PriorityGroup priority)
        {
            if (_settings.Priorities.Any(p => p.Name == priority.Name))
                throw new InvalidOperationException($"优先级 '{priority.Name}' 已存在");

            _settings.Priorities.Add(priority);
            SaveSettingsDebounced();
            DataChanged?.Invoke();
        }

        /// <summary>
        /// 更新优先级
        /// </summary>
        public void UpdatePriority(PriorityGroup priority)
        {
            var existing = _settings.Priorities.FirstOrDefault(p => p.Name == priority.Name);
            if (existing != null)
            {
                existing.Color = priority.Color;
                SaveSettingsDebounced();
                DataChanged?.Invoke();
            }
        }

        /// <summary>
        /// 删除优先级
        /// </summary>
        public void DeletePriority(string name)
        {
            var priority = _settings.Priorities.FirstOrDefault(p => p.Name == name);
            if (priority != null)
            {
                _settings.Priorities.Remove(priority);
                SaveSettingsDebounced();
                DataChanged?.Invoke();
            }
        }

        /// <summary>
        /// 获取所有标签
        /// </summary>
        public IReadOnlyList<TagGroup> GetTags() => _settings.Tags.ToList();

        /// <summary>
        /// 添加标签
        /// </summary>
        public void AddTag(TagGroup tag)
        {
            if (_settings.Tags.Any(t => t.Name == tag.Name))
                throw new InvalidOperationException($"标签 '{tag.Name}' 已存在");

            _settings.Tags.Add(tag);
            SaveSettingsDebounced();
            DataChanged?.Invoke();
        }

        /// <summary>
        /// 更新标签
        /// </summary>
        public void UpdateTag(TagGroup tag)
        {
            var existing = _settings.Tags.FirstOrDefault(t => t.Name == tag.Name);
            if (existing != null)
            {
                existing.Color = tag.Color;
                SaveSettingsDebounced();
                DataChanged?.Invoke();
            }
        }

        /// <summary>
        /// 删除标签
        /// </summary>
        public void DeleteTag(string name)
        {
            var tag = _settings.Tags.FirstOrDefault(t => t.Name == name);
            if (tag != null)
            {
                _settings.Tags.Remove(tag);
                SaveSettingsDebounced();
                DataChanged?.Invoke();
            }
        }

        /// <summary>
        /// 获取所有字段模板
        /// </summary>
        public IReadOnlyList<CustomFieldTemplate> GetFieldTemplates() => _settings.FieldTemplates.ToList();

        /// <summary>
        /// 添加字段模板
        /// </summary>
        public void AddFieldTemplate(CustomFieldTemplate template)
        {
            _settings.FieldTemplates.Add(template);
            SaveSettingsDebounced();
            DataChanged?.Invoke();
        }

        /// <summary>
        /// 更新字段模板
        /// </summary>
        public void UpdateFieldTemplate(CustomFieldTemplate template)
        {
            var existing = _settings.FieldTemplates.FirstOrDefault(f => f.Id == template.Id);
            if (existing != null)
            {
                existing.Name = template.Name;
                existing.FieldType = template.FieldType;
                existing.DefaultValue = template.DefaultValue;
                existing.Required = template.Required;
                SaveSettingsDebounced();
                DataChanged?.Invoke();
            }
        }

        /// <summary>
        /// 删除字段模板
        /// </summary>
        public void DeleteFieldTemplate(string id)
        {
            var template = _settings.FieldTemplates.FirstOrDefault(f => f.Id == id);
            if (template != null)
            {
                _settings.FieldTemplates.Remove(template);
                SaveSettingsDebounced();
                DataChanged?.Invoke();
            }
        }

        #endregion

        #region 项目管理

        /// <summary>
        /// 获取所有项目
        /// </summary>
        public IReadOnlyList<Project> GetProjects() => _projects.OrderBy(p => p.Order).ToList();

        /// <summary>
        /// 获取项目
        /// </summary>
        public Project? GetProject(string id) => _projects.FirstOrDefault(p => p.Id == id);

        /// <summary>
        /// 添加项目
        /// </summary>
        public void AddProject(Project project)
        {
            if (string.IsNullOrWhiteSpace(project.Name))
                throw new ArgumentException("项目名称不能为空");

            if (_projects.Any(p => p.Name == project.Name))
                throw new InvalidOperationException($"项目 '{project.Name}' 已存在");

            project.Order = _projects.Count;
            _projects.Add(project);
            _itemsByProject[project.Id] = new List<TodoItem>();
            
            SaveProjectsDebounced();
            DataChanged?.Invoke();
        }

        /// <summary>
        /// 更新项目
        /// </summary>
        public void UpdateProject(Project project)
        {
            var existing = _projects.FirstOrDefault(p => p.Id == project.Id);
            if (existing == null)
                throw new InvalidOperationException("项目不存在");

            existing.Name = project.Name;
            existing.Color = project.Color;
            existing.Icon = project.Icon;
            existing.Description = project.Description;

            SaveProjectsDebounced();
            DataChanged?.Invoke();
        }

        /// <summary>
        /// 删除项目
        /// </summary>
        public void DeleteProject(string id)
        {
            var project = _projects.FirstOrDefault(p => p.Id == id);
            if (project == null) return;

            // 删除项目的所有待办事项文件
            string itemsFile = Path.Combine(_itemsFolder, $"{id}.json");
            if (File.Exists(itemsFile))
            {
                File.Delete(itemsFile);
            }

            _projects.Remove(project);
            _itemsByProject.Remove(id);

            SaveProjectsDebounced();
            DataChanged?.Invoke();
        }

        /// <summary>
        /// 重新排序项目
        /// </summary>
        public void ReorderProjects(List<string> projectIds)
        {
            for (int i = 0; i < projectIds.Count; i++)
            {
                var project = _projects.FirstOrDefault(p => p.Id == projectIds[i]);
                if (project != null)
                {
                    project.Order = i;
                }
            }
            SaveProjectsDebounced();
            DataChanged?.Invoke();
        }

        #endregion

        #region 待办事项管理

        /// <summary>
        /// 获取项目的所有待办事项
        /// </summary>
        public IReadOnlyList<TodoItem> GetItemsByProject(string projectId)
        {
            if (_itemsByProject.TryGetValue(projectId, out var items))
            {
                return items.OrderBy(i => i.Order).ToList();
            }
            return new List<TodoItem>();
        }

        /// <summary>
        /// 获取项目中指定状态的待办事项
        /// </summary>
        public IReadOnlyList<TodoItem> GetItemsByStatus(string projectId, string statusColumnId)
        {
            if (_itemsByProject.TryGetValue(projectId, out var items))
            {
                return items
                    .Where(i => i.StatusColumnId == statusColumnId)
                    .OrderBy(i => i.Order)
                    .ToList();
            }
            return new List<TodoItem>();
        }

        /// <summary>
        /// 获取待办事项
        /// </summary>
        public TodoItem? GetItem(string projectId, string itemId)
        {
            if (_itemsByProject.TryGetValue(projectId, out var items))
            {
                return items.FirstOrDefault(i => i.Id == itemId);
            }
            return null;
        }

        /// <summary>
        /// 添加待办事项
        /// </summary>
        public void AddItem(TodoItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Title))
                throw new ArgumentException("标题不能为空");

            if (string.IsNullOrEmpty(item.ProjectId))
                throw new ArgumentException("必须指定项目");

            // 设置默认状态
            if (string.IsNullOrEmpty(item.StatusColumnId))
            {
                var defaultStatus = GetDefaultStatusColumn();
                item.StatusColumnId = defaultStatus?.Id ?? string.Empty;
            }

            if (!_itemsByProject.TryGetValue(item.ProjectId, out var items))
            {
                items = new List<TodoItem>();
                _itemsByProject[item.ProjectId] = items;
            }

            // 设置排序（添加到对应状态列的末尾）
            var statusItems = items.Where(i => i.StatusColumnId == item.StatusColumnId).ToList();
            item.Order = statusItems.Count > 0 ? statusItems.Max(i => i.Order) + 1 : 0;

            items.Add(item);
            SaveItemsDebounced(item.ProjectId);
            DataChanged?.Invoke();
        }

        /// <summary>
        /// 更新待办事项
        /// </summary>
        public void UpdateItem(TodoItem item)
        {
            if (!_itemsByProject.TryGetValue(item.ProjectId, out var items)) return;

            var existing = items.FirstOrDefault(i => i.Id == item.Id);
            if (existing != null)
            {
                existing.Title = item.Title;
                existing.Description = item.Description;
                existing.StatusColumnId = item.StatusColumnId;
                existing.PriorityName = item.PriorityName;
                existing.Tags = item.Tags;
                existing.CustomFields = item.CustomFields;
                existing.IsReminderEnabled = item.IsReminderEnabled;
                existing.ReminderIntervalType = item.ReminderIntervalType;
                existing.LastReminderTime = item.LastReminderTime;

                SaveItemsDebounced(item.ProjectId);
                DataChanged?.Invoke();
            }
        }

        /// <summary>
        /// 删除待办事项
        /// </summary>
        public void DeleteItem(string projectId, string itemId)
        {
            if (!_itemsByProject.TryGetValue(projectId, out var items)) return;

            var item = items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                items.Remove(item);
                SaveItemsDebounced(projectId);
                DataChanged?.Invoke();
            }
        }

        /// <summary>
        /// 移动待办事项到其他状态
        /// </summary>
        public void MoveItemToStatus(string projectId, string itemId, string targetStatusId, int? targetOrder = null)
        {
            if (!_itemsByProject.TryGetValue(projectId, out var items)) return;

            var item = items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return;

            item.StatusColumnId = targetStatusId;

            // 重新计算排序
            if (targetOrder.HasValue)
            {
                item.Order = targetOrder.Value;
            }
            else
            {
                var statusItems = items.Where(i => i.StatusColumnId == targetStatusId && i.Id != itemId).ToList();
                item.Order = statusItems.Count > 0 ? statusItems.Max(i => i.Order) + 1 : 0;
            }

            SaveItemsDebounced(projectId);
            DataChanged?.Invoke();
        }

        /// <summary>
        /// 移动待办事项到其他项目
        /// </summary>
        public void MoveItemToProject(string sourceProjectId, string itemId, string targetProjectId, string? targetStatusId = null)
        {
            if (!_itemsByProject.TryGetValue(sourceProjectId, out var sourceItems)) return;

            var item = sourceItems.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return;

            // 从源项目移除
            sourceItems.Remove(item);

            // 更新项目ID
            item.ProjectId = targetProjectId;

            // 如果指定了目标状态，更新状态
            if (!string.IsNullOrEmpty(targetStatusId))
            {
                item.StatusColumnId = targetStatusId;
            }

            // 添加到目标项目
            if (!_itemsByProject.TryGetValue(targetProjectId, out var targetItems))
            {
                targetItems = new List<TodoItem>();
                _itemsByProject[targetProjectId] = targetItems;
            }

            // 重新计算排序
            var statusItems = targetItems.Where(i => i.StatusColumnId == item.StatusColumnId).ToList();
            item.Order = statusItems.Count > 0 ? statusItems.Max(i => i.Order) + 1 : 0;

            targetItems.Add(item);

            SaveItemsDebounced(sourceProjectId);
            SaveItemsDebounced(targetProjectId);
            DataChanged?.Invoke();
        }

        /// <summary>
        /// 重新排序状态内的待办事项
        /// </summary>
        public void ReorderItems(string projectId, string statusColumnId, List<string> itemIds)
        {
            if (!_itemsByProject.TryGetValue(projectId, out var items)) return;

            for (int i = 0; i < itemIds.Count; i++)
            {
                var item = items.FirstOrDefault(x => x.Id == itemIds[i] && x.StatusColumnId == statusColumnId);
                if (item != null)
                {
                    item.Order = i;
                }
            }

            SaveItemsDebounced(projectId);
            DataChanged?.Invoke();
        }

        #endregion

        #region 保存

        private async void SaveSettingsDebounced()
        {
            _saveSettingsCts?.Cancel();
            _saveSettingsCts = new CancellationTokenSource();
            try
            {
                await Task.Delay(500, _saveSettingsCts.Token);
                await SaveSettingsAsync();
            }
            catch (TaskCanceledException) { }
        }

        private async Task SaveSettingsAsync()
        {
            await _saveLock.WaitAsync();
            try
            {
                string filePath = Path.Combine(_dataFolder, "settings.json");
                string json = JsonSerializer.Serialize(_settings, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            finally
            {
                _saveLock.Release();
            }
        }

        private async void SaveProjectsDebounced()
        {
            _saveProjectsCts?.Cancel();
            _saveProjectsCts = new CancellationTokenSource();
            try
            {
                await Task.Delay(500, _saveProjectsCts.Token);
                await SaveProjectsAsync();
            }
            catch (TaskCanceledException) { }
        }

        private async Task SaveProjectsAsync()
        {
            await _saveLock.WaitAsync();
            try
            {
                string filePath = Path.Combine(_dataFolder, "projects.json");
                string json = JsonSerializer.Serialize(_projects, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            finally
            {
                _saveLock.Release();
            }
        }

        private async void SaveItemsDebounced(string projectId)
        {
            if (_saveItemsCts.TryGetValue(projectId, out var existingCts))
            {
                existingCts.Cancel();
            }

            var cts = new CancellationTokenSource();
            _saveItemsCts[projectId] = cts;

            try
            {
                await Task.Delay(500, cts.Token);
                await SaveProjectItemsAsync(projectId);
            }
            catch (TaskCanceledException) { }
        }

        private async Task SaveProjectItemsAsync(string projectId)
        {
            await _saveLock.WaitAsync();
            try
            {
                if (_itemsByProject.TryGetValue(projectId, out var items))
                {
                    string filePath = Path.Combine(_itemsFolder, $"{projectId}.json");
                    string json = JsonSerializer.Serialize(items, _jsonOptions);
                    await File.WriteAllTextAsync(filePath, json);
                }
            }
            finally
            {
                _saveLock.Release();
            }
        }

        /// <summary>
        /// 立即保存所有数据
        /// </summary>
        public async Task SaveAllAsync()
        {
            await SaveSettingsAsync();
            await SaveProjectsAsync();
            foreach (var projectId in _itemsByProject.Keys.ToList())
            {
                await SaveProjectItemsAsync(projectId);
            }
        }

        #endregion
    }
}
