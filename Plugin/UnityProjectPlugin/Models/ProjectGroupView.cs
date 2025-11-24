using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UnityProjectPlugin.Models
{
    /// <summary>
    /// 项目分组模型（用于视图显示）
    /// </summary>
    public class ProjectGroupView : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _groupName = string.Empty;
        private ObservableCollection<UnityProject> _projects = new();

        /// <summary>
        /// 分组名称（空表示"未分组"）
        /// </summary>
        public string GroupName
        {
            get => _groupName;
            set
            {
                if (_groupName != value)
                {
                    _groupName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 该分组下的项目列表
        /// </summary>
        public ObservableCollection<UnityProject> Projects
        {
            get => _projects;
            set
            {
                if (_projects != null)
                {
                    _projects.CollectionChanged -= OnProjectsCollectionChanged;
                }

                _projects = value;

                if (_projects != null)
                {
                    _projects.CollectionChanged += OnProjectsCollectionChanged;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(ProjectCountDescription));
            }
        }

        /// <summary>
        /// 项目计数描述（用于 SettingsExpander.Description）
        /// </summary>
        public string ProjectCountDescription => $"{Projects.Count} 个项目";

        private void OnProjectsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ProjectCountDescription));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
