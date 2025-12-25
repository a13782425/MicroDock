using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using UnityProjectPlugin.Helpers;
using UnityProjectPlugin.Models;

namespace UnityProjectPlugin.ViewModels
{
    /// <summary>
    /// 项目编辑面板 ViewModel
    /// </summary>
    public class ProjectEditPanelViewModel : INotifyPropertyChanged
    {
        private readonly UnityProjectPlugin _plugin;
        private readonly Action _onSaveCallback;
        private readonly Action _onCloseCallback;

        private UnityProject? _project;
        private string _projectName = string.Empty;
        private string? _selectedGroup;
        private UnityVersion? _selectedVersion;
        private UnityPlatform? _selectedPlatform;
        private bool _isPanelOpen;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand SelectPlatformCommand { get; }

        public ProjectEditPanelViewModel(UnityProjectPlugin plugin, Action onSaveCallback, Action onCloseCallback)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _onSaveCallback = onSaveCallback;
            _onCloseCallback = onCloseCallback;

            SaveCommand = new AsyncRelayCommand(SaveAsync);
            CancelCommand = new RelayCommand(_ => Cancel());
            CloseCommand = new RelayCommand(_ => Close());
            SelectPlatformCommand = new RelayCommand(SelectPlatform);

            // 初始化可选列表
            AvailableGroups = new ObservableCollection<string>();
            AvailableVersions = new ObservableCollection<UnityVersion>();
            AvailablePlatforms = new ObservableCollection<UnityPlatform>();
            InstalledPlatforms = new ObservableCollection<UnityPlatform>();
        }

        #region Properties

