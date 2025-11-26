using ReactiveUI;
using MicroNotePlugin.Services;

namespace MicroNotePlugin.ViewModels;

/// <summary>
/// 随手记主页面 ViewModel
/// </summary>
public class MicroNoteTabViewModel : ReactiveObject
{
    public FileTreeViewModel FileTree { get; }
    public MarkdownEditorViewModel Editor { get; }

    public MicroNoteTabViewModel(
        NoteFileService fileService, 
        MetadataService metadataService,
        MarkdownService markdownService)
    {
        FileTree = new FileTreeViewModel(fileService, metadataService);
        Editor = new MarkdownEditorViewModel(fileService, markdownService);
    }

    /// <summary>
    /// 打开选中的文件
    /// </summary>
    public void OpenSelectedFile()
    {
        if (FileTree.SelectedNode is { IsFile: true } node)
        {
            FileTree.RecordFileOpen(node);
            Editor.LoadFile(node);
        }
    }

    /// <summary>
    /// 通过节点打开文件
    /// </summary>
    public void OpenFile(FileNodeViewModel node)
    {
        if (!node.IsFile) return;
        FileTree.RecordFileOpen(node);
        Editor.LoadFile(node);
    }

    /// <summary>
    /// 通过 Hash 打开文件
    /// </summary>
    public void OpenFile(string hash, string fileName)
    {
        Editor.LoadFile(hash, fileName);
    }
}
