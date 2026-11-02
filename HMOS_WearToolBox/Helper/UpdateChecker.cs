using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace HMOS_WearToolBox.Helper
{
    /// <summary>检查 GitHub Releases 上的新版本；失败时返回结果而不是抛异常。</summary>
    public static class UpdateChecker
    {
        public const string Repository = "GSpaceKS/HMOS_WearToolBox";
        public const string ReleasesUrl = "https://github.com/" + Repository + "/releases";

        private const string LatestApiUrl = "https://api.github.com/repos/" + Repository + "/releases/latest";
        private const string ListApiUrl = "https://api.github.com/repos/" + Repository + "/releases?per_page=30";

        /// <summary>设置文件：%APPDATA%\HMOS_WearToolBox\update.json，和快照、签名物料放在一起。</summary>
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HMOS_WearToolBox",
            "update.json");

        private static readonly object Sync = new object();
        private static Settings? cache;

        private sealed class Settings
        {
            public bool IncludeBeta { get; set; }

            public string Token { get; set; } = string.Empty;
        }

        private static Settings Current
        {
            get
            {
                lock (Sync)
                {
                    if (cache == null)
                        cache = LoadSettings();
                    return cache;
                }
            }
        }

        /// <summary>是否接收 beta / rc 这类预发布版本。</summary>
        public static bool IncludeBeta
        {
            get { return Current.IncludeBeta; }
            set
            {
                lock (Sync)
                {
                    Current.IncludeBeta = value;
                    SaveSettings();
                }
            }
        }

        /// <summary>GitHub 个人访问令牌（可留空，留空走匿名限额）。</summary>
        public static string Token
        {
            get { return Current.Token; }
            set
            {
                lock (Sync)
                {
                    Current.Token = (value ?? string.Empty).Trim();
                    SaveSettings();
                }
            }
        }

        private static Settings LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new Settings();

                Settings? settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsPath));
                return settings ?? new Settings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("读取更新设置失败: " + ex.Message);
                return new Settings();
            }
        }

        private static void SaveSettings()
        {
            try
            {
                string folder = Path.GetDirectoryName(SettingsPath) ?? string.Empty;
                if (folder.Length > 0)
                    Directory.CreateDirectory(folder);

                string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                string tempPath = SettingsPath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, SettingsPath, overwrite: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("保存更新设置失败: " + ex.Message);
            }
        }

        /// <summary>当前程序版本（取程序集里的 InformationalVersion，如 1.0.0-beta3）。</summary>
        public static string CurrentVersion
        {
            get
            {
                string? informational = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

                if (!string.IsNullOrWhiteSpace(informational))
                    return informational.Trim();

                return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
            }
        }

        public sealed class Result
        {
            public bool Succeeded { get; set; }
            public bool HasUpdate { get; set; }
            public string LatestTag { get; set; } = string.Empty;
            public string ReleaseUrl { get; set; } = ReleasesUrl;
            public string Message { get; set; } = string.Empty;
            public bool IsPrerelease { get; set; }
        }

        /// <summary>从 GitHub 上取到的一条 Release（只留我们比对需要的字段）。</summary>
        public sealed class ReleaseInfo
        {
            public string Tag { get; set; } = string.Empty;
            public string Url { get; set; } = ReleasesUrl;
            public bool Prerelease { get; set; }
            public bool Draft { get; set; }
        }

        /// <summary>纯比对逻辑，不含网络调用。</summary>
        public static Result Evaluate(IEnumerable<ReleaseInfo> releases, string current, bool includeBeta)
        {
            Result result = new Result();

            List<ReleaseInfo> candidates = releases
                .Where(release => !release.Draft)
                .Where(release => includeBeta || !release.Prerelease)
                .Where(release => !string.IsNullOrWhiteSpace(release.Tag))
                .ToList();

            if (candidates.Count == 0)
            {
                result.Message = includeBeta
                    ? "仓库里还没有发布任何 Release，可以去 Releases 页面看看。"
                    : "仓库里还没有发布正式版，可以去 Releases 页面自己看看（或在下面勾选接收 beta 版）。";
                return result;
            }

            ReleaseInfo best = candidates[0];
            foreach (ReleaseInfo candidate in candidates)
            {
                if (IsNewer(candidate.Tag, best.Tag))
                    best = candidate;
            }

            result.Succeeded = true;
            result.LatestTag = best.Tag;
            result.ReleaseUrl = best.Url;
            result.IsPrerelease = best.Prerelease;
            result.HasUpdate = IsNewer(best.Tag, current);
            result.Message = result.HasUpdate
                ? "发现新版本 " + best.Tag + (best.Prerelease ? "（beta）" : string.Empty) + "（当前 " + current + "）。"
                : "已经是最新版本（" + current + "）。";

            return result;
        }

        /// <param name="includeBeta">为空时用界面里的勾选状态（<see cref="IncludeBeta"/>）。</param>
        public static async Task<Result> CheckAsync(bool? includeBeta = null)
        {
            bool withBeta = includeBeta ?? IncludeBeta;
            return withBeta ? await CheckIncludingBetaAsync().ConfigureAwait(false) : await CheckStableAsync().ConfigureAwait(false);
        }

        public sealed class TokenCheck
        {
            /// <summary>请求本身是否成功发出（网络失败时为 false）。</summary>
            public bool Succeeded { get; set; }

            public bool Valid { get; set; }

            public string Login { get; set; } = string.Empty;

            public string Message { get; set; } = string.Empty;
        }

        /// <summary>验证令牌是否有效。</summary>
        public static async Task<TokenCheck> VerifyTokenAsync(string token)
        {
            string value = (token ?? string.Empty).Trim();
            if (value.Length == 0)
            {
                return new TokenCheck { Message = "还没有填令牌。留空保存会退回匿名访问（60 次/小时）。" };
            }

            try
            {
                using HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("HMOS_WearToolBox/" + CurrentVersion);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", value);

                using HttpResponseMessage response = await client.GetAsync("https://api.github.com/user").ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    string login = string.Empty;
                    try
                    {
                        using JsonDocument document = JsonDocument.Parse(json);
                        if (document.RootElement.TryGetProperty("login", out JsonElement loginElement))
                            login = loginElement.GetString() ?? string.Empty;
                    }
                    catch (JsonException)
                    {
                    }

                    return new TokenCheck
                    {
                        Succeeded = true,
                        Valid = true,
                        Login = login,
                        Message = login.Length > 0
                            ? "令牌有效（账号 " + login + "），限额提升到 5000 次/小时。"
                            : "令牌有效，限额提升到 5000 次/小时。"
                    };
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new TokenCheck
                    {
                        Succeeded = true,
                        Message = "GitHub 说这个令牌无效（401）：可能少复制了几位、已经过期或者被撤销了。"
                    };
                }

                return new TokenCheck
                {
                    Succeeded = true,
                    Message = "GitHub 返回 " + (int)response.StatusCode + "，暂时没法确认这个令牌能不能用。"
                };
            }
            catch (Exception ex)
            {
                return new TokenCheck { Message = "没法验证（网络不通）：" + ex.Message };
            }
        }

        private static HttpClient CreateClient()
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("HMOS_WearToolBox/" + CurrentVersion);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            // 填了令牌就带上：匿名 60 次/小时 → 认证 5000 次/小时。
            string token = Token;
            if (token.Length > 0)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        /// <summary>把失败状态码翻成人话。403/429 基本都是限额用尽。</summary>
        private static string DescribeFailure(int status)
        {
            if (status == 403 || status == 429)
            {
                return Token.Length > 0
                    ? "GitHub 拒绝了这次请求（" + status + "）。令牌可能已过期或没有读仓库的权限。"
                    : "GitHub 的接口限额用完了（" + status + "）：匿名每小时只有 60 次。在下面填一个令牌可以提到 5000 次/小时。";
            }

            return "GitHub 返回 " + status + "，可能被限流或网络不通。";
        }

        /// <summary>只看正式版：/releases/latest 本身就排除 draft 与 prerelease。</summary>
        private static async Task<Result> CheckStableAsync()
        {
            try
            {
                using HttpClient client = CreateClient();
                using HttpResponseMessage response = await client.GetAsync(LatestApiUrl).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return new Result
                    {
                        Message = (int)response.StatusCode == 404
                        ? "仓库里还没有发布正式版，可以去 Releases 页面自己看看（或在下面勾选接收 beta 版）。"
                        : DescribeFailure((int)response.StatusCode)
                    };
                }

                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return Evaluate(ParseReleases(json), CurrentVersion, includeBeta: false);
            }
            catch (Exception ex)
            {
                return new Result { Message = "检查更新失败：" + ex.Message };
            }
        }

        /// <summary>包含预发布：从 Releases 列表里挑版本号最高的一条。</summary>
        private static async Task<Result> CheckIncludingBetaAsync()
        {
            try
            {
                using HttpClient client = CreateClient();
                using HttpResponseMessage response = await client.GetAsync(ListApiUrl).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return new Result
                    {
                        Message = (int)response.StatusCode == 404
                        ? "仓库里还没有发布任何 Release，可以去 Releases 页面看看。"
                        : DescribeFailure((int)response.StatusCode)
                    };
                }

                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return Evaluate(ParseReleases(json), CurrentVersion, includeBeta: true);
            }
            catch (Exception ex)
            {
                return new Result { Message = "检查更新失败：" + ex.Message };
            }
        }

        /// <summary>把 GitHub 的 Release JSON（单个对象或数组）解析成候选列表。纯函数，离线可测。</summary>
        public static List<ReleaseInfo> ParseReleases(string json)
        {
            List<ReleaseInfo> list = new List<ReleaseInfo>();
            if (string.IsNullOrWhiteSpace(json))
                return list;

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in root.EnumerateArray())
                    list.Add(ToRelease(item));
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                list.Add(ToRelease(root));
            }

            return list;
        }

        private static ReleaseInfo ToRelease(JsonElement element)
        {
            return new ReleaseInfo
            {
                Tag = element.TryGetProperty("tag_name", out JsonElement tag) ? tag.GetString() ?? string.Empty : string.Empty,
                Url = element.TryGetProperty("html_url", out JsonElement url) ? url.GetString() ?? ReleasesUrl : ReleasesUrl,
                Prerelease = element.TryGetProperty("prerelease", out JsonElement pre) && pre.ValueKind == JsonValueKind.True,
                Draft = element.TryGetProperty("draft", out JsonElement draft) && draft.ValueKind == JsonValueKind.True
            };
        }


        /// <summary>比较版本号，拿不准时一律认为没有新版本。</summary>
        public static bool IsNewer(string remote, string current)
        {
            (int[] numbers, string suffix) Parse(string value)
            {
                string text = (value ?? string.Empty).Trim().TrimStart('v', 'V');
                int dash = text.IndexOf('-');
                string numericPart = dash >= 0 ? text.Substring(0, dash) : text;
                string suffixPart = dash >= 0 ? text.Substring(dash + 1) : string.Empty;

                int[] parts = numericPart
                    .Split('.', StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => int.TryParse(part, out int number) ? number : 0)
                    .ToArray();

                return (parts, suffixPart.ToLowerInvariant());
            }

            (int[] remoteNumbers, string remoteSuffix) = Parse(remote);
            (int[] currentNumbers, string currentSuffix) = Parse(current);

            if (remoteNumbers.Length == 0)
                return false;

            for (int i = 0; i < Math.Max(remoteNumbers.Length, currentNumbers.Length); i++)
            {
                int remoteNumber = i < remoteNumbers.Length ? remoteNumbers[i] : 0;
                int currentNumber = i < currentNumbers.Length ? currentNumbers[i] : 0;

                if (remoteNumber != currentNumber)
                    return remoteNumber > currentNumber;
            }

            // 数字段完全一致，比预发布后缀：没有后缀 = 正式版，比任何 beta 都新
            if (string.IsNullOrEmpty(remoteSuffix))
                return !string.IsNullOrEmpty(currentSuffix);

            if (string.IsNullOrEmpty(currentSuffix))
                return false;

            if (remoteSuffix == currentSuffix)
                return false;

            return CompareSuffix(remoteSuffix, currentSuffix) > 0;
        }

        /// <summary>比较 beta3 / rc1 这类后缀：前缀不同按字符串比，前缀相同比尾部的数字。</summary>
        private static int CompareSuffix(string remote, string current)
        {
            string remotePrefix = new string(remote.TakeWhile(char.IsLetter).ToArray());
            string currentPrefix = new string(current.TakeWhile(char.IsLetter).ToArray());

            if (remotePrefix != currentPrefix)
                return string.CompareOrdinal(remotePrefix, currentPrefix);

            string remoteDigits = new string(remote.SkipWhile(char.IsLetter).ToArray());
            string currentDigits = new string(current.SkipWhile(char.IsLetter).ToArray());

            int remoteNumber = int.TryParse(remoteDigits, out int a) ? a : 0;
            int currentNumber = int.TryParse(currentDigits, out int b) ? b : 0;

            return remoteNumber.CompareTo(currentNumber);
        }
    }
}
