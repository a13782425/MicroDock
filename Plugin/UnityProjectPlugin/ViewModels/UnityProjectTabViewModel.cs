using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using UnityProjectPlugin.Helpers;
using UnityProjectPlugin.Models;
using UnityProjectPlugin.Services;

namespace UnityProjectPlugin.ViewModels
{
    /// <summary>
    /// Unity 项目标签页 ViewModel
    /// </summary>
    public class UnityProjectTabViewModel : INotifyPropertyChanged
    {
        private readonly UnityProjectPlugin _plugin;
        private readonly IFilePickerService _filePickerService;
        private ObservableCollection<UnityProject> _projects = new();
        private ObservableCollection<UnityProject> _filteredProjects = new();
        private ObservableCollection<ProjectGroupView> _groupedProjects = new();
        private string _searchText = string.Empty;
        private bool _isGroupViewEnabled = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand AddProjectCommand { get; }
        public ICommand OpenProjectCommand { get; }
        public ICommand DeleteProjectCommand { get; }
        public ICommand ToggleGroupViewCommand { get; }

        public UnityProjectTabViewModel(UnityProjectPlugin plugin, IFilePickerService filePickerService)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _filePickerService = filePickerService ?? throw new ArgumentNullException(nameof(filePickerService));
            // 从插件加载保存的分组视图状态
            _isGroupViewEnabled = _plugin.IsGroupViewEnabled;
            AddProjectCommand = new AsyncRelayCommand(AddProjectAsync);
            OpenProjectCommand = new AsyncRelayCommand(OpenProjectAsync);
            DeleteProjectCommand = new AsyncRelayCommand(DeleteProjectAsync);
            ToggleGroupViewCommand = new RelayCommand(_ => ToggleGroupView());

            LoadProjects();
        }

        /// <summary>
        /// 所有项目列表
        /// </summary>
        public ObservableCollection<UnityProject> Projects
        {
            get => _projects;
            set
            {
                if (_projects != value)
                {
                    _projects = value;
                    OnPropertyChanged();
                    FilterProjects();
                }
            }
        }

