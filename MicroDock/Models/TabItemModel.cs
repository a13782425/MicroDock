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
        public TabItemModel(string header, TabType tabType, bool isClosable = false)
        {
            _header = header;
            _tabType = tabType;
            _isClosable = isClosable;
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
    }
}

