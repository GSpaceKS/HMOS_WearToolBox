using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using HMOS_WearToolBox.Manager;

namespace HMOS_WearToolBox.Helper
{
    /// <summary>本机签名物料的保存位置（%APPDATA%\HMOS_WearToolBox\signing）。</summary>
    public sealed class SigningSettings
    {
        public string KeyStoreFile { get; set; } = string.Empty;
        public string KeyAlias { get; set; } = "hmoswear";
        public string KeyPwd { get; set; } = string.Empty;
        public string CertFile { get; set; } = string.Empty;
        public string ProfileFile { get; set; } = string.Empty;
        public string LastHap { get; set; } = string.Empty;

        /// <summary>上次用的新包名。同一份 HAP 再打开时沿用它，免得每次都要去 AGC 重新登记。</summary>
        public string LastBundle { get; set; } = string.Empty;

        public static string Folder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "HMOS_WearToolBox", "signing");

        public static string SettingsFile => Path.Combine(Folder, "signing.json");

        public static string DefaultKeyStore => Path.Combine(Folder, "hmoswear.p12");

        public static string DefaultCsr => Path.Combine(Folder, "hmoswear.csr");

        public static SigningSettings Load()
        {
            SigningSettings settings = new SigningSettings();
            try
            {
                if (File.Exists(SettingsFile))
                {
                    SigningSettings? loaded = JsonSerializer.Deserialize<SigningSettings>(File.ReadAllText(SettingsFile));
                    if (loaded != null)
                        settings = loaded;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("读取签名配置失败: " + ex.Message);
            }

            if (string.IsNullOrWhiteSpace(settings.KeyStoreFile))
                settings.KeyStoreFile = DefaultKeyStore;
            if (string.IsNullOrWhiteSpace(settings.KeyAlias))
                settings.KeyAlias = "hmoswear";
            if (string.IsNullOrWhiteSpace(settings.KeyPwd))
                settings.KeyPwd = RandomPassword();

            return settings;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(SettingsFile, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("保存签名配置失败: " + ex.Message);
            }
        }

        private static string RandomPassword()
        {
            byte[] bytes = new byte[9];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }

    /// <summary>从 .p7b 里读回明文字段。</summary>
    public sealed class SignedProfileInfo
    {
        public string BundleName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string VersionName { get; set; } = string.Empty;
        public List<string> DeviceIds { get; set; } = new List<string>();
    }

    public static class ProfileInspector
    {
        public static bool TryRead(string p7bPath, out SignedProfileInfo info)
        {
            info = new SignedProfileInfo();
            try
            {
                if (!File.Exists(p7bPath))
                    return false;

                byte[] bytes = File.ReadAllBytes(p7bPath);
                string text = Encoding.UTF8.GetString(bytes);

                int start = text.IndexOf("{\"version-name\"", StringComparison.Ordinal);
                if (start < 0)
                    start = text.IndexOf("{\"bundle-info\"", StringComparison.Ordinal);
                if (start < 0)
                    return false;

                int depth = 0;
                int end = -1;
                for (int i = start; i < text.Length; i++)
                {
                    char c = text[i];
                    if (c == '{')
                    {
                        depth++;
                    }
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            end = i;
                            break;
                        }
                    }
                }

                if (end < 0)
                    return false;

                string json = text.Substring(start, end - start + 1);
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("version-name", out JsonElement version) && version.ValueKind == JsonValueKind.String)
                    info.VersionName = version.GetString() ?? string.Empty;
                if (root.TryGetProperty("type", out JsonElement type) && type.ValueKind == JsonValueKind.String)
                    info.Type = type.GetString() ?? string.Empty;
                if (root.TryGetProperty("bundle-info", out JsonElement bundleInfo) &&
                    bundleInfo.TryGetProperty("bundle-name", out JsonElement bundleName) &&
                    bundleName.ValueKind == JsonValueKind.String)
                    info.BundleName = bundleName.GetString() ?? string.Empty;
                if (root.TryGetProperty("debug-info", out JsonElement debugInfo) &&
                    debugInfo.TryGetProperty("device-ids", out JsonElement ids) &&
                    ids.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in ids.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                            info.DeviceIds.Add(item.GetString() ?? string.Empty);
                    }
                }

                return info.BundleName.Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 自签安装的完整流程：重打包 → 签名 → 推送到设备 → 安装。
    /// </summary>
    public static class SelfSignService
    {
        /// <summary>编译期常量：签名块 schema 版本。API 23 的设备用 23 即可。</summary>
        private const int CompatibleVersion = 23;

        public static string WorkRoot =>
            Path.Combine(Path.GetTempPath(), "HMOS_WearToolBox", "sign");

