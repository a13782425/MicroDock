using ReactiveUI;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MicroNotePlugin.ViewModels;

public class FileTreeViewModel : ReactiveObject
{
    // 简单的文件节点模型
    public class FileNode : ReactiveObject
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public ObservableCollection<FileNode> Children { get; set; } = new();
        public bool IsDirectory => Directory.Exists(FullPath);
    }

    private ObservableCollection<FileNode> _rootNodes = new();
    public ObservableCollection<FileNode> RootNodes
    {
        get => _rootNodes;
        set => this.RaiseAndSetIfChanged(ref _rootNodes, value);
    }

    private string? _selectedFilePath;
    public string? SelectedFilePath
    {
        get => _selectedFilePath;
        set => this.RaiseAndSetIfChanged(ref _selectedFilePath, value);
    }

    public FileTreeViewModel()
    {
        // 默认根目录为插件数据文件夹（可自行修改）
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "MicroNoteData");
        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);
        LoadDirectory(basePath, null);
    }

    private void LoadDirectory(string path, FileNode? parent)
    {
        var node = new FileNode
        {
            Name = Path.GetFileName(path),
            FullPath = path
        };
        var directories = Directory.GetDirectories(path);
        foreach (var dir in directories)
        {
            LoadDirectory(dir, node);
        }
        var files = Directory.GetFiles(path, "*.md");
        foreach (var file in files)
        {
            node.Children.Add(new FileNode
            {
                Name = Path.GetFileName(file),
                FullPath = file
            });
        }
        if (parent == null)
        {
            RootNodes.Add(node);
        }
        else
        {
            parent.Children.Add(node);
        }
    }
}