        /// <summary>
        /// 过滤后的项目列表（平铺视图）
        /// </summary>
        public ObservableCollection<UnityProject> FilteredProjects
        {
            get => _filteredProjects;
            set
            {
                if (_filteredProjects != value)
                {
                    _filteredProjects = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 分组后的项目列表（分组视图）
        /// </summary>
        public ObservableCollection<ProjectGroupView> GroupedProjects
        {
            get => _groupedProjects;
            set
            {
                if (_groupedProjects != value)
                {
                    _groupedProjects = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否启用分组视图
        /// </summary>
        public bool IsGroupViewEnabled
        {
            get => _isGroupViewEnabled;
            set
            {
                if (_isGroupViewEnabled != value)
                {
                    _isGroupViewEnabled = value;
                    _plugin.IsGroupViewEnabled = value;  // 同步到插件
                    OnPropertyChanged();
                    FilterProjects(); // 重新过滤和分组
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
                    FilterProjects();
                }
            }
        }

        private bool _hasProjects;

        /// <summary>
        /// 是否有项目（用于控制空状态显示）
        /// </summary>
        public bool HasProjects
        {
            get => _hasProjects;
            private set
            {
                if (_hasProjects != value)
                {
                    _hasProjects = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 加载项目列表
        /// </summary>
        public void LoadProjects()
        {
            try
            {
                _plugin.Context?.ShowLoading("加载项目中...");
                List<UnityProject> projects = _plugin.GetProjects();
                _projects.Clear();
                foreach (UnityProject project in projects)
                {
                    _projects.Add(project);
                }
                FilterProjects();
            }
            finally
            {
                _plugin.Context?.HideLoading();
            }
        }

        /// <summary>
        /// 过滤项目列表（同时更新平铺和分组视图）
        /// </summary>
        private void FilterProjects()
        {
            List<UnityProject> filtered;

            if (string.IsNullOrWhiteSpace(_searchText))
            {
                // 没有搜索文本，显示所有项目
                filtered = _projects.ToList();
            }
            else
            {
                // 按项目名或分组名搜索（大小写不敏感）
                string searchLower = _searchText.ToLowerInvariant();
                filtered = _projects.Where(project =>
                {
                    bool matchesName = project.Name.ToLowerInvariant().Contains(searchLower);
                    bool matchesGroup = !string.IsNullOrEmpty(project.GroupName) &&
                                       project.GroupName.ToLowerInvariant().Contains(searchLower);
                    return matchesName || matchesGroup;
                }).ToList();
            }

            // 按最后打开时间降序排序
            filtered = filtered.OrderByDescending(p => p.LastOpened).ToList();

            // 更新平铺视图
            _filteredProjects.Clear();
            foreach (UnityProject project in filtered)
            {
                _filteredProjects.Add(project);
            }

            // 更新 HasProjects 属性
            HasProjects = _projects.Count > 0;

            // 更新分组视图
            UpdateGroupedProjects(filtered);
        }

        /// <summary>
        /// 更新分组项目列表
        /// </summary>
        private void UpdateGroupedProjects(List<UnityProject> projects)
        {
            _groupedProjects.Clear();

            // 按分组名分组，组内按最后打开时间降序排序
            IEnumerable<IGrouping<string, UnityProject>> groups = projects
                .GroupBy(p => p.GroupName ?? string.Empty)
                .OrderBy(g => g.Key); // 分组按名称排序

            foreach (IGrouping<string, UnityProject> group in groups)
            {
                ProjectGroupView groupView = new ProjectGroupView
                {
                    GroupName = string.IsNullOrEmpty(group.Key) ? "未分组" : group.Key
                };

                // 分组内按最后打开时间降序排序
                foreach (UnityProject project in group.OrderByDescending(p => p.LastOpened))
                {
                    groupView.Projects.Add(project);
                }

                _groupedProjects.Add(groupView);
            }
        }

        /// <summary>
        /// 切换分组视图
        /// </summary>
        public void ToggleGroupView()
        {
            IsGroupViewEnabled = !IsGroupViewEnabled;
        }

        /// <summary>
        /// 添加项目
        /// </summary>
        private async Task AddProjectAsync(object? parameter)
        {
            try
            {
                _plugin.Context?.ShowLoading("选择项目文件夹...");
                var folderPath = await _filePickerService.PickSingleFolderAsync("选择 Unity 项目文件夹");
                if (folderPath != null)
                {
                    _plugin.Context?.ShowLoading("添加项目中...");
                    // 检查是否为 Unity 项目
                    var assetsPath = System.IO.Path.Combine(folderPath, "Assets");
                    var projectSettingsPath = System.IO.Path.Combine(folderPath, "ProjectSettings");
                    if (!System.IO.Directory.Exists(assetsPath) || !System.IO.Directory.Exists(projectSettingsPath))
                    {
                        _plugin.Context?.ShowInAppNotification(
                            "添加失败",
                            "这不是一个有效的 Unity 项目",
                            MicroDock.Plugin.NotificationType.Warning
                        );
                        return;
                    }

                    await _plugin.AddProjectAsync(folderPath);
                    LoadProjects();
                    
                    _plugin.Context?.ShowInAppNotification(
                        "添加成功",
                        $"已添加项目: {System.IO.Path.GetFileName(folderPath)}",
                        MicroDock.Plugin.NotificationType.Success
                    );
                }
            }
            catch (Exception ex)
            {
                _plugin.Context?.ShowInAppNotification(
                    "添加失败",
                    ex.Message,
                    MicroDock.Plugin.NotificationType.Error
                );
                _plugin.Context?.LogError("添加项目失败", ex);
            }
            finally
            {
                _plugin.Context?.HideLoading();
            }
        }

        /// <summary>
        /// 删除项目
        /// </summary>
        private async Task DeleteProjectAsync(object? parameter)
        {
            if (parameter is UnityProject project)
            {
                try
                {
                    await _plugin.RemoveProjectAsync(project.Path);
                    RemoveProjectFromView(project);
                    
                    _plugin.Context?.ShowInAppNotification(
                        "删除成功",
                        $"已删除项目: {project.Name}",
                        MicroDock.Plugin.NotificationType.Success
                    );
                }
                catch (Exception ex)
                {
                    _plugin.Context?.ShowInAppNotification(
                        "删除失败",
                        ex.Message,
                        MicroDock.Plugin.NotificationType.Error
                    );
                    _plugin.Context?.LogError("删除项目失败", ex);
                }
            }
        }

        /// <summary>
        /// 从视图中移除项目（增量更新，不刷新整个列表）
        /// </summary>
        private void RemoveProjectFromView(UnityProject project)
        {
            // 从主列表移除
            _projects.Remove(project);
            
            // 从过滤列表移除
            _filteredProjects.Remove(project);
            
            // 从分组视图移除
            foreach (ProjectGroupView groupView in _groupedProjects)
            {
                if (groupView.Projects.Remove(project))
                {
                    // 保留空分组，不移除
                    break;
                }
            }
            
            // 更新 HasProjects 状态
            HasProjects = _projects.Count > 0;
        }

        /// <summary>
        /// 打开项目
        /// </summary>
        private async Task OpenProjectAsync(object? parameter)
        {
            if (parameter is UnityProject project)
            {
                try
                {
                    _plugin.Context?.ShowLoading($"正在打开 {project.Name}...");
                    await _plugin.OpenUnityProject(project.Path);
                    // 刷新列表以更新最后打开时间
                    LoadProjects();
                }
                catch (Exception ex)
                {
                    _plugin.Context?.ShowInAppNotification(
                        "打开失败",
                        ex.Message,
                        MicroDock.Plugin.NotificationType.Error
                    );
                    _plugin.Context?.LogError("打开项目失败", ex);
                }
                finally
                {
                    _plugin.Context?.HideLoading();
                }
            }
        }

        /// <summary>
        /// 更新项目
        /// </summary>
        public async Task UpdateProjectAsync(UnityProject project, string newName, string? newGroup)
        {
            try
            {
                await _plugin.UpdateProjectAsync(project.Path, newName, newGroup);
                project.Name = newName;
                project.GroupName = newGroup;
                LoadProjects();
            }
            catch (Exception ex)
            {
                _plugin.Context?.LogError("更新项目失败", ex);
            }
        }

        /// <summary>
        /// 添加分组
        /// </summary>
        public async Task AddGroupAsync(string name)
        {
            try
            {
                await _plugin.AddGroupAsync(name);
                // 刷新可能需要的状态
            }
            catch (Exception ex)
            {
                _plugin.Context?.LogError("添加分组失败", ex);
            }
        }

        /// <summary>
        /// 删除分组
        /// </summary>
        public async Task DeleteGroupAsync(ProjectGroup group)
        {
            try
            {
                await _plugin.DeleteGroupAsync(group.Id);
                RemoveGroupFromView(group);
                
                _plugin.Context?.ShowInAppNotification(
                    "删除成功",
                    $"已删除分组: {group.Name}",
                    MicroDock.Plugin.NotificationType.Success
                );
            }
            catch (Exception ex)
            {
                _plugin.Context?.ShowInAppNotification(
                    "删除失败",
                    ex.Message,
                    MicroDock.Plugin.NotificationType.Error
                );
                _plugin.Context?.LogError("删除分组失败", ex);
            }
        }

        /// <summary>
        /// 从视图中移除分组（增量更新，不刷新整个列表）
        /// 将该分组下的项目移动到"未分组"
        /// </summary>
        private void RemoveGroupFromView(ProjectGroup group)
        {
            // 找到要删除的分组视图
            ProjectGroupView? groupViewToRemove = _groupedProjects.FirstOrDefault(g => g.GroupName == group.Name);
            if (groupViewToRemove == null)
            {
                return;
            }

            // 获取该分组下的所有项目
            List<UnityProject> projectsToMove = groupViewToRemove.Projects.ToList();

            // 如果有项目需要移动到"未分组"
            if (projectsToMove.Count > 0)
            {
                // 查找或创建"未分组"视图
                ProjectGroupView? ungroupedView = _groupedProjects.FirstOrDefault(g => g.GroupName == "未分组");
                if (ungroupedView == null)
                {
                    ungroupedView = new ProjectGroupView { GroupName = "未分组" };
                    _groupedProjects.Add(ungroupedView);
                }

                // 将项目移动到"未分组"，并更新项目的 GroupName
                foreach (UnityProject project in projectsToMove)
                {
                    project.GroupName = null;
                    ungroupedView.Projects.Add(project);
                }
            }

            // 从分组视图中移除该分组
            _groupedProjects.Remove(groupViewToRemove);
        }

        /// <summary>
        /// 获取所有分组
        /// </summary>
        public List<ProjectGroup> GetGroups()
        {
            return _plugin.GetGroups();
        }

        /// <summary>
        /// 获取分组使用计数
        /// </summary>
        public int GetGroupUsageCount(string groupName)
        {
            return _plugin.GetGroupUsageCount(groupName);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
