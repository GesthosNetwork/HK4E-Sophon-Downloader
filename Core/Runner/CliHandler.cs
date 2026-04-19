using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mono.Options;

namespace Core.Runner
{
    public static class CliHandler
    {
        private static string EnsureNotEmpty(string v, string name)
        {
            if (string.IsNullOrWhiteSpace(v))
                throw new OptionException($"Missing value for --{name}", name);
            return v.Trim();
        }

        public static void ParseArgsAndSetConfig(string[] args)
        {
            var options = new OptionSet
            {
                { "region=", "Region: OSREL or CNREL", v =>
                    {
                        var region = EnsureNotEmpty(v, "region").ToUpperInvariant();
                        if (region != "OSREL" && region != "CNREL")
                            throw new OptionException("Invalid value for --region", "region");
                        AppConfig.Config.Region = region;
                    }
                },
                { "branch=", "Branch override", v =>
                    AppConfig.Config.Branch = EnsureNotEmpty(v, "branch")
                },
                { "launcherId=", "Launcher ID override", v =>
                    AppConfig.Config.LauncherId = EnsureNotEmpty(v, "launcherId")
                },
                { "platApp=", "Platform App ID override", v =>
                    AppConfig.Config.PlatApp = EnsureNotEmpty(v, "platApp")
                },
                { "threads=", "Threads to use", v =>
                    {
                        if (!int.TryParse(v, out int val) || val <= 0)
                            throw new OptionException("Invalid value for --threads", "threads");
                        AppConfig.Config.Threads = val;
                    }
                },
                { "handles=", "HTTP handles", v =>
                    {
                        if (!int.TryParse(v, out int val) || val <= 0)
                            throw new OptionException("Invalid value for --handles", "handles");
                        AppConfig.Config.MaxHttpHandle = val;
                    }
                },
                { "silent", "Silent mode", _ => AppConfig.Config.Silent = true },
                { "CNREL", "Switch to CN region", _ =>
                    {
                        AppConfig.Config.Region = "CNREL";
                        AppConfig.Config.LauncherId = "jGHBHlcOq1";
                        AppConfig.Config.PlatApp = "ddxf5qt290cg";
                    }
                },
                { "OSREL", "Switch to OS region", _ =>
                    {
                        AppConfig.Config.Region = "OSREL";
                        AppConfig.Config.LauncherId = "VYTpXlbWo8";
                        AppConfig.Config.PlatApp = "ddxf6vlr1reo";
                    }
                },
                { "h|help", "Show help", _ => {} },
            };

            options.Parse(args);

            if (args.Contains("--main"))
                AppConfig.Config.Branch = "main";
            else if (args.Contains("--predownload"))
                AppConfig.Config.Branch = "predownload";

            AppConfig.Config.SetPasswordByBranch();
        }

        public static async Task<int> RunWithArgs(string[] args)
        {
            bool showHelp = false;
            string action = "", gameId = "", updateFrom = "", updateTo = "", outputDir = "", matchingField = "";

            try
            {
                List<string> extra = new OptionSet().Parse(args);
                int count = extra.Count;
                action = count > 1 ? extra[0].ToLowerInvariant() : "";

                if (action == "full" && count >= 5)
                {
                    gameId = extra[1];
                    matchingField = extra[2];
                    updateFrom = extra[3];
                    outputDir = extra[4];
                }
                else if (action == "update" && count >= 6)
                {
                    gameId = extra[1];
                    matchingField = extra[2];
                    updateFrom = extra[3];
                    updateTo = extra[4];
                    outputDir = extra[5];
                }
                else
                {
                    showHelp = true;
                }

                if (!showHelp)
                {
                    string fullPath = Path.GetFullPath(outputDir);
                    Directory.CreateDirectory(fullPath);
                }
            }
            catch (OptionException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + e.Message);
                Console.ResetColor();
                Console.WriteLine("Use --help to see usage information.");
                return 1;
            }

            if (showHelp)
            {
                Console.WriteLine("""
                    Sophon Downloader - Command Line Interface

                    Usage:
                      Sophon.Downloader.exe full   <gameId> <package> <version> <outputDir> [options]
                      Sophon.Downloader.exe update <gameId> <package> <fromVer> <toVer> <outputDir> [options]

                    Example:
                      Sophon.Downloader.exe full hk4e game 6.5 Downloads
                      Sophon.Downloader.exe update hk4e en-us 6.5 6.6 Downloads --predownload --OSREL --threads=4 --handles=32
                """);
                return 0;
            }

            var preparedArgs = action == "full"
                ? new[] { action, gameId, matchingField, updateFrom, outputDir }
                : new[] { action, gameId, matchingField, updateFrom, updateTo, outputDir };

            await DownloadExecutor.RunDownload(preparedArgs);
            return 0;
        }
    }
}
