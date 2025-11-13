using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using UnityProjectPlugin.Models;

namespace UnityProjectPlugin.ViewModels
{
    /// <summary>
    /// Unity 项目列表标签页 ViewModel
    /// </summary>
    public class UnityProjectTabViewModel
    {
        private readonly UnityProjectPlugin _plugin;
        private string _searchText = string.Empty;
        private UnityProject? _selectedProject;

        public UnityProjectTabViewModel(UnityProjectPlugin plugin)
        {
            _plugin = plugin;
            Projects = new ObservableCollection<UnityProject>();
            
            // 初始化命令
            AddProjectCommand = new RelayCommand(AddProject);
            OpenProjectCommand = new RelayCommand(OpenProject, CanOpenProject);
            DeleteProjectCommand = new RelayCommand(DeleteProject, CanDeleteProject);
            RefreshCommand = new RelayCommand(Refresh);

            // 加载项目
            Refresh();
        }

        /// <summary>
        /// 项目列表
        /// </summary>
        public ObservableCollection<UnityProject> Projects { get; }

        /// <summary>
        /// 选中的项目
        /// </summary>
        public UnityProject? SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                OnPropertyChanged(nameof(SelectedProject));
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
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterProjects();
            }
        }

        /// <summary>
        /// 添加项目命令
        /// </summary>
        public ICommand AddProjectCommand { get; }

        /// <summary>
        /// 打开项目命令
        /// </summary>
        public ICommand OpenProjectCommand { get; }

        /// <summary>
        /// 删除项目命令
        /// </summary>
        public ICommand DeleteProjectCommand { get; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// 刷新项目列表
        /// </summary>
        public void Refresh()
        {
            Projects.Clear();
            var projects = _plugin.GetProjects();
            
            foreach (var project in projects.OrderByDescending(p => p.LastOpened))
            {
                Projects.Add(project);
            }
        }

        /// <summary>
        /// 过滤项目
        /// </summary>
        private void FilterProjects()
        {
            var allProjects = _plugin.GetProjects();
            Projects.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? allProjects
                : allProjects.Where(p => 
                    p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    p.Path.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var project in filtered.OrderByDescending(p => p.LastOpened))
            {
                Projects.Add(project);
            }
        }

        /// <summary>
        /// 添加项目
        /// </summary>
        private void AddProject()
        {
            // 这个方法需要在 View 层处理文件夹选择对话框
            // 通过事件或委托通知 View 层
            AddProjectRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 打开项目
        /// </summary>
        private async void OpenProject()
        {
            if (SelectedProject == null) return;

            try
            {
                var result = await _plugin.OpenUnityProject(SelectedProject.Path);
                System.Diagnostics.Debug.WriteLine($"打开项目结果: {result}");
                Refresh(); // 刷新以更新最后打开时间
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开项目失败: {SelectedProject.Name} - {ex.Message}");
            }
        }

        /// <summary>
        /// 删除项目
        /// </summary>
        private void DeleteProject()
        {
            if (SelectedProject == null) return;

            try
            {
                _plugin.RemoveProject(SelectedProject.Path);
                Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除项目失败: {SelectedProject.Name} - {ex.Message}");
            }
        }

        /// <summary>
        /// 是否可以打开项目
        /// </summary>
        private bool CanOpenProject() => SelectedProject != null;

        /// <summary>
        /// 是否可以删除项目
        /// </summary>
        private bool CanDeleteProject() => SelectedProject != null;

        /// <summary>
        /// 添加项目请求事件
        /// </summary>
        public event EventHandler? AddProjectRequested;

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event EventHandler<string>? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, propertyName);
        }

        /// <summary>
        /// 简单的命令实现
        /// </summary>
        private class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool>? _canExecute;

            public RelayCommand(Action execute, Func<bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

            public void Execute(object? parameter) => _execute();

            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

