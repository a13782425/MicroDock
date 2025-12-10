using ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using MicroNotePlugin.Services;

namespace MicroNotePlugin.ViewModels;

/// <summary>
/// 随手记主页面 ViewModel
/// </summary>
public class MicroNoteTabViewModel : ReactiveObject
{
    public FileTreeViewModel FileTree { get; }
    public MarkdownEditorViewModel Editor { get; }

    public MicroNoteTabViewModel(IServiceProvider serviceProvider)
    {
        var noteRepository = serviceProvider.GetRequiredService<INoteRepository>();
        var folderRepository = serviceProvider.GetRequiredService<IFolderRepository>();
        var tagRepository = serviceProvider.GetRequiredService<ITagRepository>();
        var searchService = serviceProvider.GetRequiredService<ISearchService>();
        var versionService = serviceProvider.GetRequiredService<IVersionService>();
        var markdownService = serviceProvider.GetRequiredService<MarkdownService>();

        FileTree = new FileTreeViewModel(noteRepository, folderRepository, tagRepository, searchService);
        Editor = new MarkdownEditorViewModel(noteRepository, versionService, markdownService);
    }

    /// <summary>
    /// 打开选中的文件
    /// </summary>
    public async Task OpenSelectedFileAsync()
    {
        if (FileTree.SelectedNode is { IsFile: true } node)
        {
            await FileTree.RecordFileOpenAsync(node);
            await Editor.LoadNoteAsync(node);
        }
    }

    /// <summary>
    /// 通过节点打开文件
    /// </summary>
    public async Task OpenFileAsync(FileNodeViewModel node)
    {
        if (!node.IsFile) return;
        await FileTree.RecordFileOpenAsync(node);
        await Editor.LoadNoteAsync(node);
    }

    /// <summary>
    /// 通过 ID 打开文件
    /// </summary>
    public async Task OpenFileAsync(string noteId)
    {
        await Editor.LoadNoteAsync(noteId);
    }
}
