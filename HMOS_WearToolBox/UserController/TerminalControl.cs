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
    /// 终端控件，模拟命令行交互，支持执行 hdc 命令、命令历史记录和命令记录保存。
    /// </summary>
    public partial class TerminalControl : UserControl
    {
        // 是否启用命令记录功能
        private bool _recordEnabled = false;
        // 最大保存记录数
        private int _maxRecords = 100;
        // 存储命令执行记录
        private List<CommandRecord> _records = new List<CommandRecord>();
        // 定时检测设备连接状态的计时器
        private System.Windows.Forms.Timer _connectionTimer;
        // 当前设备是否已连接
        private bool _isConnected = false;
        // 命令历史列表（用于上下键切换）
        private List<string> _commandHistory = new List<string>();
        // 当前历史索引（-1 表示未使用历史）
        private int _historyIndex = -1;

        public TerminalControl()
        {
            InitializeComponent();
            InitializeControl();
            ApplySettings(); // 应用用户保存的终端样式
        }

        /// <summary>
        /// 初始化控件外观和事件绑定。
        /// </summary>
        private void InitializeControl()
        {
            if (richTextBoxTerminal == null) return;

            // 设置终端基础样式（默认值，后续会被 ApplySettings 覆盖）
            richTextBoxTerminal.BackColor = Color.Black;
            richTextBoxTerminal.ForeColor = Color.White;
            richTextBoxTerminal.Font = new Font("Consolas", 10);
            richTextBoxTerminal.WordWrap = false;              // 禁止自动换行
            richTextBoxTerminal.ScrollBars = RichTextBoxScrollBars.Both;
            // 禁用默认右键菜单，使用自定义的鼠标右键处理（复制/粘贴）
            richTextBoxTerminal.ContextMenuStrip = new ContextMenuStrip();

            // 绑定键盘和鼠标事件
            richTextBoxTerminal.KeyDown += RichTextBox_KeyDown;
            richTextBoxTerminal.MouseDown += RichTextBox_MouseDown;

            // 创建连接状态检测计时器，每 3 秒检测一次
            _connectionTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            _connectionTimer.Tick += (s, e) => CheckConnectionAndUpdateUI();

            // 显示初始提示符
            AppendPrompt();
        }

        /// <summary>
        /// 应用用户保存的终端样式（字体、颜色）
        /// </summary>
        public void ApplySettings()
        {
            if (richTextBoxTerminal == null) return;

            // 从应用程序设置中读取保存的终端样式
            string fontName = Properties.Settings.Default.TerminalFontName;
            float fontSize = Properties.Settings.Default.TerminalFontSize;

            // 使用字体辅助类获取字体（优先系统字体，其次嵌入字体，最后回退到微软雅黑）
            richTextBoxTerminal.Font = FontHelper.GetFont(fontName, fontSize);

            // 应用颜色
            richTextBoxTerminal.ForeColor = Properties.Settings.Default.TerminalForeColor;
            richTextBoxTerminal.BackColor = Properties.Settings.Default.TerminalBackColor;
        }

        /// <summary>
        /// 控件可见性改变时，控制连接检测计时器的启停。
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
        /// 检测设备连接状态并更新 UI（如果状态发生变化）。
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
        /// 判断当前是否有设备连接。
        /// </summary>
        private bool IsDeviceConnected()
        {
            try
            {
                string targets = HdcHelper.RunHdcCommand("list targets");
                if (string.IsNullOrWhiteSpace(targets)) return false;
                var lines = targets.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                // 包含 '.' 表示 IP 地址，或包含 "device" 表示已连接设备
                return lines.Any(line => line.Contains('.') || line.Contains("device"));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 根据连接状态更新 UI：只读/可编辑，并显示提示信息。
        /// </summary>
        private void UpdateUIForConnection(bool connected)
        {
            if (richTextBoxTerminal.InvokeRequired)
            {
                richTextBoxTerminal.Invoke(new Action(() => UpdateUIForConnection(connected)));
                return;
            }

            richTextBoxTerminal.ReadOnly = !connected;   // 未连接时禁止输入

            if (!connected)
            {
                string text = richTextBoxTerminal.Text;
                if (string.IsNullOrWhiteSpace(text) || text.Trim() == ">")
                {
                    // 未连接时显示提示信息
                    richTextBoxTerminal.Text = "[未连接设备，请先连接]\r\n> ";
                    richTextBoxTerminal.Select(richTextBoxTerminal.Text.Length - 2, 0);
                }
            }
            else
            {
                // 如果之前显示的是未连接提示，则清空并显示正常提示符
                if (richTextBoxTerminal.Text.StartsWith("[未连接设备"))
                {
                    richTextBoxTerminal.Clear();
                    AppendPrompt();
                }
            }
        }

        /// <summary>
        /// 在终端末尾追加新的命令提示符 "> "。
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
        /// 获取当前行中用户输入的命令（去掉提示符部分）。
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
        /// 设置当前行中的命令文本（用于上下键历史替换）。
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
            // 选中提示符后的文本区域并替换为新命令
            richTextBoxTerminal.Select(lineStart + 2, lineLength - 2);
            richTextBoxTerminal.SelectedText = command;
            // 将光标定位到命令末尾
            richTextBoxTerminal.Select(lineStart + 2 + command.Length, 0);
        }

        /// <summary>
        /// 删除最后一行（用于执行命令前移除提示符行）。
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
        /// 在终端末尾追加输出文本。
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
        /// 执行用户输入的命令。
        /// </summary>
        private async void ExecuteCommand(string? command)
        {
            // 空命令：直接换行并显示新提示符
            if (string.IsNullOrWhiteSpace(command))
            {
                richTextBoxTerminal.AppendText(Environment.NewLine);
                AppendPrompt();
                return;
            }

            // 设备未连接时提示并返回
            if (!IsDeviceConnected())
            {
                MessageBox.Show("未连接设备，请先连接设备后再执行命令。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AppendPrompt();
                return;
            }

            // 保存命令到历史记录（限制最大100条）
            _commandHistory.Add(command);
            if (_commandHistory.Count > 100) _commandHistory.RemoveAt(0);
            _historyIndex = -1;   // 重置历史索引

            // 临时禁止编辑，移除提示符行，准备输出
            richTextBoxTerminal.ReadOnly = true;
            RemoveLastLine();
            richTextBoxTerminal.AppendText(Environment.NewLine);

            // 在后台线程执行 hdc 命令
            string output = await Task.Run(() => RunHdcCommand(command));
            AppendOutput(output);
            richTextBoxTerminal.AppendText(Environment.NewLine);

            // 如果启用了命令记录，保存本次执行记录
            if (_recordEnabled)
                RecordCommand(command, output);

            // 恢复编辑状态，显示新提示符
            richTextBoxTerminal.ReadOnly = false;
            AppendPrompt();

            // 重新检查连接状态（可能命令执行后设备断开）
            CheckConnectionAndUpdateUI();
        }

        /// <summary>
        /// 实际调用 HdcHelper 执行命令。
        /// </summary>
        private string RunHdcCommand(string userInput)
        {
            // 如果用户输入以 "hdc " 开头，则去掉这个前缀（因为 HdcHelper 会自动添加）
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

        /// <summary>
        /// 判断当前光标是否位于最后一个提示符的后面（即用户输入区域）。
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
        /// 将光标移动到最后一个提示符的末尾（用户输入起始位置）。
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

        /// <summary>
        /// 处理终端键盘输入，包括命令输入、上下键历史、回车执行等。
        /// </summary>
        private void RichTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!IsDeviceConnected())
            {
                // 未连接时禁止任何键盘输入
                e.SuppressKeyPress = true;
                return;
            }

            // 可打印字符（非控制键）输入时，如果光标不在提示符后，自动跳转
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
                // 禁止删除或剪切提示符
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

                // 禁止 Ctrl+X 剪切提示符
                if (e.Control && e.KeyCode == Keys.X)
                {
                    if (selStart <= lastPromptPos + promptLen - 1 && (selStart + selLength) > lastPromptPos)
                    {
                        e.SuppressKeyPress = true;
                        return;
                    }
                }
            }

            // 上下键处理命令历史
            if (e.KeyCode == Keys.Up)
            {
                e.SuppressKeyPress = true;
                if (_commandHistory.Count == 0) return;
                if (_historyIndex == -1) _historyIndex = _commandHistory.Count - 1;
                else if (_historyIndex > 0) _historyIndex--;
                SetCurrentCommand(_commandHistory[_historyIndex]);
            }
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
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string? command = GetCurrentCommand();
                ExecuteCommand(command);
            }
        }

        /// <summary>
        /// 处理鼠标右键：如果选中文本则复制，否则尝试粘贴剪贴板内容。
        /// </summary>
        private void RichTextBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // 获取鼠标下的字符位置
                int pos = richTextBoxTerminal.GetCharIndexFromPosition(e.Location);
                if (pos >= 0)
                {
                    // 如果存在选中文本且鼠标点击在选中区域内，则复制并清除选中
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

                    // 否则尝试粘贴（仅当设备已连接且剪贴板有文本时）
                    if (IsDeviceConnected() && Clipboard.ContainsText())
                    {
                        string textToPaste = Clipboard.GetText();
                        if (!string.IsNullOrEmpty(textToPaste))
                        {
                            // 只粘贴第一行（避免多行命令导致意外）
                            string firstLine = textToPaste.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                            // 确保光标在提示符后
                            if (!IsCursorAtLastPrompt())
                                MoveCursorToLastPromptEnd();
                            richTextBoxTerminal.SelectedText = firstLine;
                            // 光标移到插入文本末尾
                            richTextBoxTerminal.Select(richTextBoxTerminal.SelectionStart + firstLine.Length, 0);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 外部调用，用于更新记录配置（是否启用、最大记录数）。
        /// </summary>
        public void RefreshSettings(bool recordEnabled, int maxRecords)
        {
            _recordEnabled = recordEnabled;
            _maxRecords = maxRecords > 0 ? maxRecords : 1;
            // 如果当前记录数超过新的限制，截断多余部分
            if (_records.Count > _maxRecords)
                _records.RemoveRange(0, _records.Count - _maxRecords);
        }

        /// <summary>
        /// 保存一条命令执行记录。
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
            // 保证记录数不超过最大限制
            while (_records.Count > _maxRecords)
                _records.RemoveAt(0);
        }

        /// <summary>
        /// 获取当前保存的所有命令记录（只读）。
        /// </summary>
        public IReadOnlyList<CommandRecord> GetRecords() => _records.AsReadOnly();

        /// <summary>
        /// 清空所有命令记录。
        /// </summary>
        public void ClearRecords() => _records.Clear();
    }

    /// <summary>
    /// 命令执行记录实体类。
    /// </summary>
    public class CommandRecord
    {
        public string? Command { get; set; }   // 执行的命令
        public string? Output { get; set; }    // 命令输出
        public DateTime Timestamp { get; set; } // 执行时间
    }
}