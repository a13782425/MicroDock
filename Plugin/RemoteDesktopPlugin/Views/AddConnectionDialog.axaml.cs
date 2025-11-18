using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RemoteDesktopPlugin.Models;
using System;

namespace RemoteDesktopPlugin.Views
{
    /// <summary>
    /// 添加/编辑远程连接对话框内容
    /// </summary>
    public partial class AddConnectionDialog : UserControl
    {
        private readonly RemoteDesktopPlugin _plugin;
        private readonly RemoteConnection? _editingConnection;
        private readonly bool _isEditMode;

        // UI 控件引用
        private TextBox? _connectionNameTextBox;
        private TextBox? _hostTextBox;
        private TextBox? _usernameTextBox;
        private TextBox? _passwordTextBox;
        private TextBox? _domainTextBox;
        private TextBox? _descriptionTextBox;

        /// <summary>
        /// 构造函数 - 添加模式
        /// </summary>
        public AddConnectionDialog(RemoteDesktopPlugin plugin)
        {
            _plugin = plugin;
            _isEditMode = false;

            InitializeComponent();
            InitializeControls();
        }

        /// <summary>
        /// 构造函数 - 编辑模式
        /// </summary>
        public AddConnectionDialog(RemoteDesktopPlugin plugin, RemoteConnection connection)
        {
            _plugin = plugin;
            _editingConnection = connection;
            _isEditMode = true;

            InitializeComponent();
            InitializeControls();
            LoadConnectionData();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _connectionNameTextBox = this.FindControl<TextBox>("ConnectionNameTextBox");
            _hostTextBox = this.FindControl<TextBox>("HostTextBox");
            _usernameTextBox = this.FindControl<TextBox>("UsernameTextBox");
            _passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
            _domainTextBox = this.FindControl<TextBox>("DomainTextBox");
            _descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");
        }

        private void LoadConnectionData()
        {
            if (_editingConnection == null) return;

            if (_connectionNameTextBox != null)
                _connectionNameTextBox.Text = _editingConnection.Name;

            if (_hostTextBox != null)
                _hostTextBox.Text = _editingConnection.Host;

            if (_usernameTextBox != null)
                _usernameTextBox.Text = _editingConnection.Username;

            if (_passwordTextBox != null)
                _passwordTextBox.Text = _editingConnection.Password;

            if (_domainTextBox != null && !string.IsNullOrEmpty(_editingConnection.Domain))
                _domainTextBox.Text = _editingConnection.Domain;

            if (_descriptionTextBox != null && !string.IsNullOrEmpty(_editingConnection.Description))
                _descriptionTextBox.Text = _editingConnection.Description;
        }

        /// <summary>
        /// 验证输入并返回结果
        /// </summary>
        public (bool IsValid, string? ErrorMessage) ValidateInput()
        {
            string name = _connectionNameTextBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                return (false, "请输入连接名称");
            }

            string host = _hostTextBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(host))
            {
                return (false, "请输入主机地址");
            }

            string username = _usernameTextBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(username))
            {
                return (false, "请输入用户名");
            }

            string password = _passwordTextBox?.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(password))
            {
                return (false, "请输入密码");
            }

            return (true, null);
        }

        /// <summary>
        /// 保存连接（添加或更新）
        /// </summary>
        public void SaveConnection()
        {
            var validation = ValidateInput();
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.ErrorMessage ?? "验证失败");
            }

            string name = _connectionNameTextBox?.Text?.Trim() ?? string.Empty;
            string host = _hostTextBox?.Text?.Trim() ?? string.Empty;
            string username = _usernameTextBox?.Text?.Trim() ?? string.Empty;
            string password = _passwordTextBox?.Text ?? string.Empty;

            // 固定使用默认端口 3389
            int port = 3389;

            // 获取可选字段
            string? domain = string.IsNullOrWhiteSpace(_domainTextBox?.Text) ? null : _domainTextBox.Text.Trim();
            string? description = string.IsNullOrWhiteSpace(_descriptionTextBox?.Text) ? null : _descriptionTextBox.Text.Trim();

            if (_isEditMode && _editingConnection != null)
            {
                _plugin.UpdateConnection(_editingConnection.Id, name, host, username, password, port, null, description);
            }
            else
            {
                _plugin.AddConnection(name, host, username, password, port, domain, null, description);
            }
        }
    }
}