        /// <summary>
        /// 当前编辑的项目
        /// </summary>
        public UnityProject? Project
        {
            get => _project;
            private set
            {
                if (_project != value)
                {
                    _project = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 项目名称（可编辑）
        /// </summary>
        public string ProjectName
        {
            get => _projectName;
            set
            {
                if (_projectName != value)
                {
                    _projectName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 选中的分组
        /// </summary>
        public string? SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                if (_selectedGroup != value)
                {
                    _selectedGroup = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 可选分组列表
        /// </summary>
        public ObservableCollection<string> AvailableGroups { get; }

        /// <summary>
        /// 选中的 Unity 版本
        /// </summary>
        public UnityVersion? SelectedVersion
        {
            get => _selectedVersion;
            set
            {
                if (_selectedVersion != value)
                {
                    _selectedVersion = value;
                    OnPropertyChanged();
                    // 版本变更时刷新平台列表
                    RefreshPlatforms();
                }
            }
        }

        /// <summary>
        /// 可选 Unity 版本列表
        /// </summary>
        public ObservableCollection<UnityVersion> AvailableVersions { get; }

        /// <summary>
        /// 选中的目标平台
        /// </summary>
        public UnityPlatform? SelectedPlatform
        {
            get => _selectedPlatform;
            set
            {
                if (_selectedPlatform != value)
                {
                    _selectedPlatform = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 可选平台列表（带安装状态）
        /// </summary>
        public ObservableCollection<UnityPlatform> AvailablePlatforms { get; }

        /// <summary>
        /// 已安装的平台列表（用于下拉选择）
        /// </summary>
        public ObservableCollection<UnityPlatform> InstalledPlatforms { get; }

        /// <summary>
        /// 面板是否打开
        /// </summary>
        public bool IsPanelOpen
        {
            get => _isPanelOpen;
            set
            {
                if (_isPanelOpen != value)
                {
                    _isPanelOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// 打开面板并加载项目数据
        /// </summary>
        public void OpenPanel(UnityProject project)
        {
            Project = project;

            // 加载项目当前值
            ProjectName = project.Name;
            if (string.IsNullOrWhiteSpace(project.GroupName))
                SelectedGroup = "未分组";
            else
                SelectedGroup = project.GroupName;

            // 加载可选分组
            AvailableGroups.Clear();
            AvailableGroups.Add(string.Empty); // 无分组选项
            foreach (var group in _plugin.GetGroups())
            {
                AvailableGroups.Add(group.Name);
            }

            // 加载可选版本
            AvailableVersions.Clear();
            foreach (var version in _plugin.GetVersions())
            {
                AvailableVersions.Add(version);
            }

            // 设置当前版本
            SelectedVersion = AvailableVersions.FirstOrDefault(v => v.Version == project.UnityVersion)
                              ?? AvailableVersions.FirstOrDefault();

            // 刷新平台列表
            RefreshPlatforms();

            // 设置当前平台
            if (!string.IsNullOrEmpty(project.TargetPlatform))
            {
                SelectedPlatform = InstalledPlatforms.FirstOrDefault(p => p.Id == project.TargetPlatform)
                                   ?? InstalledPlatforms.FirstOrDefault();
            }
            else
            {
                SelectedPlatform = InstalledPlatforms.FirstOrDefault();
            }

            IsPanelOpen = true;
        }

        /// <summary>
        /// 刷新平台列表（根据当前选择的版本）
        /// </summary>
        private void RefreshPlatforms()
        {
            AvailablePlatforms.Clear();
            InstalledPlatforms.Clear();

            string? editorPath = SelectedVersion?.EditorPath;
            var platforms = string.IsNullOrEmpty(editorPath)
                ? PlatformDetector.GetAllPlatforms()
                : PlatformDetector.DetectInstalledPlatforms(editorPath);

            foreach (var platform in platforms)
            {
                AvailablePlatforms.Add(platform);
                // 只添加已安装的平台到 InstalledPlatforms
                if (platform.IsInstalled)
                {
                    InstalledPlatforms.Add(platform);
                }
            }

            // 尝试保持之前的选择
            if (_selectedPlatform != null)
            {
                var previousPlatform = InstalledPlatforms.FirstOrDefault(p => p.Id == _selectedPlatform.Id);
                if (previousPlatform != null)
                {
                    SelectedPlatform = previousPlatform;
                }
                else
                {
                    SelectedPlatform = InstalledPlatforms.FirstOrDefault();
                }
            }
        }

        /// <summary>
        /// 选择平台
        /// </summary>
        private void SelectPlatform(object? parameter)
        {
            if (parameter is UnityPlatform platform && platform.IsInstalled)
            {
                SelectedPlatform = platform;
            }
        }

        /// <summary>
        /// 保存修改
        /// </summary>
        private async Task SaveAsync(object? parameter)
        {
            if (Project == null) return;

            try
            {
                // 更新项目名称和分组
                await _plugin.UpdateProjectAsync(Project.Path, ProjectName, SelectedGroup);

                // 更新 Unity 版本（包括 ProjectVersion.txt）
                if (SelectedVersion != null && SelectedVersion.Version != Project.UnityVersion)
                {
                    await UpdateProjectVersionFileAsync(Project.Path, SelectedVersion.Version);
                    Project.UnityVersion = SelectedVersion.Version;
                }

                // 更新目标平台
                if (SelectedPlatform != null)
                {
                    Project.TargetPlatform = SelectedPlatform.Id;
                }

                // 保存项目数据
                await _plugin.SaveProjectChangesAsync();

                _onSaveCallback?.Invoke();
                Close();
            }
            catch (Exception ex)
            {
                _plugin.Context?.ShowInAppNotification(
                    "保存失败",
                    ex.Message,
                    MicroDock.Plugin.NotificationType.Error);
            }
        }

        /// <summary>
        /// 更新项目的 ProjectVersion.txt 文件
        /// </summary>
        private async Task UpdateProjectVersionFileAsync(string projectPath, string newVersion)
        {
            try
            {
                string versionFilePath = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");

                if (!File.Exists(versionFilePath))
                {
                    _plugin.Context?.LogWarning($"ProjectVersion.txt 不存在: {versionFilePath}");
                    return;
                }

                string[] lines = await File.ReadAllLinesAsync(versionFilePath);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("m_EditorVersion:"))
                    {
                        lines[i] = $"m_EditorVersion: {newVersion}";
                        break;
                    }
                }

                await File.WriteAllLinesAsync(versionFilePath, lines);
                _plugin.Context?.LogInfo($"已更新 ProjectVersion.txt: {newVersion}");
            }
            catch (Exception ex)
            {
                _plugin.Context?.LogError($"更新 ProjectVersion.txt 失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 取消编辑
        /// </summary>
        private void Cancel()
        {
            Close();
        }

        /// <summary>
        /// 关闭面板
        /// </summary>
        private void Close()
        {
            IsPanelOpen = false;
            _onCloseCallback?.Invoke();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
