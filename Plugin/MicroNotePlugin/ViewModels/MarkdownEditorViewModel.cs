using ReactiveUI;
using System.Reactive.Linq;
using MicroNotePlugin.Services;

namespace MicroNotePlugin.ViewModels;

public class MarkdownEditorViewModel : ReactiveObject
{
    private string _markdownText = string.Empty;
    public string MarkdownText
    {
        get => _markdownText;
        set => this.RaiseAndSetIfChanged(ref _markdownText, value);
    }

    private string _htmlPreview = string.Empty;
    public string HtmlPreview
    {
        get => _htmlPreview;
        private set => this.RaiseAndSetIfChanged(ref _htmlPreview, value);
    }

    public MarkdownEditorViewModel()
    {
        // 当 MarkdownText 变化时，更新 HtmlPreview
        this.WhenAnyValue(vm => vm.MarkdownText)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(text =>
            {
                HtmlPreview = MarkdownService.RenderHtml(text);
            });
    }
}
