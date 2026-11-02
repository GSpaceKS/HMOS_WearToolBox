#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace HMOS_WearToolBox.Helper
{
    /// <summary>
    /// 从嵌入资源 app_names.json 加载 包名 → 中文名 映射，进程内只读一次。
    /// </summary>
    public static class AppNameResolver
    {
        private static Dictionary<string, string> cache;

        public static Dictionary<string, string> Load()
        {
            if (cache != null) return cache;

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(r => r.EndsWith("app_names.json", StringComparison.OrdinalIgnoreCase));
                if (resourceName != null)
                {
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    using (var reader = new StreamReader(stream))
                    {
                        var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadToEnd());
                        if (loaded != null)
                            foreach (var kv in loaded) map[kv.Key] = kv.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("加载应用名称映射失败: " + ex.Message);
            }

            cache = map;
            return cache;
        }

        /// <summary>
        /// 先查内置映射（原厂应用），再查「本工具改过名」的记录（用安装包文件名当名字），
        /// 都没有就返回包名本身，避免界面上出现「未知」。
        /// </summary>
        public static string Resolve(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName)) return "";

            var map = Load();
            string name;
            if (map.TryGetValue(bundleName, out name) && !string.IsNullOrWhiteSpace(name))
                return name;

            RenameRecord renamed;
            if (RenameStore.TryGet(bundleName, out renamed))
                return renamed.Name;

            return bundleName;
        }
    }
}
