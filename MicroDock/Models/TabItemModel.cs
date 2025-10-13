using Avalonia.Controls;
using ReactiveUI;

namespace MicroDock.Models
{
    public class TabItemModel : ReactiveObject
    {
        private string _header;
        private object? _content;
        private bool _isClosable;
        private TabType _tabType;
        private Control? _pluginControl;
        public TabItemModel(string header, TabType tabType, bool isClosable = true, Control? pluginControl = null)
        {
            _header = header;
            _tabType = tabType;
            _isClosable = isClosable;
            _pluginControl = pluginControl;
        }

        /// <summary>
        /// 页签标题
        /// </summary>
        public string Header
        {
            get => _header;
            set => this.RaiseAndSetIfChanged(ref _header, value);
        }

        /// <summary>
        /// 页签内容
        /// </summary>
        public object? Content
        {
            get => _content;
            set => this.RaiseAndSetIfChanged(ref _content, value);
        }

        /// <summary>
        /// 是否可以关闭
        /// </summary>
        public bool IsClosable
        {
            get => _isClosable;
            set => this.RaiseAndSetIfChanged(ref _isClosable, value);
        }

        /// <summary>
        /// 页签类型
        /// </summary>
        public TabType TabType
        {
            get => _tabType;
            set => this.RaiseAndSetIfChanged(ref _tabType, value);
        }
        /// <summary>
        /// 插件控件（仅Plugin类型使用）
        /// </summary>
        public Control? PluginControl
        {
            get => _pluginControl;
            set => this.RaiseAndSetIfChanged(ref _pluginControl, value);
        }
    }
}

