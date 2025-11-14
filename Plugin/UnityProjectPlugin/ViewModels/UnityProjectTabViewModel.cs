using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityProjectPlugin.Models;

namespace UnityProjectPlugin.ViewModels
{
    /// <summary>
    /// Unity 项目标签页 ViewModel
    /// </summary>
    public class UnityProjectTabViewModel : INotifyPropertyChanged
    {
        private readonly UnityProjectPlugin _plugin;
        private ObservableCollection<UnityProject> _projects = new();
        private ObservableCollection<UnityProject> _filteredProjects = new();
        private string _searchText = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public UnityProjectTabViewModel(UnityProjectPlugin plugin)
        {
            _plugin = plugin;
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
        /// 过滤后的项目列表
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

        /// <summary>
        /// 加载项目列表
        /// </summary>
        public void LoadProjects()
        {
            List<UnityProject> projects = _plugin.GetProjects();
            _projects.Clear();
            foreach (UnityProject project in projects)
            {
                _projects.Add(project);
            }
            FilterProjects();
        }

        /// <summary>
        /// 过滤项目列表
        /// </summary>
        private void FilterProjects()
        {
            _filteredProjects.Clear();

            if (string.IsNullOrWhiteSpace(_searchText))
            {
                // 没有搜索文本，显示所有项目
                foreach (UnityProject project in _projects)
                {
                    _filteredProjects.Add(project);
                }
            }
            else
            {
                // 按项目名或分组名搜索（大小写不敏感）
                string searchLower = _searchText.ToLowerInvariant();
                foreach (UnityProject project in _projects)
                {
                    bool matchesName = project.Name.ToLowerInvariant().Contains(searchLower);
                    bool matchesGroup = !string.IsNullOrEmpty(project.GroupName) && 
                                       project.GroupName.ToLowerInvariant().Contains(searchLower);
                    
                    if (matchesName || matchesGroup)
                    {
                        _filteredProjects.Add(project);
                    }
                }
            }
        }

        /// <summary>
        /// 添加项目
        /// </summary>
        public void AddProject(string path, string? name = null)
        {
            try
            {
                _plugin.AddProject(path, name);
                LoadProjects();
            }
            catch (Exception ex)
            {
                throw new Exception($"添加项目失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除项目
        /// </summary>
        public void DeleteProject(UnityProject project)
        {
            try
            {
                _plugin.RemoveProject(project.Path);
                LoadProjects();
            }
            catch (Exception ex)
            {
                throw new Exception($"删除项目失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 打开项目
        /// </summary>
        public async void OpenProject(UnityProject project)
        {
            try
            {
                string result = await _plugin.OpenUnityProject(project.Path);
                // 刷新列表以更新最后打开时间
                LoadProjects();
            }
            catch (Exception ex)
            {
                throw new Exception($"打开项目失败: {ex.Message}", ex);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
