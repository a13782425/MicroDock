using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using RemoteDesktopPlugin.Models;

namespace RemoteDesktopPlugin.ViewModels
{
    /// <summary>
    /// 远程桌面标签页 ViewModel
    /// </summary>
    public class RemoteDesktopTabViewModel : INotifyPropertyChanged
    {
        private readonly RemoteDesktopPlugin _plugin;
        private ObservableCollection<RemoteConnection> _connections = new();
        private ObservableCollection<RemoteConnection> _filteredConnections = new();
        private ObservableCollection<ProjectGroupView> _groupedConnections = new();
        private string _searchText = string.Empty;
        private bool _isGroupViewEnabled = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public RemoteDesktopTabViewModel(RemoteDesktopPlugin plugin)
        {
            _plugin = plugin;
            LoadConnections();
        }

        /// <summary>
        /// 所有连接列表
        /// </summary>
        public ObservableCollection<RemoteConnection> Connections
        {
            get => _connections;
            set
            {
                if (_connections != value)
                {
                    _connections = value;
                    OnPropertyChanged();
                    FilterConnections();
                }
            }
        }

        /// <summary>
        /// 过滤后的连接列表（平铺视图）
        /// </summary>
        public ObservableCollection<RemoteConnection> FilteredConnections
        {
            get => _filteredConnections;
            set
            {
                if (_filteredConnections != value)
                {
                    _filteredConnections = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 分组后的连接列表（分组视图）
        /// </summary>
        public ObservableCollection<ProjectGroupView> GroupedConnections
        {
            get => _groupedConnections;
            set
            {
                if (_groupedConnections != value)
                {
                    _groupedConnections = value;
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
                    OnPropertyChanged();
                    FilterConnections(); // 重新过滤和分组
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
                    FilterConnections();
                }
            }
        }

        /// <summary>
        /// 加载连接列表
        /// </summary>
        public void LoadConnections()
        {
            List<RemoteConnection> connections = _plugin.GetConnections();
            _connections.Clear();
            foreach (RemoteConnection connection in connections)
            {
                _connections.Add(connection);
            }
            FilterConnections();
        }

        /// <summary>
        /// 过滤连接列表
        /// </summary>
        private void FilterConnections()
        {
            List<RemoteConnection> filtered;

            if (string.IsNullOrWhiteSpace(_searchText))
            {
                // 没有搜索文本，显示所有连接
                filtered = _connections.ToList();
            }
            else
            {
                // 按连接名搜索（大小写不敏感）
                string searchLower = _searchText.ToLowerInvariant();
                filtered = _connections.Where(connection =>
                    connection.Name.ToLowerInvariant().Contains(searchLower)
                ).ToList();
            }

            // 更新过滤后的列表
            _filteredConnections.Clear();
            foreach (RemoteConnection connection in filtered)
            {
                _filteredConnections.Add(connection);
            }
        }

        /// <summary>
        /// 更新分组连接列表
        /// </summary>
        private void UpdateGroupedConnections(List<RemoteConnection> connections)
        {
            _groupedConnections.Clear();

            // 按分组名分组
            IEnumerable<IGrouping<string, RemoteConnection>> groups = connections
                .OrderBy(c => c.GroupName ?? string.Empty)
                .ThenBy(c => c.Name)
                .GroupBy(c => c.GroupName ?? string.Empty);

            foreach (IGrouping<string, RemoteConnection> group in groups)
            {
                ProjectGroupView groupView = new ProjectGroupView
                {
                    GroupName = string.IsNullOrEmpty(group.Key) ? "未分组" : group.Key
                };

                foreach (RemoteConnection connection in group)
                {
                    groupView.Connections.Add(connection);
                }

                _groupedConnections.Add(groupView);
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
        /// 添加连接
        /// </summary>
        public void AddConnection(string name, string host, string username, string password,
            int port = 3389, string? domain = null, string? groupName = null, string? description = null)
        {
            try
            {
                _plugin.AddConnection(name, host, username, password, port, domain, groupName, description);
                LoadConnections();
            }
            catch (Exception ex)
            {
                throw new Exception($"添加连接失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除连接
        /// </summary>
        public void DeleteConnection(RemoteConnection connection)
        {
            try
            {
                _plugin.RemoveConnection(connection.Id);
                LoadConnections();
            }
            catch (Exception ex)
            {
                throw new Exception($"删除连接失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 连接到远程桌面
        /// </summary>
        public void ConnectToRemote(RemoteConnection connection)
        {
            try
            {
                _plugin.ConnectToRemote(connection);
                // 刷新列表以更新最后连接时间
                LoadConnections();
            }
            catch (Exception ex)
            {
                throw new Exception($"连接失败: {ex.Message}", ex);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 分组视图模型
    /// </summary>
    public class ProjectGroupView
    {
        public string GroupName { get; set; } = string.Empty;
        public ObservableCollection<RemoteConnection> Connections { get; set; } = new();
    }
}

