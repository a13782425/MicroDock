using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TodoListPlugin.Models;
using TodoListPlugin.Services;

namespace TodoListPlugin.ViewModels
{
    /// <summary>
    /// TodoList 主视图 ViewModel - 看板模式
    /// </summary>
    public class TodoListMainViewModel : INotifyPropertyChanged
    {
        private readonly TodoDataService _dataService;
        private readonly ObservableCollection<ProjectBoardViewModel> _projects;
        private ProjectBoardViewModel? _selectedProject;
        private bool _isLoading;
        private string _searchText = string.Empty;

        public TodoListMainViewModel(string dataPath)
        {
            _dataService = new TodoDataService(dataPath);
            _projects = new ObservableCollection<ProjectBoardViewModel>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 数据服务
        /// </summary>
        public TodoDataService DataService => _dataService;

        /// <summary>
        /// 项目列表
        /// </summary>
        public ObservableCollection<ProjectBoardViewModel> Projects => _projects;

        /// <summary>
        /// 当前选中的项目
        /// </summary>
        public ProjectBoardViewModel? SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (_selectedProject != value)
                {
                    _selectedProject = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelectedProject));
                }
            }
        }

        /// <summary>
        /// 是否有选中的项目
        /// </summary>
        public bool HasSelectedProject => _selectedProject != null;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    OnSearchTextChanged();
                }
            }
        }

        /// <summary>
        /// 全局设置
        /// </summary>
        public PluginSettings Settings => _dataService.Settings;

        /// <summary>
        /// 状态列列表
        /// </summary>
        public IReadOnlyList<StatusColumn> StatusColumns => _dataService.GetStatusColumns();

        /// <summary>
        /// 优先级列表
        /// </summary>
        public IReadOnlyList<PriorityGroup> Priorities => _dataService.GetPriorities();

        /// <summary>
        /// 标签列表
        /// </summary>
        public IReadOnlyList<TagGroup> Tags => _dataService.GetTags();

        /// <summary>
        /// 字段模板列表
        /// </summary>
        public IReadOnlyList<CustomFieldTemplate> FieldTemplates => _dataService.GetFieldTemplates();

        /// <summary>
        /// 加载所有数据
        /// </summary>
        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                await _dataService.LoadAllDataAsync();
                RefreshProjects();

                // 默认选中第一个项目
                if (_projects.Count > 0 && SelectedProject == null)
                {
                    SelectedProject = _projects[0];
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 刷新项目列表
        /// </summary>
        private void RefreshProjects()
        {
            _projects.Clear();
            var statusColumns = _dataService.GetStatusColumns();
            var fieldTemplates = _dataService.GetFieldTemplates();

            foreach (var project in _dataService.GetProjects().OrderBy(p => p.Order))
            {
                var projectVm = new ProjectBoardViewModel(project, statusColumns);
                var items = _dataService.GetItemsByProject(project.Id);
                projectVm.LoadItems(items, OnItemChanged, fieldTemplates);
                _projects.Add(projectVm);
            }
        }

        /// <summary>
        /// 添加新项目
        /// </summary>
        public ProjectBoardViewModel AddProject(string name, string color = "#2196F3", string? icon = null)
        {
            var project = new Project
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                Color = color,
                Icon = icon,
                Order = _projects.Count,
                CreatedTime = DateTime.Now
            };

            _dataService.AddProject(project);

            var statusColumns = _dataService.GetStatusColumns();
            var projectVm = new ProjectBoardViewModel(project, statusColumns);
            _projects.Add(projectVm);

            if (SelectedProject == null)
            {
                SelectedProject = projectVm;
            }

            return projectVm;
        }

        /// <summary>
        /// 删除项目
        /// </summary>
        public void DeleteProject(string projectId)
        {
            var projectVm = _projects.FirstOrDefault(p => p.Id == projectId);
            if (projectVm == null) return;

            _dataService.DeleteProject(projectId);
            _projects.Remove(projectVm);

            if (SelectedProject?.Id == projectId)
            {
                SelectedProject = _projects.FirstOrDefault();
            }
        }

        /// <summary>
        /// 更新项目
        /// </summary>
        public void UpdateProject(ProjectBoardViewModel projectVm)
        {
            _dataService.UpdateProject(projectVm.Model);
        }

        /// <summary>
        /// 重命名项目
        /// </summary>
        public void RenameProject(string projectId, string newName)
        {
            var projectVm = _projects.FirstOrDefault(p => p.Id == projectId);
            if (projectVm == null) return;

            projectVm.Name = newName;
            _dataService.UpdateProject(projectVm.Model);
        }

        /// <summary>
        /// 修改项目颜色
        /// </summary>
        public void ChangeProjectColor(string projectId, string newColor)
        {
            var projectVm = _projects.FirstOrDefault(p => p.Id == projectId);
            if (projectVm == null) return;

            projectVm.Color = newColor;
            _dataService.UpdateProject(projectVm.Model);
        }
        /// <summary>
        /// 添加待办事项
        /// </summary>
        public TodoItemViewModel AddTodoItem(string title, string? statusColumnId = null)
        {
            if (SelectedProject == null)
                throw new InvalidOperationException("请先选择一个项目");

            // 使用默认状态列（第一个）
            var targetStatusId = statusColumnId ?? StatusColumns.FirstOrDefault()?.Id ?? "todo";

            var item = new TodoItem
            {
                Id = Guid.NewGuid().ToString("N"),
                ProjectId = SelectedProject.Id,
                StatusColumnId = targetStatusId,
                Title = title,
                Order = GetNextOrderInStatus(targetStatusId),
                CreatedTime = DateTime.Now
            };

            _dataService.AddItem(item);

            var itemVm = new TodoItemViewModel(item, OnItemChanged);
            itemVm.SetFieldTemplates(FieldTemplates);
            SelectedProject.AddItem(itemVm);

            return itemVm;
        }

        /// <summary>
        /// 获取状态列中的下一个排序
        /// </summary>
        private int GetNextOrderInStatus(string statusColumnId)
        {
            if (SelectedProject == null) return 0;

            var column = SelectedProject.GetStatusColumn(statusColumnId);
            if (column == null || column.Items.Count == 0) return 0;

            return column.Items.Max(i => i.Order) + 1;
        }

        /// <summary>
        /// 删除待办事项
        /// </summary>
        public void DeleteTodoItem(string itemId)
        {
            if (SelectedProject == null) return;

            _dataService.DeleteItem(SelectedProject.Id, itemId);
            SelectedProject.RemoveItem(itemId);
        }

        /// <summary>
        /// 移动待办事项到其他状态
        /// </summary>
        public void MoveItemToStatus(string itemId, string targetStatusId)
        {
            if (SelectedProject == null) return;

            _dataService.MoveItemToStatus(SelectedProject.Id, itemId, targetStatusId);
            SelectedProject.MoveItemToStatus(itemId, targetStatusId);
        }

        /// <summary>
        /// 移动待办事项到其他项目
        /// </summary>
        public void MoveItemToProject(string itemId, string targetProjectId)
        {
            if (SelectedProject == null) return;
            if (SelectedProject.Id == targetProjectId) return;

            var itemVm = SelectedProject.GetItem(itemId);
            if (itemVm == null) return;

            _dataService.MoveItemToProject(SelectedProject.Id, itemId, targetProjectId);

            // 从当前项目移除
            SelectedProject.RemoveItem(itemId);

            // 添加到目标项目
            var targetProject = _projects.FirstOrDefault(p => p.Id == targetProjectId);
            if (targetProject != null)
            {
                itemVm.ProjectId = targetProjectId;
                targetProject.AddItem(itemVm);
            }
        }

        /// <summary>
        /// 更新待办事项
        /// </summary>
        public void UpdateTodoItem(TodoItemViewModel itemVm)
        {
            if (itemVm == null) return;
            _dataService.UpdateItem(itemVm.Model);
        }

        /// <summary>
        /// 删除待办事项（重载版本）
        /// </summary>
        public void DeleteTodoItem(TodoItemViewModel itemVm)
        {
            if (itemVm == null) return;
            DeleteTodoItem(itemVm.Id);
        }

        /// <summary>
        /// 移动待办事项到其他项目（重载版本）
        /// </summary>
        public void MoveTodoItemToProject(TodoItemViewModel itemVm, string targetProjectId)
        {
            if (itemVm == null) return;
            MoveItemToProject(itemVm.Id, targetProjectId);
        }

        /// <summary>
        /// 待办事项变更回调
        /// </summary>
        private void OnItemChanged(TodoItem item)
        {
            if (SelectedProject == null) return;
            _dataService.UpdateItem(item);
        }

        /// <summary>
        /// 搜索文本变更
        /// </summary>
        private void OnSearchTextChanged()
        {
            if (SelectedProject == null) return;

            // 遍历所有状态列和待办项，应用搜索过滤
            foreach (var column in SelectedProject.StatusColumns)
            {
                foreach (var item in column.Items)
                {
                    item.ApplySearchFilter(_searchText);
                }
            }
        }

        #region 设置管理

        /// <summary>
        /// 添加状态列
        /// </summary>
        public void AddStatusColumn(StatusColumn column)
        {
            _dataService.AddStatusColumn(column);
            OnPropertyChanged(nameof(StatusColumns));

            // 更新所有项目的状态列
            RefreshProjects();
        }

        /// <summary>
        /// 更新状态列
        /// </summary>
        public void UpdateStatusColumn(StatusColumn column)
        {
            _dataService.UpdateStatusColumn(column);
            OnPropertyChanged(nameof(StatusColumns));
        }

        /// <summary>
        /// 删除状态列
        /// </summary>
        public void DeleteStatusColumn(string columnId)
        {
            _dataService.DeleteStatusColumn(columnId);
            OnPropertyChanged(nameof(StatusColumns));

            // 更新所有项目的状态列
            RefreshProjects();
        }

        /// <summary>
        /// 添加优先级
        /// </summary>
        public void AddPriority(PriorityGroup priority)
        {
            _dataService.AddPriority(priority);
            OnPropertyChanged(nameof(Priorities));
        }

        /// <summary>
        /// 更新优先级
        /// </summary>
        public void UpdatePriority(PriorityGroup priority)
        {
            _dataService.UpdatePriority(priority);
            OnPropertyChanged(nameof(Priorities));
        }

        /// <summary>
        /// 删除优先级
        /// </summary>
        public void DeletePriority(string priorityId)
        {
            _dataService.DeletePriority(priorityId);
            OnPropertyChanged(nameof(Priorities));
        }

        /// <summary>
        /// 添加标签
        /// </summary>
        public void AddTag(TagGroup tag)
        {
            _dataService.AddTag(tag);
            OnPropertyChanged(nameof(Tags));
        }

        /// <summary>
        /// 更新标签
        /// </summary>
        public void UpdateTag(TagGroup tag)
        {
            _dataService.UpdateTag(tag);
            OnPropertyChanged(nameof(Tags));
        }

        /// <summary>
        /// 删除标签
        /// </summary>
        public void DeleteTag(string tagId)
        {
            _dataService.DeleteTag(tagId);
            OnPropertyChanged(nameof(Tags));
        }

        /// <summary>
        /// 添加字段模板
        /// </summary>
        public void AddFieldTemplate(CustomFieldTemplate template)
        {
            _dataService.AddFieldTemplate(template);
            OnPropertyChanged(nameof(FieldTemplates));
        }

        /// <summary>
        /// 更新字段模板
        /// </summary>
        public void UpdateFieldTemplate(CustomFieldTemplate template)
        {
            _dataService.UpdateFieldTemplate(template);
            OnPropertyChanged(nameof(FieldTemplates));
        }

        /// <summary>
        /// 删除字段模板
        /// </summary>
        public void DeleteFieldTemplate(string templateId)
        {
            _dataService.DeleteFieldTemplate(templateId);
            OnPropertyChanged(nameof(FieldTemplates));
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
