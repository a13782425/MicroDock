using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityProjectPlugin.Models;

namespace UnityProjectPlugin.ViewModels
{
    /// <summary>
    /// 分组管理 ViewModel
    /// </summary>
    public class GroupManagementViewModel : INotifyPropertyChanged
    {
        private readonly UnityProjectPlugin _plugin;
        private ObservableCollection<ProjectGroup> _groups = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public GroupManagementViewModel(UnityProjectPlugin plugin)
        {
            _plugin = plugin;
            LoadGroups();
        }

        /// <summary>
        /// 分组列表
        /// </summary>
        public ObservableCollection<ProjectGroup> Groups
        {
            get => _groups;
            set
            {
                if (_groups != value)
                {
                    _groups = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 加载分组列表
        /// </summary>
        public void LoadGroups()
        {
            List<ProjectGroup> groups = _plugin.GetGroups();
            _groups.Clear();
            foreach (ProjectGroup group in groups)
            {
                _groups.Add(group);
            }
        }

        /// <summary>
        /// 添加分组
        /// </summary>
        public async System.Threading.Tasks.Task AddGroupAsync(string name)
        {
            try
            {
                await _plugin.AddGroupAsync(name);
                LoadGroups();
            }
            catch (Exception ex)
            {
                throw new Exception($"添加分组失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新分组
        /// </summary>
        public async System.Threading.Tasks.Task UpdateGroupAsync(string id, string newName)
        {
            try
            {
                await _plugin.UpdateGroupAsync(id, newName);
                LoadGroups();
            }
            catch (Exception ex)
            {
                throw new Exception($"更新分组失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取分组使用数量
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

