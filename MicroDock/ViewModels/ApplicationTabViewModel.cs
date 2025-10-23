using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Windows.Input;
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
        var dialog = new Avalonia.Controls.OpenFileDialog
        {
            Title = "选择要添加的应用程序",
            AllowMultiple = false,
            Filters = new List<Avalonia.Controls.FileDialogFilter>
            {
                new() { Name = "Applications", Extensions = { "exe", "lnk" } },
                new() { Name = "All files", Extensions = { "*" } }
            }
        };

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var result = await dialog.ShowAsync(desktop.MainWindow);
            if (result != null && result.Length > 0)
            {
                string filePath = result[0];
                byte[]? iconBytes = IconService.TryExtractFileIconBytes(filePath);
                ApplicationDB app = new ApplicationDB
                {
                    Name = Path.GetFileNameWithoutExtension(filePath),
                    FilePath = filePath
                };

                DBContext.AddApplication(app, iconBytes);
                LoadApplications();
            }
        }
    }

    private void LaunchApplication(ApplicationDB app)
    {
        if (app != null)
        {
            IconService.TryStartProcess(app.FilePath);
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
