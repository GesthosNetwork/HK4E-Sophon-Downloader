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
        public int Threads { get; set; } = Environment.ProcessorCount;
        public int MaxHttpHandle { get; set; } = 128;
        public bool Silent { get; set; } = false;
        public VersionsConfig Versions { get; set; }

        private static readonly string ConfigPath = "config.json";
        public static AppConfig Config { get; private set; } = LoadInternal();

        public AppConfig()
        {
            Versions = new VersionsConfig();
        }

        private static AppConfig LoadInternal()
        {
            if (!File.Exists(ConfigPath))
            {
                var defaultConfig = new AppConfig();
                defaultConfig.Versions.Full = new() { "5.6", "5.7" };
                defaultConfig.Versions.Update = new()
                {
                    new() { "5.5", "5.6" },
                    new() { "5.5", "5.7" },
                    new() { "5.6", "5.7" }
                };
                defaultConfig.Save();
                Console.WriteLine("[INFO] config.json not found. Created default config.");
                return defaultConfig;
            }

            try
            {
                string json = File.ReadAllText(ConfigPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var config = new AppConfig();

                if (root.TryGetProperty("Region", out var regionProp))
                {
                    string regionVal = regionProp.GetString()?.ToUpperInvariant() ?? "";
                    if (regionVal == "OSREL" || regionVal == "CNREL")
                        config.Region = regionVal;
                }

                if (root.TryGetProperty("Branch", out var branchProp))
                {
                    string branchVal = branchProp.GetString() ?? "";
                    config.Branch = branchVal == "main" ? "main" : "main";
                }

                if (root.TryGetProperty("LauncherId", out var launcherProp))
                    config.LauncherId = launcherProp.GetString() ?? config.LauncherId;

                if (root.TryGetProperty("PlatApp", out var platAppProp))
                    config.PlatApp = platAppProp.GetString() ?? config.PlatApp;

                if (root.TryGetProperty("Threads", out var threadsProp) && threadsProp.TryGetInt32(out int threadsVal))
                {
                    config.Threads = threadsVal > 0 && threadsVal <= Environment.ProcessorCount
                        ? threadsVal
                        : Environment.ProcessorCount;
                }

                if (root.TryGetProperty("MaxHttpHandle", out var handleProp) && handleProp.TryGetInt32(out int handlesVal))
                {
                    config.MaxHttpHandle = handlesVal > 0 && handlesVal <= 512 ? handlesVal : 128;
                }

                config.Silent = root.TryGetProperty("Silent", out var silentProp) && silentProp.ValueKind == JsonValueKind.True;

                var versions = new VersionsConfig();

                if (root.TryGetProperty("Versions", out var versionsProp))
                {
                    if (versionsProp.TryGetProperty("full", out var fullProp) && fullProp.ValueKind == JsonValueKind.Array)
                    {
                        versions.Full = fullProp.EnumerateArray()
                            .Where(x => x.ValueKind == JsonValueKind.String)
                            .Select(x => x.GetString()!)
                            .Where(v => !string.IsNullOrWhiteSpace(v))
                            .ToList();
                    }

                    if (versionsProp.TryGetProperty("update", out var updateProp) && updateProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var pair in updateProp.EnumerateArray())
                        {
                            if (pair.ValueKind == JsonValueKind.Array)
                            {
                                var arr = pair.EnumerateArray()
                                    .Where(x => x.ValueKind == JsonValueKind.String)
                                    .Select(x => x.GetString()!)
                                    .Where(v => !string.IsNullOrWhiteSpace(v))
                                    .ToList();

                                if (arr.Count == 2)
                                    versions.Update.Add(arr);
                            }
                        }
                    }
                }

                if (versions.Full.Count == 0)
                    versions.Full = new() { "5.6", "5.7" };

                if (versions.Update.Count == 0)
                {
                    versions.Update = new()
                    {
                        new() { "5.5", "5.6" },
                        new() { "5.5", "5.7" },
                        new() { "5.6", "5.7" }
                    };
                }

                config.Versions = versions;

                switch (config.Region)
                {
                    case "CNREL":
                        config.LauncherId = "jGHBHlcOq1";
                        config.PlatApp = "ddxf5qt290cg";
                        break;

                    case "OSREL":
                    default:
                        config.LauncherId = "VYTpXlbWo8";
                        config.PlatApp = "ddxf6vlr1reo";
                        break;
                }

                config.Save();
                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Failed to load config.json: {ex.Message}");
                Console.WriteLine("[INFO] Using default configuration.");
                var fallback = new AppConfig();
                fallback.Versions.Full = new() { "5.6", "5.7" };
                fallback.Versions.Update = new()
                {
                    new() { "5.5", "5.6" },
                    new() { "5.5", "5.7" },
                    new() { "5.6", "5.7" }
                };
                fallback.Save();
                return fallback;
            }
        }

        public void Save()
        {
            var sb = new StringBuilder();
            void W(int level, string text) => AddIndented(sb, level, text);

            W(0, "{");
            W(1, $"\"Region\": \"{Region}\",");
            W(1, $"\"Branch\": \"{Branch}\",");
            W(1, $"\"LauncherId\": \"{LauncherId}\",");
            W(1, $"\"PlatApp\": \"{PlatApp}\",");
            W(1, $"\"Threads\": {Threads},");
            W(1, $"\"MaxHttpHandle\": {MaxHttpHandle},");
            W(1, $"\"Silent\": {Silent.ToString().ToLower()},");
            W(1, "\"Versions\": {");

            W(2, "\"full\": [" + string.Join(", ", Versions.Full.Select(v => $"\"{v}\"")) + "],");

            W(2, "\"update\": [");
            for (int i = 0; i < Versions.Update.Count; i++)
            {
                var pair = Versions.Update[i];
                string line = "[" + string.Join(", ", pair.Select(v => $"\"{v}\"")) + "]";
                W(3, line + (i < Versions.Update.Count - 1 ? "," : ""));
            }
            W(2, "]");
            W(1, "}");
            W(0, "}");

            File.WriteAllText(ConfigPath, sb.ToString());
        }

        private void AddIndented(StringBuilder sb, int indentLevel, string text)
        {
            sb.AppendLine(new string(' ', indentLevel * 2) + text);
        }
    }

    public class VersionsConfig
    {
        public List<string> Full { get; set; } = new();
        public List<List<string>> Update { get; set; } = new();
    }
}
