using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;

namespace MicroNotePlugin.ViewModels;

public class MicroNoteTabViewModel : ReactiveObject
{
    public FileTreeViewModel FileTree { get; }
    public MarkdownEditorViewModel Editor { get; }

    public MicroNoteTabViewModel(MicroNotePlugin plugin)
    {
        // 初始化子 ViewModel
        FileTree = new FileTreeViewModel();
        Editor = new MarkdownEditorViewModel();

        // 当选中文件变化时，加载内容
        this.WhenAnyValue(vm => vm.FileTree.SelectedFilePath)
            .Where(path => !string.IsNullOrEmpty(path))
            .Subscribe(async path =>
            {
                var content = await Services.FileService.Instance.ReadFileAsync(path);
                Editor.MarkdownText = content ?? string.Empty;
            });
    }
}
