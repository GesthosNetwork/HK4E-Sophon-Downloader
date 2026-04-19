using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Core
{
    public class AppConfig
    {
        public string Region { get; set; } = "OSREL";
        public string Branch { get; set; } = "main";
        public string LauncherId { get; set; } = "VYTpXlbWo8";
        public string PlatApp { get; set; } = "ddxf6vlr1reo";
        public string Password { get; set; } = "bDL4JUHL625x";
        public int Threads { get; set; } = Math.Max(1, Environment.ProcessorCount / 2);
        public int MaxHttpHandle { get; set; } = 32;
        public bool Silent { get; set; } = false;
        public VersionsConfig Versions { get; set; } = new();

        private static readonly string ConfigPath = "config.json";
        public static AppConfig Config { get; private set; } = LoadInternal();

        private static VersionsConfig GetDefaultVersions() => new()
        {
            Full = new() { "6.0", "6.1", "6.2", "6.3", "6.4", "6.5" },
            Update = new()
            {
                new() { "6.0", "6.1" }, new() { "6.1", "6.2" },
                new() { "6.2", "6.3" }, new() { "6.3", "6.4" },
                new() { "6.4", "6.5" }
            }
        };

        private static AppConfig LoadInternal()
        {
            if (!File.Exists(ConfigPath))
            {
                var cfg = new AppConfig { Versions = GetDefaultVersions() };
                cfg.SetPasswordByBranch();
                cfg.Save();
                Console.WriteLine("[INFO] config.json not found. Created default config.");
                return cfg;
            }

            try
            {
                var root = JsonDocument.Parse(File.ReadAllText(ConfigPath)).RootElement;
                var cfg = new AppConfig();

                string? region = root.GetPropertyOrNull("Region")?.GetString()?.ToUpperInvariant();
                if (region is "OSREL" or "CNREL") cfg.Region = region;

                string? branch = root.GetPropertyOrNull("Branch")?.GetString();
                if (!string.IsNullOrWhiteSpace(branch)) cfg.Branch = branch;

                cfg.LauncherId = root.GetPropertyOrNull("LauncherId")?.GetString() ?? cfg.LauncherId;
                cfg.PlatApp = root.GetPropertyOrNull("PlatApp")?.GetString() ?? cfg.PlatApp;

                if (root.TryGetInt("Threads", out int t))
                    cfg.Threads = t > 0 && t <= Environment.ProcessorCount
                        ? t
                        : Math.Max(1, Environment.ProcessorCount / 2);

                if (root.TryGetInt("MaxHttpHandle", out int h))
                    cfg.MaxHttpHandle = h > 0 && h <= 512 ? h : 128;

                cfg.Silent = root.TryGetProperty("Silent", out var sp) &&
                             sp.ValueKind == JsonValueKind.True;

                var versions = new VersionsConfig();

                if (root.TryGetProperty("Versions", out var v))
                {
                    if (v.TryGetProperty("full", out var full) && full.ValueKind == JsonValueKind.Array)
                    {
                        versions.Full = full.EnumerateArray()
                            .Where(x => x.ValueKind == JsonValueKind.String)
                            .Select(x => x.GetString()!)
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList();
                    }

                    if (v.TryGetProperty("update", out var upd) && upd.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var arr in upd.EnumerateArray()
                                               .Where(x => x.ValueKind == JsonValueKind.Array))
                        {
                            var list = arr.EnumerateArray()
                                .Where(x => x.ValueKind == JsonValueKind.String)
                                .Select(x => x.GetString()!)
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .ToList();

                            if (list.Count == 2) versions.Update.Add(list);
                        }
                    }
                }

                if (versions.Full.Count == 0 || versions.Update.Count == 0)
                    versions = GetDefaultVersions();

                cfg.Versions = versions;

                if (cfg.Region == "CNREL")
                {
                    cfg.LauncherId = "jGHBHlcOq1";
                    cfg.PlatApp = "ddxf5qt290cg";
                }
                else
                {
                    cfg.LauncherId = "VYTpXlbWo8";
                    cfg.PlatApp = "ddxf6vlr1reo";
                }

                cfg.SetPasswordByBranch();

                cfg.Save();
                return cfg;
            }
            catch
            {
                var fallback = new AppConfig { Versions = GetDefaultVersions() };
                fallback.SetPasswordByBranch();
                fallback.Save();
                return fallback;
            }
        }

        public void SetPasswordByBranch()
        {
            Password = Branch switch
            {
                "main" => "bDL4JUHL625x",
                "predownload" => "ZOJpUiKu4Sme",
                _ => ""
            };
        }

        public void Save()
        {
            var sb = new StringBuilder();

            void W(int l, string t) =>
                sb.AppendLine(new string(' ', l * 2) + t);

            W(0, "{");
            W(1, $"\"Region\": \"{Region}\",");
            W(1, $"\"Branch\": \"{Branch}\",");
            W(1, $"\"LauncherId\": \"{LauncherId}\",");
            W(1, $"\"PlatApp\": \"{PlatApp}\",");
            W(1, $"\"Password\": \"{Password}\",");
            W(1, $"\"Threads\": {Threads},");
            W(1, $"\"MaxHttpHandle\": {MaxHttpHandle},");
            W(1, $"\"Silent\": {Silent.ToString().ToLower()},");
            W(1, "\"Versions\": {");
            W(2, "\"full\": [" + string.Join(", ", Versions.Full.Select(v => $"\"{v}\"")) + "],");
            W(2, "\"update\": [");

            for (int i = 0; i < Versions.Update.Count; i++)
            {
                W(3,
                    "[" + string.Join(", ", Versions.Update[i].Select(v => $"\"{v}\"")) + "]" +
                    (i < Versions.Update.Count - 1 ? "," : ""));
            }

            W(2, "]");
            W(1, "}");
            W(0, "}");

            File.WriteAllText(ConfigPath, sb.ToString());
        }
    }

    public class VersionsConfig
    {
        public List<string> Full { get; set; } = new();
        public List<List<string>> Update { get; set; } = new();
    }

    static class JsonExt
    {
        public static JsonElement? GetPropertyOrNull(this JsonElement e, string name) =>
            e.TryGetProperty(name, out var val) ? val : (JsonElement?)null;

        public static bool TryGetInt(this JsonElement e, string name, out int value)
        {
            value = 0;
            return e.TryGetProperty(name, out var val) && val.TryGetInt32(out value);
        }
    }
}
