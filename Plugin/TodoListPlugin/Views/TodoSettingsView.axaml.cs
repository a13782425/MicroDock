using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using System;
using TodoListPlugin.Helpers;
using TodoListPlugin.Models;
using TodoListPlugin.ViewModels;
using TodoListPlugin.Views.Dialogs;

namespace TodoListPlugin.Views
{
    public partial class TodoSettingsView : UserControl
    {
        private TodoListMainViewModel? _viewModel;

        public TodoSettingsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            _viewModel = DataContext as TodoListMainViewModel;
            RefreshLists();
        }

        /// <summary>
        /// 刷新数据（公共方法，供外部调用）
        /// </summary>
        public void RefreshData()
        {
            _viewModel = DataContext as TodoListMainViewModel;
            RefreshLists();
        }

        private void RefreshLists()
        {
            if (_viewModel == null) return;

            StatusColumnsList.ItemsSource = _viewModel.StatusColumns;
            PrioritiesList.ItemsSource = _viewModel.Priorities;
            TagsList.ItemsSource = _viewModel.Tags;
            FieldTemplatesList.ItemsSource = _viewModel.FieldTemplates;
        }

        /// <summary>
        /// 返回按钮点击
        /// </summary>
        private void OnBackClick(object? sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        #region 状态列管理

        private async void OnAddStatusClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var content = new EditStatusContent();
            var result = await DialogHelper.ShowContentDialogWithValidationAsync(
                "添加状态列",
                content,
                () => content.IsValid);

            if (result == ContentDialogResult.Primary)
            {
                var status = new StatusColumn
                {
                    Name = content.StatusName,
                    Color = content.SelectedColor,
                    IsDefault = content.IsDefault
                };
                _viewModel.AddStatusColumn(status);
                RefreshLists();
            }
        }

