using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
        public Task<ProjectBoardViewModel> AddProjectAsync(string name, string color = "#2196F3", string? icon = null)
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

            return Task.FromResult(projectVm);
        }

        /// <summary>
        /// 删除项目
        /// </summary>
        public Task DeleteProjectAsync(string projectId)
        {
            var projectVm = _projects.FirstOrDefault(p => p.Id == projectId);
            if (projectVm == null) return Task.CompletedTask;

            _dataService.DeleteProject(projectId);
            _projects.Remove(projectVm);

            if (SelectedProject?.Id == projectId)
            {
                SelectedProject = _projects.FirstOrDefault();
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 更新项目
        /// </summary>
        public Task UpdateProjectAsync(ProjectBoardViewModel projectVm)
        {
            _dataService.UpdateProject(projectVm.Model);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 重命名项目
        /// </summary>
        public Task RenameProjectAsync(string projectId, string newName)
        {
            var projectVm = _projects.FirstOrDefault(p => p.Id == projectId);
            if (projectVm == null) return Task.CompletedTask;

            projectVm.Name = newName;
            _dataService.UpdateProject(projectVm.Model);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 修改项目颜色
        /// </summary>
        public Task ChangeProjectColorAsync(string projectId, string newColor)
        {
            var projectVm = _projects.FirstOrDefault(p => p.Id == projectId);
            if (projectVm == null) return Task.CompletedTask;

            projectVm.Color = newColor;
            _dataService.UpdateProject(projectVm.Model);
            return Task.CompletedTask;
        }
        /// <summary>
        /// 添加待办事项
        /// </summary>
        public Task<TodoItemViewModel> AddTodoItemAsync(string title, string? statusColumnId = null)
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

            return Task.FromResult(itemVm);
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
        public Task DeleteTodoItemAsync(string itemId)
        {
            if (SelectedProject == null) return Task.CompletedTask;

            _dataService.DeleteItem(SelectedProject.Id, itemId);
            SelectedProject.RemoveItem(itemId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 移动待办事项到其他状态
        /// </summary>
        public Task MoveItemToStatusAsync(string itemId, string targetStatusId)
        {
            if (SelectedProject == null) return Task.CompletedTask;

            _dataService.MoveItemToStatus(SelectedProject.Id, itemId, targetStatusId);
            SelectedProject.MoveItemToStatus(itemId, targetStatusId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 移动待办事项到其他项目
        /// </summary>
        public Task MoveItemToProjectAsync(string itemId, string targetProjectId)
        {
            if (SelectedProject == null) return Task.CompletedTask;
            if (SelectedProject.Id == targetProjectId) return Task.CompletedTask;

            var itemVm = SelectedProject.GetItem(itemId);
            if (itemVm == null) return Task.CompletedTask;

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
            return Task.CompletedTask;
        }

        /// <summary>
        /// 更新待办事项
        /// </summary>
        public Task UpdateTodoItemAsync(TodoItemViewModel itemVm)
        {
            if (itemVm == null) return Task.CompletedTask;
            _dataService.UpdateItem(itemVm.Model);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 删除待办事项（重载版本）
        /// </summary>
        public Task DeleteTodoItemAsync(TodoItemViewModel itemVm)
        {
            if (itemVm == null) return Task.CompletedTask;
            return DeleteTodoItemAsync(itemVm.Id);
        }

        /// <summary>
        /// 移动待办事项到其他项目（重载版本）
        /// </summary>
        public Task MoveTodoItemToProjectAsync(TodoItemViewModel itemVm, string targetProjectId)
        {
            if (itemVm == null) return Task.CompletedTask;
            return MoveItemToProjectAsync(itemVm.Id, targetProjectId);
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
        public Task AddStatusColumnAsync(StatusColumn column)
        {
            _dataService.AddStatusColumn(column);
            OnPropertyChanged(nameof(StatusColumns));

            // 更新所有项目的状态列
            RefreshProjects();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 更新状态列
        /// </summary>
        public Task UpdateStatusColumnAsync(StatusColumn column)
        {
            _dataService.UpdateStatusColumn(column);
            OnPropertyChanged(nameof(StatusColumns));
            return Task.CompletedTask;
        }

        /// <summary>
        /// 删除状态列
        /// </summary>
        public Task DeleteStatusColumnAsync(string columnId)
        {
            _dataService.DeleteStatusColumn(columnId);
            OnPropertyChanged(nameof(StatusColumns));

            // 更新所有项目的状态列
            RefreshProjects();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 添加优先级
        /// </summary>
        public Task AddPriorityAsync(PriorityGroup priority)
        {
            _dataService.AddPriority(priority);
            OnPropertyChanged(nameof(Priorities));
            return Task.CompletedTask;
        }

        /// <summary>
        /// 更新优先级
        /// </summary>
        public Task UpdatePriorityAsync(PriorityGroup priority)
        {
            _dataService.UpdatePriority(priority);
            OnPropertyChanged(nameof(Priorities));
            return Task.CompletedTask;
        }

        /// <summary>
        /// 删除优先级
        /// </summary>
        public Task DeletePriorityAsync(string priorityId)
        {
            _dataService.DeletePriority(priorityId);
            OnPropertyChanged(nameof(Priorities));
            return Task.CompletedTask;
        }

        /// <summary>
        /// 添加标签
        /// </summary>
        public Task AddTagAsync(TagGroup tag)
        {
            _dataService.AddTag(tag);
            OnPropertyChanged(nameof(Tags));
            return Task.CompletedTask;
        }

        /// <summary>
        /// 更新标签
        /// </summary>
        public Task UpdateTagAsync(TagGroup tag)
        {
            _dataService.UpdateTag(tag);
            OnPropertyChanged(nameof(Tags));
            return Task.CompletedTask;
        }

        /// <summary>
        /// 删除标签
        /// </summary>
        public Task DeleteTagAsync(string tagId)
        {
            _dataService.DeleteTag(tagId);
            OnPropertyChanged(nameof(Tags));
            return Task.CompletedTask;
        }

        /// <summary>
        /// 添加字段模板
        /// </summary>
        public Task AddFieldTemplateAsync(CustomFieldTemplate template)
        {
            _dataService.AddFieldTemplate(template);
            OnPropertyChanged(nameof(FieldTemplates));
            return Task.CompletedTask;
        }

        /// <summary>
        /// 更新字段模板
        /// </summary>
        public Task UpdateFieldTemplateAsync(CustomFieldTemplate template)
        {
            _dataService.UpdateFieldTemplate(template);
            OnPropertyChanged(nameof(FieldTemplates));
            return Task.CompletedTask;
        }

        /// <summary>
        /// 删除字段模板
        /// </summary>
        public Task DeleteFieldTemplateAsync(string templateId)
        {
            _dataService.DeleteFieldTemplate(templateId);
            OnPropertyChanged(nameof(FieldTemplates));
            return Task.CompletedTask;
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
