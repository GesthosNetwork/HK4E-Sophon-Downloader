using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Mono.Options;

namespace Core.Runner
{
    public static class CliHandler
    {
        public static async Task<int> RunWithArgs(string[] args)
        {
            bool showHelp = false;
            string action = "", gameId = "", updateFrom = "", updateTo = "", outputDir = "", matchingField = "";

            string EnsureNotEmpty(string v, string name)
            {
                if (string.IsNullOrWhiteSpace(v))
                    throw new OptionException($"Missing value for --{name}", name);
                return v.Trim();
            }

            var options = new OptionSet
            {
                { "region=", "Region: OSREL or CNREL", v =>
                    {
                        var region = EnsureNotEmpty(v, "region").ToUpperInvariant();
                        if (region != "OSREL" && region != "CNREL")
                            throw new OptionException("Invalid value for --region (must be OSREL or CNREL)", "region");

                        AppConfig.Config.Region = region;
                    }
                },
                { "branch=", "Branch name override", v =>
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
                            throw new OptionException("Invalid value for --threads (must be positive integer)", "threads");

                        if (val > 32 && !AppConfig.Config.Silent)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("[WARN] Using more than 32 threads may not be optimal for all systems.");
                            Console.ResetColor();
                        }

                        AppConfig.Config.Threads = val;
                    }
                },
                { "handles=", "HTTP handles", v =>
                    {
                        if (!int.TryParse(v, out int val) || val <= 0)
                            throw new OptionException("Invalid value for --handles (must be positive integer)", "handles");

                        AppConfig.Config.MaxHttpHandle = val;
                    }
                },
                { "silent", "Silent mode", _ => AppConfig.Config.Silent = true },
                { "h|help", "Show help", _ => showHelp = true },
            };

            try
            {
                List<string> extra = options.Parse(args);
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
                    try
                    {
                        string fullPath = Path.GetFullPath(outputDir);
                        Directory.CreateDirectory(fullPath);
                    }
                    catch
                    {
                        throw new OptionException("Invalid output directory path.", "outputDir");
                    }
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

                    Arguments:
                      <gameId>     Game ID (example: gopR6Cufr3)
                      <package>    Language or resource type (example: game, en-us, ja-jp)
                      <version>    Game version (example: 5.6)
                      <outputDir>  Target folder for downloaded files

                    Game ID:
                      gopR6Cufr3   game id for hk4e OSREL
                      1Z8W5NHUQb   game id for hk4e CNREL

                    Options:
                      --region=OSREL|CNREL        Region code (default: OSREL)
                      --branch=<value>            Branch override (default: main)
                      --launcherId=<value>        Override Launcher ID
                      --platApp=<value>           Override Platform App ID
                      --threads=<n>               Number of download threads
                      --handles=<n>               Max HTTP handles (default: 128, max: 512)
                      --silent                    Disable all console output except errors
                      -h, --help                  Show this help message

                    Example:
                      Sophon.Downloader.exe full gopR6Cufr3 game 5.6 Downloads
                      Sophon.Downloader.exe update gopR6Cufr3 en-us 5.5 5.6 Downloads --threads=4 --handles=64
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
