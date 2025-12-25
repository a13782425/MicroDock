using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using MicroDock.Plugin;
using System;
using UnityProjectPlugin.Models;
using UnityProjectPlugin.Services;
using UnityProjectPlugin.ViewModels;

namespace UnityProjectPlugin.Views
{
    /// <summary>
    /// Unity 项目列表标签页视图
    /// </summary>
    public partial class UnityProjectTabView : UserControl, IMicroTab
    {
        private readonly UnityProjectPlugin _plugin;
        private readonly UnityProjectTabViewModel _viewModel;

        private Border? _overlayMask;
        private Border? _editPanelContainer;
        private ProjectEditPanel? _editPanel;
        private bool _isPanelOpen = false;

        /// <summary>
        /// 公开插件实例供子控件使用
        /// </summary>
        public UnityProjectPlugin Plugin => _plugin;

        /// <summary>
        /// 公开 ViewModel 供子控件使用
        /// </summary>
        public UnityProjectTabViewModel ViewModel => _viewModel;


        public UnityProjectTabView(UnityProjectPlugin plugin)
        {
            _plugin = plugin;

            // 创建文件选择服务
            var filePickerService = new FilePickerService(this);
            _viewModel = new UnityProjectTabViewModel(plugin, filePickerService);

            InitializeComponent(true);
            // 设置 DataContext
            DataContext = _viewModel;

            // 初始化控件引用
            _overlayMask = this.FindControl<Border>("OverlayMask");
            _editPanelContainer = this.FindControl<Border>("EditPanelContainer");
            _editPanel = this.FindControl<ProjectEditPanel>("EditPanel");

            // 绑定遮罩层点击事件
            if (_overlayMask != null)
            {
                _overlayMask.PointerPressed += OnOverlayMaskPressed;
            }

            // 设置编辑面板的 DataContext
            if (_editPanel != null)
            {
                _editPanel.DataContext = _viewModel.EditPanelViewModel;
            }

            // 监听 ViewModel 的面板状态变化
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UnityProjectTabViewModel.IsEditPanelOpen))
            {
                if (_viewModel.IsEditPanelOpen)
                {
                    ShowEditPanel();
                }
                else
                {
                    HideEditPanel();
                }
            }
        }

        /// <summary>
        /// 显示编辑面板（带动画）
        /// </summary>
        public void ShowEditPanel()
        {
            if (_isPanelOpen) return;
            _isPanelOpen = true;

            // 显示遮罩
            if (_overlayMask != null)
            {
                _overlayMask.IsVisible = true;
                _overlayMask.Opacity = 1;
            }

            // 显示面板并播放滑入动画
            if (_editPanelContainer != null)
            {
                _editPanelContainer.IsVisible = true;
                _editPanelContainer.RenderTransform = new TranslateTransform(0, 0);
            }
        }

        /// <summary>
        /// 隐藏编辑面板（带动画）
        /// </summary>
        public void HideEditPanel()
        {
            if (!_isPanelOpen) return;
            _isPanelOpen = false;

            // 淡出遮罩
            if (_overlayMask != null)
            {
                _overlayMask.Opacity = 0;
                // 延迟隐藏以等待动画完成
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(200);
                    if (!_isPanelOpen && _overlayMask != null)
                    {
                        _overlayMask.IsVisible = false;
                    }
                });
            }

            // 滑出面板
            if (_editPanelContainer != null)
            {
                _editPanelContainer.RenderTransform = new TranslateTransform(400, 0);
                // 延迟隐藏以等待动画完成
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(250);
                    if (!_isPanelOpen && _editPanelContainer != null)
                    {
                        _editPanelContainer.IsVisible = false;
                    }
                });
            }
        }

        /// <summary>
        /// 打开编辑面板（供外部调用）
        /// </summary>
        public void OpenEditPanel(UnityProject project)
        {
            _viewModel.OpenEditPanel(project);
        }

        /// <summary>
        /// 关闭编辑面板
        /// </summary>
        public void CloseEditPanel()
        {
            _viewModel.IsEditPanelOpen = false;
        }

        /// <summary>
        /// 遮罩层点击 - 关闭面板
        /// </summary>
        private void OnOverlayMaskPressed(object? sender, PointerPressedEventArgs e)
        {
            CloseEditPanel();
        }

        public string TabName => "Unity项目";

        public object IconSymbol => Symbol.GamesFilled;

        /// <summary>
        /// 刷新项目列表（供卡片调用）
        /// </summary>
        public void RefreshProjects()
        {
            _viewModel.LoadProjects();
        }
    }
}