        public static string CreateWorkDir()
        {
            string dir = Path.Combine(WorkRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static bool EnsureKeyStore(SigningSettings settings, Action<string> log)
        {
            if (File.Exists(settings.KeyStoreFile))
                return false;

            string? folder = Path.GetDirectoryName(settings.KeyStoreFile);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            SignToolResult result = SignTool.Run("generate-keypair", new List<string>
            {
                "generate-keypair",
                "-keyAlias", settings.KeyAlias,
                "-keyPwd", settings.KeyPwd,
                "-keyAlg", "ECC",
                "-keySize", "NIST-P-256",
                "-keystoreFile", settings.KeyStoreFile,
                "-keystorePwd", settings.KeyPwd
            }, Path.GetDirectoryName(settings.KeyStoreFile) ?? WorkRoot);

            log(result.Output);
            if (!result.Success)
                throw new InvalidOperationException("生成密钥库失败：" + LastLine(result.Output));

            return true;
        }

        /// <summary>
        /// 导出 CSR（证书签名请求），用于上传到 AGC 换调试证书。
        /// </summary>
        public static string GenerateCsr(SigningSettings settings, string subject, Action<string> log)
        {
            Directory.CreateDirectory(SigningSettings.Folder);
            string csrPath = SigningSettings.DefaultCsr;

            SignToolResult result = SignTool.Run("generate-csr", new List<string>
            {
                "generate-csr",
                "-keyAlias", settings.KeyAlias,
                "-keyPwd", settings.KeyPwd,
                "-subject", subject,
                "-signAlg", "SHA256withECDSA",
                "-keystoreFile", settings.KeyStoreFile,
                "-keystorePwd", settings.KeyPwd,
                "-outFile", csrPath
            }, SigningSettings.Folder);

            log(result.Output);
            if (!result.Success || !File.Exists(csrPath))
                throw new InvalidOperationException("生成 CSR 失败：" + LastLine(result.Output));

            return csrPath;
        }

        public static string Repack(HapInfo info, string newBundle, string workDir, Action<string> log)
        {
            string target = Path.Combine(workDir, "patched.hap");
            HapPatcher.Repack(info, newBundle, new[] { "wearable" }, target, log);
            return target;
        }

        public static string Sign(string hapPath, string newBundle, string certFile, string profileFile,
                                  SigningSettings settings, string workDir, Action<string> log)
        {
            string certPem = PrepareCertFile(certFile, workDir, log);
            string signedPath = Path.Combine(workDir, "signed.hap");

            SignToolResult result = SignTool.Run("sign-app", new List<string>
            {
                "sign-app",
                "-mode", "localSign",
                "-keyAlias", settings.KeyAlias,
                "-keyPwd", settings.KeyPwd,
                "-appCertFile", certPem,
                "-profileFile", profileFile,
                "-inFile", hapPath,
                "-signAlg", "SHA256withECDSA",
                "-keystoreFile", settings.KeyStoreFile,
                "-keystorePwd", settings.KeyPwd,
                "-outFile", signedPath,
                "-compatibleVersion", CompatibleVersion.ToString()
            }, workDir);

            log(result.Output);
            if (!result.Success || !File.Exists(signedPath))
                throw new InvalidOperationException("签名失败：" + LastLine(result.Output));

            return signedPath;
        }

        public static void Verify(string signedHap, string workDir, Action<string> log)
        {
            SignToolResult result = SignTool.Run("verify-app", new List<string>
            {
                "verify-app",
                "-inFile", signedHap,
                "-outCertChain", Path.Combine(workDir, "out.cer"),
                "-outProfile", Path.Combine(workDir, "out.p7b")
            }, workDir);

            log(result.Output);
            if (!result.Success)
                throw new InvalidOperationException("签名自检失败：" + LastLine(result.Output));
        }

        /// <summary>推送到设备并安装。</summary>
        public static string Install(string signedHap, Action<string> log)
        {
            if (!File.Exists(signedHap))
                throw new FileNotFoundException("签名后的 HAP 不存在：" + signedHap, signedHap);

            string target = EnsureDevice(log);
            log("目标设备：" + target);
            log("install -r " + signedHap);

            string install = HdcHelper.RunHdcCommand("install -r \"" + signedHap + "\"", HdcHelper.LongRunningTimeoutMs);
            if (install.IndexOf("need connect-key", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // 设备刚好在这一瞬间掉线（手表息屏很常见），重连一次再试
                log("hdc 说需要重新选择设备，正在重连后重试…");
                EnsureDevice(log);
                install = HdcHelper.RunHdcCommand("install -r \"" + signedHap + "\"", HdcHelper.LongRunningTimeoutMs);
            }

            log("install: " + install.Trim());
            return install.Trim();
        }

        /// <summary>
        /// 卸载设备上的同名应用（版本降级、或者用户主动重装时用）。
        /// </summary>
        public static string Uninstall(string bundleName, Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(bundleName))
                throw new ArgumentException("包名为空，无法卸载。", nameof(bundleName));

            EnsureDevice(log);
            log("uninstall " + bundleName);
            string output = HdcHelper.RunHdcCommand("uninstall " + bundleName, HdcHelper.LongRunningTimeoutMs);
            log("uninstall: " + output.Trim());
            return output.Trim();
        }

        /// <summary>
        /// 在手表上拉起刚装好的应用。abilityName 为空时退回 EntryAbility。
        /// </summary>
        public static string StartApp(string bundleName, string abilityName, Action<string> log)
        {
            string ability = string.IsNullOrWhiteSpace(abilityName) ? "EntryAbility" : abilityName;
            EnsureDevice(log);
            string command = "shell \"aa start -a " + ability + " -b " + bundleName + "\"";
            log("aa start -a " + ability + " -b " + bundleName);
            string output = HdcHelper.RunHdcCommand(command, HdcHelper.LongRunningTimeoutMs);
            log("aa start: " + output.Trim());
            return output.Trim();
        }

        /// <summary>确保 hdc 里有一台可用设备，必要时自动重连。</summary>
        public static string EnsureDevice(Action<string> log)
        {
            List<string> targets = HdcHelper.ListTargets();
            if (targets.Count > 0)
                return targets[0];

            List<string> tried = new List<string>();
            foreach (DeviceInfo device in DeviceManager.GetDevices())
            {
                string ip = device.IpAddress;
                if (string.IsNullOrWhiteSpace(ip) || tried.Contains(ip))
                    continue;
                tried.Add(ip);

                log("设备不在线，尝试重新连接：" + ip);
                try
                {
                    HdcHelper.RunHdcCommand("tconn " + ip, HdcHelper.LongRunningTimeoutMs);
                }
                catch (Exception ex)
                {
                    log("tconn 失败：" + ex.Message);
                }

                for (int i = 0; i < 6; i++)
                {
                    Thread.Sleep(400);
                    targets = HdcHelper.ListTargets();
                    if (targets.Count > 0)
                    {
                        log("已连接：" + targets[0]);
                        return targets[0];
                    }
                }
            }

            throw new InvalidOperationException(
                "手表没连上：hdc 里没有任何设备。" +
                (tried.Count == 0
                    ? "先在「主页」里添加并连接手表。"
                    : "已尝试重连 " + string.Join("、", tried) + " 都没成功，请确认手表亮屏、和电脑在同一个无线网络里。"));
        }

        /// <summary>
        /// 把 hdc 的安装错误码翻译成用户能看懂的一句话。
        /// </summary>
        public static string HintFor(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
                return string.Empty;
            if (output.IndexOf("9568257", StringComparison.Ordinal) >= 0)
                return "\r\n\r\n提示：设备不信任这份证书链。手表只认华为 AGC 签发的调试证书，自建 CA 一定被拒。";
            if (output.IndexOf("9568320", StringComparison.Ordinal) >= 0)
                return "\r\n\r\n提示：HAP 里没有签名块，说明签名那一步没有成功。";
            if (output.IndexOf("9568263", StringComparison.Ordinal) >= 0)
                return "\r\n\r\n提示：设备上已经装了同一个包名、但版本号更高的应用。先在手表上卸载那个应用，再重新安装。";
            if (output.IndexOf("9568329", StringComparison.Ordinal) >= 0)
                return "\r\n\r\n提示：Profile 里绑定的应用标识（appIdentifier）和这个 HAP 的包名对不上。请确认 Profile 是在 AGC 里用同一个新包名创建的。";
            if (output.IndexOf("need connect-key", StringComparison.OrdinalIgnoreCase) >= 0 ||
                output.IndexOf("no devices", StringComparison.OrdinalIgnoreCase) >= 0 ||
                output.IndexOf("Connect failed", StringComparison.OrdinalIgnoreCase) >= 0)
                return "\r\n\r\n提示：手表没连上，先回「主页」重新连接再试。";
            return string.Empty;
        }

        /// <summary>
        /// AGC 下载的证书可能是 DER 也可能是 PEM，统一转成 PEM 再交给签名工具。
        /// </summary>
        private static string PrepareCertFile(string certFile, string workDir, Action<string> log)
        {
            byte[] bytes = File.ReadAllBytes(certFile);
            string text = Encoding.ASCII.GetString(bytes);

            if (text.IndexOf("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal) >= 0)
            {
                string pemPath = Path.Combine(workDir, "app-cert.pem");
                File.WriteAllText(pemPath, text, new UTF8Encoding(false));
                return pemPath;
            }

            using X509Certificate2 certificate = new X509Certificate2(bytes);
            string converted = Path.Combine(workDir, "app-cert.pem");
            File.WriteAllText(converted, certificate.ExportCertificatePem(), new UTF8Encoding(false));
            log("证书是 DER 格式，已转换为 PEM。");
            return converted;
        }

        private static string LastLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "（无输出）";

            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Length == 0 ? text.Trim() : lines[lines.Length - 1].Trim();
        }
    }
}
