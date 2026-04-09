using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Core.Utils;
using Sophon;
using Sophon.Structs;

namespace Core
{
    internal class Downloader
    {
        private static string _statusMessage = string.Empty;
        private static bool _isRetry = true;

        private static int _lastLineLength = 0;

        public static async Task<int> StartDownload(string prevManifestUrl, string newManifestUrl, string outputDir, string matchingField)
        {
        StartDownload:

            _isRetry = false;
            _statusMessage = "Downloading...";
            _lastLineLength = 0;

            CancellationTokenSource tokenSource = new();
            HttpClientHandler httpHandler = new() { MaxConnectionsPerServer = AppConfig.Config.MaxHttpHandle };
            HttpClient httpClient = new(httpHandler)
            {
                DefaultRequestVersion = HttpVersion.Version30,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
            };

            using (tokenSource)
            using (httpHandler)
            using (httpClient)
            {
                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                if (!AppConfig.Config.Silent)
                    Console.WriteLine("Fetching assets...");

                var result = await Assets.GetAssetsFromManifests(
                    httpClient,
                    matchingField,
                    prevManifestUrl,
                    newManifestUrl,
                    tokenSource
                );

                if (result?.Item1 == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[ERROR] Failed to fetch manifest. Please check if the version or parameters are valid.");
                    Console.ResetColor();
                    return 1;
                }

                var sophonAssets = result.Item1;
                long updateSize = result.Item2;

                int totalCount = sophonAssets.Count;
                int completedCount = 0;

                string totalSizeUnit = Formatter.FormatSize(updateSize);

                if (!AppConfig.Config.Silent)
                {
                    Console.WriteLine($"* Found {totalCount} assets");
                    Console.WriteLine($"* Total download size is {totalSizeUnit}");

                    Console.Write("Continue? (y/n): ");
                    var input = Console.ReadLine()?.Trim().ToLower();
                    if (input != "y" && input != "yes")
                    {
                        Console.WriteLine("Aborting...");
                        return 0;
                    }
                }

                long currentRead = 0;
                var stopwatch = Stopwatch.StartNew();
                bool completedSuccessfully = false;

                try
                {
                    foreach (var asset in sophonAssets)
                    {
                        string outputPath = Path.Combine(outputDir, asset.AssetName);

                        if (File.Exists(outputPath))
                        {
                            completedCount++;
                            continue;
                        }

                        await asset.WriteUpdateAsync(
                            httpClient,
                            outputDir,
                            outputDir,
                            outputDir,
                            false,
                            read =>
                            {
                                Interlocked.Add(ref currentRead, read);

                                double seconds = stopwatch.Elapsed.TotalSeconds;
                                if (seconds <= 0) seconds = 1;

                                string speedUnit = Formatter.FormatSize(currentRead / seconds);

                                if (!AppConfig.Config.Silent)
                                {
                                    string line = $"{_statusMessage} | {completedCount}/{totalCount} files ({speedUnit}/s)";
                                    int padding = Math.Max(0, _lastLineLength - line.Length);

                                    Console.Write(line + new string(' ', padding) + "\r");
                                    _lastLineLength = line.Length;
                                }
                            },
                            null, null,
                            tokenSource.Token
                        );

                        string outputTempPath = outputPath + "_tempUpdate";

                        if (File.Exists(outputTempPath))
                        {
                            File.Move(outputTempPath, outputPath, true);
                        }

                        completedCount++;
                    }

                    completedSuccessfully = true;
                }
                catch (OperationCanceledException)
                {
                    if (!AppConfig.Config.Silent)
                        Console.WriteLine("\nCancelled!");
                }
                finally
                {
                    stopwatch.Stop();

                    if (!AppConfig.Config.Silent)
                    {
                        Console.WriteLine();

                        if (completedSuccessfully)
                        {
                            Console.WriteLine("Download completed.");
                            Console.WriteLine($"Elapsed time: {stopwatch.Elapsed:hh\\:mm\\:ss}");
                        }
                    }
                }
            }

            if (_isRetry)
                goto StartDownload;

            return 0;
        }
    }
}