        private async void OnEditStatusClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is not Button button || button.Tag is not StatusColumn status) return;

            var content = new EditStatusContent();
            content.LoadStatus(status);
            
            var result = await DialogHelper.ShowContentDialogWithValidationAsync(
                "编辑状态列",
                content,
                () => content.IsValid);

            if (result == ContentDialogResult.Primary)
            {
                status.Name = content.StatusName;
                status.Color = content.SelectedColor;
                status.IsDefault = content.IsDefault;
                _viewModel.UpdateStatusColumn(status);
                RefreshLists();
            }
        }

        private async void OnDeleteStatusClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is not Button button || button.Tag is not StatusColumn status) return;

            if (status.IsDefault)
            {
                await DialogHelper.ShowMessageAsync("无法删除", "不能删除默认状态列");
                return;
            }

            var confirm = await DialogHelper.ShowDeleteConfirmAsync(status.Name);
            if (confirm)
            {
                _viewModel.DeleteStatusColumn(status.Id);
                RefreshLists();
            }
        }

        #endregion

        #region 优先级管理

        private async void OnAddPriorityClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var content = new EditPriorityContent();
            var result = await DialogHelper.ShowContentDialogWithValidationAsync(
                "添加优先级",
                content,
                () => content.IsValid);

            if (result == ContentDialogResult.Primary)
            {
                var priority = new PriorityGroup
                {
                    Name = content.PriorityName,
                    Color = content.SelectedColor,
                    Level = content.Level
                };
                _viewModel.AddPriority(priority);
                RefreshLists();
            }
        }

        private async void OnEditPriorityClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is not Button button || button.Tag is not PriorityGroup priority) return;

            var content = new EditPriorityContent();
            content.LoadPriority(priority);
            
            var result = await DialogHelper.ShowContentDialogWithValidationAsync(
                "编辑优先级",
                content,
                () => content.IsValid);

            if (result == ContentDialogResult.Primary)
            {
                priority.Name = content.PriorityName;
                priority.Color = content.SelectedColor;
                priority.Level = content.Level;
                _viewModel.UpdatePriority(priority);
                RefreshLists();
            }
        }

        private async void OnDeletePriorityClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is not Button button || button.Tag is not PriorityGroup priority) return;

            var confirm = await DialogHelper.ShowDeleteConfirmAsync(priority.Name);
            if (confirm)
            {
                _viewModel.DeletePriority(priority.Name);
                RefreshLists();
            }
        }

        #endregion

        #region 标签管理

        private async void OnAddTagClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var content = new EditTagContent();
            var result = await DialogHelper.ShowContentDialogWithValidationAsync(
                "添加标签",
                content,
                () => content.IsValid);

            if (result == ContentDialogResult.Primary)
            {
                var tag = new TagGroup
                {
                    Name = content.TagName,
                    Color = content.SelectedColor
                };
                _viewModel.AddTag(tag);
                RefreshLists();
            }
        }

        private async void OnEditTagClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is not Button button || button.Tag is not TagGroup tag) return;

            var content = new EditTagContent();
            content.LoadTag(tag);
            
            var result = await DialogHelper.ShowContentDialogWithValidationAsync(
                "编辑标签",
                content,
                () => content.IsValid);

            if (result == ContentDialogResult.Primary)
            {
                tag.Name = content.TagName;
                tag.Color = content.SelectedColor;
                _viewModel.UpdateTag(tag);
                RefreshLists();
            }
        }

        private async void OnDeleteTagClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is not Button button || button.Tag is not TagGroup tag) return;

            var confirm = await DialogHelper.ShowDeleteConfirmAsync(tag.Name);
            if (confirm)
            {
                _viewModel.DeleteTag(tag.Name);
                RefreshLists();
            }
        }

        #endregion

        #region 字段模板管理

        private async void OnAddFieldClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var content = new EditFieldContent();
            var result = await DialogHelper.ShowContentDialogWithValidationAsync(
                "添加自定义字段",
                content,
                () => content.IsValid);

            if (result == ContentDialogResult.Primary)
            {
                var field = new CustomFieldTemplate
                {
                    Name = content.FieldName,
                    FieldType = content.FieldType,
                    DefaultValue = content.DefaultValue,
                    Required = content.Required,
                    ShowOnCard = content.ShowOnCard
                };
                _viewModel.AddFieldTemplate(field);
                RefreshLists();
            }
        }

        private async void OnEditFieldClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is not Button button || button.Tag is not CustomFieldTemplate field) return;

            var content = new EditFieldContent();
            content.LoadField(field);
            
            var result = await DialogHelper.ShowContentDialogWithValidationAsync(
                "编辑自定义字段",
                content,
                () => content.IsValid);

            if (result == ContentDialogResult.Primary)
            {
                field.Name = content.FieldName;
                field.FieldType = content.FieldType;
                field.DefaultValue = content.DefaultValue;
                field.Required = content.Required;
                field.ShowOnCard = content.ShowOnCard;
                _viewModel.UpdateFieldTemplate(field);
                RefreshLists();
            }
        }

        private async void OnDeleteFieldClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is not Button button || button.Tag is not CustomFieldTemplate field) return;

            var confirm = await DialogHelper.ShowDeleteConfirmAsync(field.Name);
            if (confirm)
            {
                _viewModel.DeleteFieldTemplate(field.Id);
                RefreshLists();
            }
        }

        /// <summary>
        /// 字段必填状态变更
        /// </summary>
        private void OnFieldRequiredChanged(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is not CheckBox checkBox || checkBox.Tag is not CustomFieldTemplate field) return;

            field.Required = checkBox.IsChecked == true;
            _viewModel.UpdateFieldTemplate(field);
        }

        /// <summary>
        /// 字段显示状态变更
        /// </summary>
        private void OnFieldShowOnCardChanged(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is not CheckBox checkBox || checkBox.Tag is not CustomFieldTemplate field) return;

            field.ShowOnCard = checkBox.IsChecked == true;
            _viewModel.UpdateFieldTemplate(field);
        }

        #endregion

        private Window? GetWindow()
        {
            return this.FindAncestorOfType<Window>();
        }

        /// <summary>
        /// 返回请求事件
        /// </summary>
        public event EventHandler? BackRequested;
    }
}
