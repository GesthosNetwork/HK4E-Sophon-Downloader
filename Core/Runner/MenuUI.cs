using System;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Runner
{
    public static class MenuUI
    {
        public static async Task<int> RunInteractiveMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Sophon Downloader ===\n");
                Console.WriteLine("[1] Full Download");
                Console.WriteLine("[2] Update Download");
                Console.WriteLine("[0] Exit");
                Console.Write("\nChoose: ");

                string input = Console.ReadLine()?.Trim() ?? "";
                if (input == "0") return 0;
                if (input == "1") await RunDownloadCategoryMenu("full");
                else if (input == "2") await RunDownloadCategoryMenu("update");
            }
        }

        private static async Task RunDownloadCategoryMenu(string mode)
        {
            string[] langs = AppConfig.Config.Region == "CNREL"
                ? new[] { "game", "zh-cn" }
                : new[] { "game", "en-us", "ja-jp", "zh-cn", "ko-kr" };

            while (true)
            {
                Console.Clear();
                Console.WriteLine($"=== {(mode == "full" ? "Full" : "Update")} Download ===\n");

                for (int i = 0; i < langs.Length; i++)
                    Console.WriteLine($"[{i + 1}] {langs[i]}");

                Console.WriteLine("[0] Back");
                Console.Write("\nChoose: ");

                string input = Console.ReadLine()?.Trim() ?? "";
                if (input == "0") return;

                if (int.TryParse(input, out int c) && c >= 1 && c <= langs.Length)
                    await RunVersionPickerMenu(mode, langs[c - 1]);
            }
        }

        private static async Task RunVersionPickerMenu(string mode, string lang)
        {
            string[][] versions = mode == "full"
                ? AppConfig.Config.Versions.Full.Select(v => new[] { v }).ToArray()
                : AppConfig.Config.Versions.Update.Select(x => x.ToArray()).ToArray();

            Region region = Enum.TryParse(AppConfig.Config.Region, out Region parsedRegion)
                ? parsedRegion : Region.OSREL;

            string gameId = new Game(region, Game.GameType.hk4e.ToString()).GetGameId();

            while (true)
            {
                Console.Clear();
                Console.WriteLine($"=== {(mode == "full" ? "Full" : "Update")} Download: {lang} ===\n");

                for (int i = 0; i < versions.Length; i++)
                {
                    var v = versions[i];
                    string label = mode == "full" ? $"Version {v[0]}" : $"From {v[0]} → {v[1]}";
                    Console.WriteLine($"[{i + 1}] {label}");
                }

                Console.WriteLine("[0] Back");
                Console.Write("\nChoose: ");

                string input = Console.ReadLine()?.Trim() ?? "";
                if (input == "0") return;

                if (int.TryParse(input, out int choice) && choice >= 1 && choice <= versions.Length)
                {
                    string[] ver = versions[choice - 1];
                    string[] argsToRun = mode == "full"
                        ? new[] { "full", gameId, lang, ver[0], "Downloads" }
                        : new[] { "update", gameId, lang, ver[0], ver[1], "Downloads" };

                    Console.Clear();
                    Console.WriteLine($"Executing:\nSophon.Downloader.exe {string.Join(" ", argsToRun)}\n");

                    await DownloadExecutor.RunDownload(argsToRun);
                    Console.WriteLine("\nPress any key to return...");
                    Console.ReadKey();
                }
            }
        }
    }
}
