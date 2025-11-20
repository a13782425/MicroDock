using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MicroDock.ViewModel;
using MicroDock.Plugin;

namespace MicroDock.Views
{
    public partial class SettingsTabView : UserControl
    {
        public SettingsTabView()
        {
            InitializeComponent();
            // 设置独立的 ViewModel
            DataContext = new SettingsTabViewModel();
        }
        
        /// <summary>
        /// 获取设置 ViewModel
        /// </summary>
        public SettingsTabViewModel? ViewModel => DataContext as SettingsTabViewModel;
      
        /// <summary>
        /// 插件唯一名点击事件 - 复制到剪切板
        /// </summary>
        private async void PluginUniqueName_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is PluginSettingItem pluginItem)
            {
                await SettingsTabViewModel.CopyToClipboardAsync(pluginItem.UniqueName, "插件唯一名");
                e.Handled = true;
            }
        }
        
        /// <summary>
        /// 工具名点击事件 - 复制到剪切板
        /// </summary>
        private async void ToolName_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is ToolInfo toolInfo)
            {
                await SettingsTabViewModel.CopyToClipboardAsync(toolInfo.Name, "工具名");
                e.Handled = true;
            }
        }

        #region Drag and Drop Sorting

        private async void NavigationItem_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Only allow dragging if the pointer pressed event happened on the Drag Handle (TextBlock with ≡)
            // Or we can allow dragging from anywhere on the border if that's preferred, but typically handle is better or specific areas.
            // Let's check if the source is the Drag Handle TextBlock or we can just allow dragging from the whole item but maybe exclude textbox/checkbox interactions.
            
            var border = sender as Border;
            if (border?.DataContext is NavigationTabSettingItem item)
            {
                // Ensure we are not dragging when interacting with inputs
                var source = e.Source as Control;
                if (source is TextBox || source is CheckBox) return;

                var dragData = new DataObject();
                dragData.Set("NavigationTabSettingItem", item);

                await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
                e.Handled = true;
            }
        }

        private void NavigationItem_DragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains("NavigationTabSettingItem"))
            {
                e.DragEffects = DragDropEffects.Move;
                e.Handled = true;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        private void NavigationItem_Drop(object? sender, DragEventArgs e)
        {
            if (sender is Border border && 
                border.DataContext is NavigationTabSettingItem targetItem &&
                e.Data.Contains("NavigationTabSettingItem"))
            {
                var sourceItem = e.Data.Get("NavigationTabSettingItem") as NavigationTabSettingItem;
                if (sourceItem != null && sourceItem != targetItem && ViewModel != null)
                {
                    ViewModel.MoveTab(sourceItem, targetItem);
                    e.Handled = true;
                }
            }
        }

        #endregion
    }
}

