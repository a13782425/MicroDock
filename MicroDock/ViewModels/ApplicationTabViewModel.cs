using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Windows.Input;
using Avalonia.Controls;
using MicroDock.Database;
using MicroDock.Services;
using ReactiveUI;

namespace MicroDock.ViewModels;

public class ApplicationTabViewModel : ViewModelBase
{
    private readonly ObservableCollection<ApplicationDB> _applications;

    public ApplicationTabViewModel()
    {
        _applications = new ObservableCollection<ApplicationDB>(DBContext.GetApplications());
        AddApplicationCommand = ReactiveCommand.CreateFromTask(AddApplication);
        LaunchCommand = ReactiveCommand.Create<ApplicationDB>(LaunchApplication);

        // 监听数据库变化（简单实现）
        LoadApplications();
    }

    public ObservableCollection<ApplicationDB> Applications => _applications;

    public bool HasApplications => _applications.Count > 0;

    public ReactiveCommand<Unit, Unit> AddApplicationCommand { get; }

    public ReactiveCommand<ApplicationDB, Unit> LaunchCommand { get; }

    private async System.Threading.Tasks.Task AddApplication()
    {
        OpenFileDialog dialog = new OpenFileDialog
        {
            Title = "选择要添加的应用程序",
            AllowMultiple = true,
            Filters = new List<Avalonia.Controls.FileDialogFilter>
            {
                new() { Name = "Applications", Extensions = { "exe", "lnk" } },
                new() { Name = "All files", Extensions = { "*" } }
            }
        };

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            string[]? result = await dialog.ShowAsync(desktop.MainWindow);
            if (result != null && result.Length > 0)
            {
                foreach (string filePath in result)
                {
                    AddApplicationFromPath(filePath);
                }
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
        string name = isDirectory 
            ? Path.GetFileName(filePath) 
            : Path.GetFileNameWithoutExtension(filePath);
            
        ApplicationDB app = new ApplicationDB
        {
            Name = name,
            FilePath = filePath
        };
        
        // 保存到数据库并刷新列表
        DBContext.AddApplication(app, iconBytes);
        LoadApplications();
    }

    private void LaunchApplication(ApplicationDB app)
    {
        if (app != null)
        {
            IconService.TryStartProcess(app.FilePath);
        }
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
