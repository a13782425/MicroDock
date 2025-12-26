using Avalonia.Platform.Storage;
using MicroDock.Database;
using MicroDock.Extension;
using MicroDock.Model;
using MicroDock.Service;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;

namespace MicroDock.ViewModels;

public class ApplicationTabViewModel : ViewModelBase
{
    private readonly ObservableCollection<ApplicationDB> _applications;
    private ApplicationSortMode _currentSortMode;
    private bool _isSortAscending = true;
    public ApplicationTabViewModel()
    {
        // 加载用户偏好的排序设置
        _currentSortMode = (ApplicationSortMode)UserPreferenceKeys.ApplicationSortBy.GetInt((int)ApplicationSortMode.AddTime);
        _isSortAscending = UserPreferenceKeys.ApplicationSortAscending.GetBool(true);
        _applications = new ObservableCollection<ApplicationDB>(DBContext.GetApplications());
        AddApplicationCommand = ReactiveCommand.CreateFromTask(AddApplication);
        // 初始化排序命令
        SetSortModeCommand = ReactiveCommand.Create<ApplicationSortMode>(SetSortMode);
        SetSortAscendingCommand = ReactiveCommand.Create<bool>(SetSortAscending);
        // 监听数据库变化（简单实现）
        LoadApplications();
    }

    public ObservableCollection<ApplicationDB> Applications => _applications;

    public bool HasApplications => _applications.Count > 0;

    public ReactiveCommand<Unit, Unit> AddApplicationCommand { get; }
    // 命令
    public ReactiveCommand<ApplicationSortMode, Unit> SetSortModeCommand { get; }
    public ReactiveCommand<bool, Unit> SetSortAscendingCommand { get; }
    // 当前排序模式
    public ApplicationSortMode CurrentSortMode
    {
        get => _currentSortMode;
        private set
        {
            this.RaiseAndSetIfChanged(ref _currentSortMode, value);
            this.RaisePropertyChanged(nameof(IsSortByAddTime));
            this.RaisePropertyChanged(nameof(IsSortByType));
            this.RaisePropertyChanged(nameof(IsSortByName));
            this.RaisePropertyChanged(nameof(IsSortByUsage));
        }
    }

    // 是否升序
    public bool IsSortAscending
    {
        get => _isSortAscending;
        private set => this.RaiseAndSetIfChanged(ref _isSortAscending, value);
    }
    // 用于菜单打勾的只读属性
    public bool IsSortByAddTime => CurrentSortMode == ApplicationSortMode.AddTime;
    public bool IsSortByType => CurrentSortMode == ApplicationSortMode.Type;
    public bool IsSortByName => CurrentSortMode == ApplicationSortMode.Name;
    public bool IsSortByUsage => CurrentSortMode == ApplicationSortMode.Usage;

    private async System.Threading.Tasks.Task AddApplication()
    {
        // 使用新的 StorageProvider API
        if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow == null)
            return;

        IStorageProvider? storageProvider = desktop.MainWindow.StorageProvider;
        if (storageProvider == null)
            return;

        // 定义文件类型过滤器
        var filePickerFileTypes = new FilePickerFileType[]
        {
            new("Applications")
            {
                Patterns = new[] { "*.exe", "*.lnk" }
            },
            FilePickerFileTypes.All
        };

        var filePickerOptions = new FilePickerOpenOptions
        {
            Title = "选择要添加的应用程序",
            AllowMultiple = true,
            FileTypeFilter = filePickerFileTypes
        };

        IReadOnlyList<IStorageFile> result = await storageProvider.OpenFilePickerAsync(filePickerOptions);

        if (result.Count > 0)
        {
            foreach (IStorageFile file in result)
            {
                AddApplicationFromPath(file.Path.LocalPath);
            }
        }
    }

    public void AddApplicationFromPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        // 检查文件/文件夹是否存在
        bool isDirectory = Directory.Exists(filePath);
        bool isFile = File.Exists(filePath);

        if (!isDirectory && !isFile)
            return;

        // 提取图标
        byte[]? iconBytes = IconService.TryExtractFileIconBytes(filePath);

        // 创建应用记录
        string name = Path.GetFileName(filePath);

        ApplicationDB app = new ApplicationDB
        {
            Name = name,
            FilePath = filePath,
            Type = (int)GetFileType(filePath)
        };

        // 保存到数据库并刷新列表
        DBContext.AddApplication(app, iconBytes);
        LoadApplications();
    }

    public void RemoveApplication(ApplicationDB app)
    {
        if (app != null)
        {
            DBContext.DeleteApplication(app.Id);
            _applications.Remove(app);
            this.RaisePropertyChanged(nameof(HasApplications));
        }
    }

    public void RenameApplication(ApplicationDB app, string newName)
    {
        if (app != null && !string.IsNullOrWhiteSpace(newName))
        {
            app.Name = newName;
            DBContext.UpdateApplication(app);
        }
    }

    private void LoadApplications()
    {
        _applications.Clear();
        foreach (var app in DBContext.GetApplications())
        {
            if (app.AppType == FileType.Unknow)
            {
                app.Type = (int)GetFileType(app.FilePath);
                DBContext.UpdateApplication(app);
            }

            _applications.Add(app);
        }
        this.RaisePropertyChanged(nameof(HasApplications));
        ApplySort();
    }

    private void SetSortMode(ApplicationSortMode mode)
    {
        CurrentSortMode = mode;
        UserPreferenceKeys.ApplicationSortBy.Set((int)mode);
        ApplySort();
    }

    private void SetSortAscending(bool ascending)
    {
        IsSortAscending = ascending;
        UserPreferenceKeys.ApplicationSortAscending.Set(ascending);
        ApplySort();
    }

    private void ApplySort()
    {
        Comparison<ApplicationDB> comparison = CurrentSortMode switch
        {
            ApplicationSortMode.AddTime => (a, b) => a.Id.CompareTo(b.Id),
            ApplicationSortMode.Type => (a, b) => a.Type.CompareTo(b.Type),
            ApplicationSortMode.Name => (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase),
            ApplicationSortMode.Usage => (a, b) => a.UsageCount.CompareTo(b.UsageCount),
            _ => (a, b) => a.Id.CompareTo(b.Id)
        };

        if (!IsSortAscending)
        {
            var original = comparison;
            comparison = (a, b) => original(b, a); // 反转比较
        }

        _applications.Sort(comparison);
    }
}
