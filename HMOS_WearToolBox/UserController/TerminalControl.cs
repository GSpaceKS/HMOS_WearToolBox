using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using HMOS_WearToolBox.Helper;

namespace HMOS_WearToolBox.UserController
{
    /// <summary>
    /// 终端控件，模拟命令行交互，支持执行 hdc 命令、上下键命令历史、右键复制/粘贴。
    /// </summary>
    public partial class TerminalControl : UserControl
    {
        // 定时检测设备连接状态的计时器（在 InitializeControl 中创建）
        private System.Windows.Forms.Timer? _connectionTimer;
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

            // 创建连接状态检测计时器，每 3 秒检测一次（异步执行，不阻塞 UI 线程）
            _connectionTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            _connectionTimer.Tick += async (s, e) => await RefreshConnectionStateAsync();

            // 显示初始提示符
            AppendPrompt();
        }

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

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
            {
                _connectionTimer?.Start();
                _ = RefreshConnectionStateAsync();
            }
            else
            {
                _connectionTimer?.Stop();
            }
        }

        /// <summary>
        /// 重新检测设备连接状态并更新 UI（仅状态变化时才刷新界面）。
        /// 走 DeviceConnectionHelper 的缓存，且在后台线程询问 hdc，不会卡住界面。
        /// </summary>
        private async Task RefreshConnectionStateAsync()
        {
            bool connected = await DeviceConnectionHelper.CheckAndUpdateConnectionStatusAsync();

            if (_isConnected == connected)
                return;

            _isConnected = connected;
            UpdateUIForConnection(connected);
        }

        /// <summary>
        /// 当前是否有设备连接。
        /// 这里只读缓存、不主动去问 hdc —— 这个方法在每次按键、每次右键里都会调用，
        /// 真去启动一个 hdc 进程会让打字明显卡顿。缓存由上方 3 秒一次的定时器刷新。
        /// </summary>
        private bool IsDeviceConnected() => DeviceConnectionHelper.LastKnownOnline;

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

        private void AppendPrompt()
        {
            if (richTextBoxTerminal.InvokeRequired)
                richTextBoxTerminal.Invoke(new Action(() => richTextBoxTerminal.AppendText("> ")));
            else
                richTextBoxTerminal.AppendText("> ");
            richTextBoxTerminal.ScrollToCaret();
        }

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

        private void AppendOutput(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (richTextBoxTerminal.InvokeRequired)
                richTextBoxTerminal.Invoke(new Action(() => richTextBoxTerminal.AppendText(text)));
            else
                richTextBoxTerminal.AppendText(text);
        }

        private async void ExecuteCommand(string? command)
        {
            // 空命令：直接换行并显示新提示符
            if (string.IsNullOrWhiteSpace(command))
            {
                richTextBoxTerminal.AppendText(Environment.NewLine);
                AppendPrompt();
                return;
            }

            // 执行前做一次真实检测（不看可能过期的缓存），顺便同步界面状态
            DeviceConnectionHelper.InvalidateCache();
            await RefreshConnectionStateAsync();

            // 设备未连接时提示并返回
            if (!_isConnected)
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

            // 恢复编辑状态，显示新提示符
            richTextBoxTerminal.ReadOnly = false;
            AppendPrompt();

            // 重新检查连接状态（可能命令执行后设备断开）
            DeviceConnectionHelper.InvalidateCache();
            await RefreshConnectionStateAsync();
        }

        private string RunHdcCommand(string userInput)
        {
            // 如果用户输入以 "hdc " 开头，则去掉这个前缀（因为 HdcHelper 会自动添加）
            string args = userInput.StartsWith("hdc ", StringComparison.OrdinalIgnoreCase)
                ? userInput.Substring(4).Trim()
                : userInput;

            try
            {
                // 终端是给用户手敲命令用的，可能出现 file send 这类长任务，
                // 所以给长超时；但依然要有上限，避免命令挂死时永远回不来。
                return HdcHelper.RunHdcCommand(args, HdcHelper.LongRunningTimeoutMs);
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
            if (!_isConnected)
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
                    if (_isConnected && Clipboard.ContainsText())
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

    }
}