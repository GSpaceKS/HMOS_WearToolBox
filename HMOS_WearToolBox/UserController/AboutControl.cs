using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace HMOS_WearToolBox.UserController
{
    public partial class AboutControl : UserControl
    {
        public AboutControl()
        {
            InitializeComponent();
            if (!DesignMode)
            {
                LoadVersion();
                SetupLinks();
            }
        }

        /// <summary>
        /// 从程序集加载版本号（优先使用 InformationalVersion）
        /// </summary>
        private void LoadVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            // 获取程序集信息版本（可包含任意字符串，如 "1.0.0-Alpha"）
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(informationalVersion))
            {
                labelSoftwareVersionValue.Text = informationalVersion;
            }
            else
            {
                // 回退到普通版本号
                var version = assembly.GetName().Version;
                labelSoftwareVersionValue.Text = version?.ToString(3) ?? "1.0.0";
            }
        }

        /// <summary>
        /// 为三个链接添加点击事件
        /// </summary>
        private void SetupLinks()
        {
            linkLabelGitHub.LinkClicked += (s, e) => OpenUrl("https://github.com/GSpaceKS/HMOS_WearToolBox");
            linkLabelAgreement.LinkClicked += (s, e) => OpenUrl("https://github.com/GSpaceKS/HMOS_WearToolBox?tab=MIT-1-ov-file");
            linkLabelIssue.LinkClicked += (s, e) => OpenUrl("https://github.com/GSpaceKS/HMOS_WearToolBox/issues");
        }

        /// <summary>
        /// 打开默认浏览器
        /// </summary>
        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开链接：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}