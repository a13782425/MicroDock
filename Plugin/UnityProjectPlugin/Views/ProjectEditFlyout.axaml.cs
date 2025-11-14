using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityProjectPlugin.Models;

namespace UnityProjectPlugin.Views
{
    /// <summary>
    /// 项目编辑 Flyout
    /// </summary>
    public partial class ProjectEditFlyout : UserControl
    {
        private readonly UnityProjectPlugin _plugin;
        private readonly UnityProject _project;
        private readonly Action _onSaved;

        private TextBox? _projectNameTextBox;
        private ComboBox? _groupComboBox;
        private Button? _saveButton;
        private Button? _cancelButton;

        public ProjectEditFlyout(UnityProjectPlugin plugin, UnityProject project, Action onSaved)
        {
            _plugin = plugin;
            _project = project;
            _onSaved = onSaved;

            InitializeComponent();
            InitializeControls();
            LoadData();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _projectNameTextBox = this.FindControl<TextBox>("ProjectNameTextBox");
            _groupComboBox = this.FindControl<ComboBox>("GroupComboBox");
            _saveButton = this.FindControl<Button>("SaveButton");
            _cancelButton = this.FindControl<Button>("CancelButton");
        }

        private void LoadData()
        {
            // 设置项目名称
            if (_projectNameTextBox != null)
            {
                _projectNameTextBox.Text = _project.Name;
            }

            // 加载所有分组
            if (_groupComboBox != null)
            {
                List<ProjectGroup> groups = _plugin.GetGroups();
                List<string> groupNames = groups.Select(g => g.Name).ToList();
                
                // 添加空选项（无分组）
                groupNames.Insert(0, string.Empty);
                
                _groupComboBox.ItemsSource = groupNames;
                
                // 设置当前分组
                if (!string.IsNullOrEmpty(_project.GroupName))
                {
                    _groupComboBox.SelectedItem = _project.GroupName;
                }
                else
                {
                    _groupComboBox.SelectedIndex = 0;
                }
            }
        }

        private void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_projectNameTextBox == null || string.IsNullOrWhiteSpace(_projectNameTextBox.Text))
                {
                    // TODO: 显示错误消息对话框
                    return;
                }

                string newName = _projectNameTextBox.Text.Trim();
                string? groupName = _groupComboBox?.SelectedItem as string;
                
                // 如果是可编辑 ComboBox，可能输入了新的分组名
                if (_groupComboBox != null && _groupComboBox.IsEditable && !string.IsNullOrEmpty(_groupComboBox.Text))
                {
                    groupName = _groupComboBox.Text.Trim();
                    
                    // 如果是新分组，自动创建
                    if (!string.IsNullOrEmpty(groupName) && !_plugin.GetGroups().Any(g => g.Name == groupName))
                    {
                        try
                        {
                            _plugin.AddGroup(groupName);
                        }
                        catch
                        {
                            // 创建分组失败，继续使用该名称
                        }
                    }
                }

                // 更新项目
                _plugin.UpdateProject(_project.Path, newName, groupName);

                // 关闭 Flyout 并通知刷新
                _onSaved?.Invoke();
                CloseFlyout();
            }
            catch (Exception ex)
            {
                // TODO: 显示错误消息对话框
                throw new Exception($"保存项目失败: {ex.Message}", ex);
            }
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            CloseFlyout();
        }

        private void CloseFlyout()
        {
            // Flyout 会在内容中自动管理，直接返回即可
            // 父级 Flyout 会通过事件自动关闭
        }
    }
}

