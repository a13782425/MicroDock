using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MicroDock.Database;
using MicroDock.Service;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Windows.Input;

namespace MicroDock.ViewModels;

public class ApplicationTabViewModel : ViewModelBase
{
    private readonly ObservableCollection<ApplicationDB> _applications;

    public ApplicationTabViewModel()
    {
        _applications = new ObservableCollection<ApplicationDB>(DBContext.GetApplications());
        AddApplicationCommand = ReactiveCommand.CreateFromTask(AddApplication);

        // 监听数据库变化（简单实现）
        LoadApplications();
    }

    public ObservableCollection<ApplicationDB> Applications => _applications;

    public bool HasApplications => _applications.Count > 0;

    public ReactiveCommand<Unit, Unit> AddApplicationCommand { get; }

    public ReactiveCommand<ApplicationDB, Unit> LaunchCommand { get; }

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
            FilePath = filePath
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
            _applications.Add(app);
        }
        this.RaisePropertyChanged(nameof(HasApplications));
    }
}
