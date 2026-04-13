using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using HMOS_WearToolBox.Helper;

namespace HMOS_WearToolBox.UserController
{
    /// <summary>
    /// 终端控件，提供类似命令行的交互界面，支持设备连接检测、命令执行、历史记录和命令记录保存。
    /// </summary>
    public partial class TerminalControl : UserControl
    {
        // 是否启用命令记录
        private bool _recordEnabled = false;
        // 最大记录条数
        private int _maxRecords = 100;
        // 命令记录列表
        private List<CommandRecord> _records = new List<CommandRecord>();
        // 连接状态检测定时器
        private System.Windows.Forms.Timer _connectionTimer; // 现在在构造函数中初始化，不为 null
        // 设备连接状态
        private bool _isConnected = false;
        // 命令历史记录（用于上下键）
        private List<string> _commandHistory = new List<string>();
        // 当前在历史中的索引，-1 表示不在历史中
        private int _historyIndex = -1;

        /// <summary>
        /// 初始化终端控件。
        /// </summary>
        public TerminalControl()
        {
            InitializeComponent();

            // 确保计时器在构造函数中初始化，避免 null
            _connectionTimer = new System.Windows.Forms.Timer { Interval = 3000 };

            InitializeControl();
            ApplySettings();
        }

        /// <summary>
        /// 初始化控件的默认样式和事件绑定。
        /// </summary>
        private void InitializeControl()
        {
            if (richTextBoxTerminal == null) return;

            // 设置终端样式（默认值，后续会被 ApplySettings 覆盖）
            richTextBoxTerminal.BackColor = Color.Black;
            richTextBoxTerminal.ForeColor = Color.White;
            richTextBoxTerminal.Font = new Font("Consolas", 10);
            richTextBoxTerminal.WordWrap = false;
            richTextBoxTerminal.ScrollBars = RichTextBoxScrollBars.Both;
            richTextBoxTerminal.ContextMenuStrip = new ContextMenuStrip();

            // 绑定键盘和鼠标事件
            richTextBoxTerminal.KeyDown += RichTextBox_KeyDown;
            richTextBoxTerminal.MouseDown += RichTextBox_MouseDown;

            // 绑定计时器事件
            _connectionTimer.Tick += (s, e) => CheckConnectionAndUpdateUI();

            // 显示初始提示符
            AppendPrompt();
        }

        /// <summary>
        /// 应用用户设置的终端样式（字体、颜色）。
        /// </summary>
        public void ApplySettings()
        {
            if (richTextBoxTerminal == null) return;

            // 应用字体
            string fontName = Properties.Settings.Default.TerminalFontName;
            float fontSize = Properties.Settings.Default.TerminalFontSize;
            try
            {
                richTextBoxTerminal.Font = new Font(fontName, fontSize);
            }
            catch
            {
                richTextBoxTerminal.Font = new Font("Consolas", 12);
            }

            // 应用颜色
            richTextBoxTerminal.ForeColor = Properties.Settings.Default.TerminalForeColor;
            richTextBoxTerminal.BackColor = Properties.Settings.Default.TerminalBackColor;
        }

        // 连接状态检测相关方法

        /// <summary>
        /// 当控件可见性改变时，启动或停止连接检测定时器。
        /// </summary>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
            {
                _connectionTimer.Start();
                CheckConnectionAndUpdateUI();
            }
            else
            {
                _connectionTimer.Stop();
            }
        }

        /// <summary>
        /// 检查设备连接状态并更新 UI。
        /// </summary>
        private void CheckConnectionAndUpdateUI()
        {
            bool connected = IsDeviceConnected();
            if (_isConnected != connected)
            {
                _isConnected = connected;
                UpdateUIForConnection(connected);
            }
        }

