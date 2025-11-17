using Avalonia.Controls;
using ClaudeSwitchPlugin.Services;
using MicroDock.Plugin;
using ReactiveUI;
using System;
using System.Reactive;
using System.Windows.Input;

namespace ClaudeSwitchPlugin.ViewModels
{
    /// <summary>
    /// Claude 配置切换主标签页 ViewModel
    /// </summary>
    public class ClaudeSwitchTabViewModel : ReactiveObject, IMicroTab
    {
        private readonly ConfigurationService _configService;
        private ClaudeTabViewModel? _claudeTab;
        private OpenAITabViewModel? _openAITab;
        private Window? _ownerWindow;

        public string TabName => "Claude 配置";
        public IconSymbolEnum IconSymbol => IconSymbolEnum.Settings;

        public ClaudeTabViewModel ClaudeTab
        {
            get => _claudeTab ??= new ClaudeTabViewModel(_configService);
        }

        public OpenAITabViewModel OpenAITab
        {
            get => _openAITab ??= new OpenAITabViewModel(_configService);
        }

        public ICommand AddConfigurationCommand { get; }

        public ClaudeSwitchTabViewModel()
        {
            // 初始化配置服务，使用插件数据路径
            _configService = new ConfigurationService(GetDataPath());

            // 初始化命令
            AddConfigurationCommand = ReactiveCommand.Create(AddConfigurationAction);

            // 订阅添加配置请求
            ClaudeTab.AddConfigurationRequested += OnAddConfigurationRequested;
            OpenAITab.AddConfigurationRequested += OnAddConfigurationRequested;
        }

        /// <summary>
        /// 添加配置命令动作
        /// </summary>
        private void AddConfigurationAction()
        {
            OnAddConfigurationRequested();
        }

        /// <summary>
        /// 设置父窗口引用
        /// </summary>
        public void SetOwnerWindow(Window ownerWindow)
        {
            _ownerWindow = ownerWindow;
        }

        /// <summary>
        /// 获取插件数据路径
        /// </summary>
        private string GetDataPath()
        {
            // 这里应该从插件上下文获取数据路径
            // 临时使用当前目录下的 Data 文件夹
            var currentDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return System.IO.Path.Combine(currentDir ?? ".", "Data");
        }

        /// <summary>
        /// 添加配置请求处理
        /// </summary>
        private async void OnAddConfigurationRequested()
        {
            try
            {
                // 创建编辑对话框控件
                var editDialog = Views.EditConfigDialog.CreateAddDialog(_configService);

                // 显示模态对话框
                var result = await Services.DialogService.ShowModalDialog(_ownerWindow, editDialog, "添加 AI 配置");

                // 检查是否保存成功
                var viewModel = editDialog.DataContext as ViewModels.EditConfigDialogViewModel;
                if (viewModel != null && viewModel.IsSaved)
                {
                    RefreshAllTabs();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"打开编辑配置对话框失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 编辑配置请求处理
        /// </summary>
        public async void EditConfiguration(Models.AIConfiguration config)
        {
            try
            {
                // 创建编辑对话框控件
                var editDialog = Views.EditConfigDialog.CreateEditDialog(_configService, config);

                // 显示模态对话框
                var result = await Services.DialogService.ShowModalDialog(_ownerWindow, editDialog, "编辑 AI 配置");

                // 检查是否保存成功
                var viewModel = editDialog.DataContext as ViewModels.EditConfigDialogViewModel;
                if (viewModel != null && viewModel.IsSaved)
                {
                    RefreshAllTabs();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"打开编辑配置对话框失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 刷新所有页签
        /// </summary>
        public void RefreshAllTabs()
        {
            ClaudeTab.RefreshConfigurations();
            OpenAITab.RefreshConfigurations();
        }
    }
}