        /// <summary>
        /// 通过 hdc 命令检测设备是否已连接。
        /// </summary>
        private bool IsDeviceConnected()
        {
            try
            {
                string targets = HdcHelper.RunHdcCommand("list targets");
                if (string.IsNullOrWhiteSpace(targets)) return false;
                var lines = targets.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                return lines.Any(line => line.Contains('.') || line.Contains("device"));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 根据连接状态更新终端控件的只读属性和提示文本。
        /// </summary>
        private void UpdateUIForConnection(bool connected)
        {
            if (richTextBoxTerminal.InvokeRequired)
            {
                richTextBoxTerminal.Invoke(new Action(() => UpdateUIForConnection(connected)));
                return;
            }

            richTextBoxTerminal.ReadOnly = !connected;

            if (!connected)
            {
                string text = richTextBoxTerminal.Text;
                if (string.IsNullOrWhiteSpace(text) || text.Trim() == ">")
                {
                    richTextBoxTerminal.Text = "[未连接设备，请先连接]\r\n> ";
                    richTextBoxTerminal.Select(richTextBoxTerminal.Text.Length - 2, 0);
                }
            }
            else
            {
                if (richTextBoxTerminal.Text.StartsWith("[未连接设备"))
                {
                    richTextBoxTerminal.Clear();
                    AppendPrompt();
                }
            }
        }

        // 输入与命令执行相关方法

        /// <summary>
        /// 在终端末尾添加命令提示符 "> "。
        /// </summary>
        private void AppendPrompt()
        {
            if (richTextBoxTerminal.InvokeRequired)
                richTextBoxTerminal.Invoke(new Action(() => richTextBoxTerminal.AppendText("> ")));
            else
                richTextBoxTerminal.AppendText("> ");
            richTextBoxTerminal.ScrollToCaret();
        }

        /// <summary>
        /// 获取当前输入行的命令内容。
        /// </summary>
        private string? GetCurrentCommand()
        {
            var lines = richTextBoxTerminal.Lines;
            if (lines.Length == 0) return null;
            string lastLine = lines[^1];
            if (lastLine.StartsWith("> "))
                return lastLine.Substring(2).Trim();
            return null;
        }

        /// <summary>
        /// 设置当前输入行的命令内容。
        /// </summary>
        private void SetCurrentCommand(string command)
        {
            if (richTextBoxTerminal.InvokeRequired)
            {
                richTextBoxTerminal.Invoke(new Action(() => SetCurrentCommand(command)));
                return;
            }

            string text = richTextBoxTerminal.Text;
            int lastPromptPos = text.LastIndexOf("> ");
            if (lastPromptPos == -1) return;

            int lineStart = richTextBoxTerminal.GetFirstCharIndexFromLine(richTextBoxTerminal.Lines.Length - 1);
            int lineLength = richTextBoxTerminal.Lines[^1].Length;
            richTextBoxTerminal.Select(lineStart + 2, lineLength - 2);
            richTextBoxTerminal.SelectedText = command;
            richTextBoxTerminal.Select(lineStart + 2 + command.Length, 0);
        }

        /// <summary>
        /// 移除最后一整行（用于替换命令输出）。
        /// </summary>
        private void RemoveLastLine()
        {
            int lastLineStart = richTextBoxTerminal.GetFirstCharIndexFromLine(richTextBoxTerminal.Lines.Length - 1);
            int lastLineLength = richTextBoxTerminal.Lines[^1].Length;
            int newlineLen = richTextBoxTerminal.Lines.Length > 1 ? Environment.NewLine.Length : 0;
            richTextBoxTerminal.Select(lastLineStart, lastLineLength + newlineLen);
            richTextBoxTerminal.SelectedText = "";
        }

        /// <summary>
        /// 向终端追加输出文本。
        /// </summary>
        private void AppendOutput(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (richTextBoxTerminal.InvokeRequired)
                richTextBoxTerminal.Invoke(new Action(() => richTextBoxTerminal.AppendText(text)));
            else
                richTextBoxTerminal.AppendText(text);
        }

        /// <summary>
        /// 异步执行命令，并将输出显示在终端中。
        /// </summary>
        private async void ExecuteCommand(string? command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                richTextBoxTerminal.AppendText(Environment.NewLine);
                AppendPrompt();
                return;
            }

            if (!IsDeviceConnected())
            {
                MessageBox.Show("未连接设备，请先连接设备后再执行命令。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AppendPrompt();
                return;
            }

            // 记录命令历史
            _commandHistory.Add(command);
            if (_commandHistory.Count > 100) _commandHistory.RemoveAt(0);
            _historyIndex = -1;

            // 禁用输入，防止在命令执行期间修改终端
            richTextBoxTerminal.ReadOnly = true;
            RemoveLastLine();               // 移除提示符行，准备输出命令结果
            richTextBoxTerminal.AppendText(Environment.NewLine);

            // 异步执行命令（避免阻塞 UI）
            string output = await Task.Run(() => RunHdcCommand(command));
            AppendOutput(output);
            richTextBoxTerminal.AppendText(Environment.NewLine);

            // 如果启用了记录，保存本次命令及其输出
            if (_recordEnabled)
                RecordCommand(command, output);

            // 恢复输入并显示新提示符
            richTextBoxTerminal.ReadOnly = false;
            AppendPrompt();
            CheckConnectionAndUpdateUI();   // 再次检查连接状态，因为命令可能改变连接
        }

        /// <summary>
        /// 执行 hdc 命令并返回输出结果。
        /// </summary>
        private string RunHdcCommand(string userInput)
        {
            string args = userInput.StartsWith("hdc ", StringComparison.OrdinalIgnoreCase)
                ? userInput.Substring(4).Trim()
                : userInput;

            try
            {
                return HdcHelper.RunHdcCommand(args);
            }
            catch (Exception ex)
            {
                return $"错误: {ex.Message}";
            }
        }

        // 辅助方法

        /// <summary>
        /// 判断光标是否位于最后一个提示符之后（即可编辑区域）。
        /// </summary>
        private bool IsCursorAtLastPrompt()
        {
            string text = richTextBoxTerminal.Text;
            int lastPromptPos = text.LastIndexOf("> ");
            if (lastPromptPos == -1) return false;

            int cursorPos = richTextBoxTerminal.SelectionStart;
            return cursorPos >= lastPromptPos + 2;
        }

        /// <summary>
        /// 将光标移动到最后一个提示符的末尾。
        /// </summary>
        private void MoveCursorToLastPromptEnd()
        {
            string text = richTextBoxTerminal.Text;
            int lastPromptPos = text.LastIndexOf("> ");
            if (lastPromptPos != -1)
            {
                richTextBoxTerminal.Select(lastPromptPos + 2, 0);
            }
        }

        // 事件处理

        /// <summary>
        /// 处理终端的键盘按键事件，实现命令编辑限制、历史命令导航和命令执行。
        /// </summary>
        private void RichTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!IsDeviceConnected())
            {
                e.SuppressKeyPress = true;
                return;
            }

            // 对于普通字符输入，如果光标不在最后一个提示符后，则移动到提示符末尾
            if (!e.Control && !e.Alt && e.KeyCode != Keys.Enter && e.KeyCode != Keys.Back && e.KeyCode != Keys.Delete &&
                e.KeyCode != Keys.Up && e.KeyCode != Keys.Down && e.KeyCode != Keys.Left && e.KeyCode != Keys.Right &&
                e.KeyCode != Keys.Home && e.KeyCode != Keys.End && e.KeyCode != Keys.PageUp && e.KeyCode != Keys.PageDown)
            {
                if (!IsCursorAtLastPrompt())
                {
                    MoveCursorToLastPromptEnd();
                }
            }

            int selStart = richTextBoxTerminal.SelectionStart;
            int selLength = richTextBoxTerminal.SelectionLength;
            string text = richTextBoxTerminal.Text;
            int lastPromptPos = text.LastIndexOf("> ");
            const int promptLen = 2;

            if (lastPromptPos >= 0)
            {
                // 防止删除或修改提示符
                if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
                {
                    if (selStart <= lastPromptPos + promptLen - 1 && (selStart + selLength) > lastPromptPos)
                    {
                        e.SuppressKeyPress = true;
                        return;
                    }
                    if (e.KeyCode == Keys.Back && selStart == lastPromptPos + promptLen && selLength == 0)
                    {
                        e.SuppressKeyPress = true;
                        return;
                    }
                    if (e.KeyCode == Keys.Delete && selStart == lastPromptPos && selLength == 0)
                    {
                        e.SuppressKeyPress = true;
                        return;
                    }
                }

                // 防止剪切提示符
                if (e.Control && e.KeyCode == Keys.X)
                {
                    if (selStart <= lastPromptPos + promptLen - 1 && (selStart + selLength) > lastPromptPos)
                    {
                        e.SuppressKeyPress = true;
                        return;
                    }
                }
            }

            // 上箭头：历史命令向上
            if (e.KeyCode == Keys.Up)
            {
                e.SuppressKeyPress = true;
                if (_commandHistory.Count == 0) return;
                if (_historyIndex == -1) _historyIndex = _commandHistory.Count - 1;
                else if (_historyIndex > 0) _historyIndex--;
                SetCurrentCommand(_commandHistory[_historyIndex]);
            }
            // 下箭头：历史命令向下
            else if (e.KeyCode == Keys.Down)
            {
                e.SuppressKeyPress = true;
                if (_commandHistory.Count == 0) return;
                if (_historyIndex == -1) return;
                if (_historyIndex < _commandHistory.Count - 1)
                {
                    _historyIndex++;
                    SetCurrentCommand(_commandHistory[_historyIndex]);
                }
                else
                {
                    _historyIndex = -1;
                    SetCurrentCommand("");
                }
            }
            // 回车：执行命令
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string? command = GetCurrentCommand();
                ExecuteCommand(command);
            }
        }

        /// <summary>
        /// 处理右键单击，实现右键粘贴功能（粘贴剪贴板内容的第一行）。
        /// </summary>
        private void RichTextBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int pos = richTextBoxTerminal.GetCharIndexFromPosition(e.Location);
                if (pos >= 0)
                {
                    // 如果有选中的文本，则复制到剪贴板（即右键复制）
                    if (richTextBoxTerminal.SelectionLength > 0)
                    {
                        int selStart = richTextBoxTerminal.SelectionStart;
                        int selEnd = selStart + richTextBoxTerminal.SelectionLength;
                        if (pos >= selStart && pos < selEnd)
                        {
                            Clipboard.SetText(richTextBoxTerminal.SelectedText);
                            richTextBoxTerminal.SelectionLength = 0;
                            return;
                        }
                    }

                    // 否则，如果设备已连接且剪贴板有文本，则粘贴第一行
                    if (IsDeviceConnected() && Clipboard.ContainsText())
                    {
                        string textToPaste = Clipboard.GetText();
                        if (!string.IsNullOrEmpty(textToPaste))
                        {
                            string firstLine = textToPaste.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                            if (!IsCursorAtLastPrompt())
                                MoveCursorToLastPromptEnd();
                            richTextBoxTerminal.SelectedText = firstLine;
                            richTextBoxTerminal.Select(richTextBoxTerminal.SelectionStart + firstLine.Length, 0);
                        }
                    }
                }
            }
        }

        // 记录功能相关方法

        /// <summary>
        /// 刷新记录设置（是否启用记录、最大记录数）。
        /// </summary>
        public void RefreshSettings(bool recordEnabled, int maxRecords)
        {
            _recordEnabled = recordEnabled;
            _maxRecords = maxRecords > 0 ? maxRecords : 1;
            if (_records.Count > _maxRecords)
                _records.RemoveRange(0, _records.Count - _maxRecords);
        }

        /// <summary>
        /// 记录一条命令及其输出。
        /// </summary>
        private void RecordCommand(string command, string output)
        {
            var record = new CommandRecord
            {
                Command = command,
                Output = output,
                Timestamp = DateTime.Now
            };
            _records.Add(record);
            while (_records.Count > _maxRecords)
                _records.RemoveAt(0);
        }

        /// <summary>
        /// 获取已记录的命令列表（只读）。
        /// </summary>
        public IReadOnlyList<CommandRecord> GetRecords() => _records.AsReadOnly();

        /// <summary>
        /// 清空所有命令记录。
        /// </summary>
        public void ClearRecords() => _records.Clear();
    }

    /// <summary>
    /// 表示一条命令记录，包含命令文本、输出和记录时间。
    /// </summary>
    public class CommandRecord
    {
        /// <summary>执行的命令。</summary>
        public string? Command { get; set; }
        /// <summary>命令的输出。</summary>
        public string? Output { get; set; }
        /// <summary>记录时间。</summary>
        public DateTime Timestamp { get; set; }
    }
}