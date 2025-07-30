using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Core.Utils;
using Sophon;
using Sophon.Structs;

namespace Core
{
    internal class Downloader
    {
        private static string _cancelMessage = string.Empty;
        private static bool _isRetry = true;

        public static async Task<int> StartDownload(string prevManifestUrl, string newManifestUrl, string outputDir, string matchingField)
        {
        StartDownload:

            _isRetry = false;
            _cancelMessage = "[\"C\"] Stop or [\"R\"] Restart";

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

                long totalSizeDiff = sophonAssets.GetCalculatedDiffSize(true);
                string totalSizeDiffUnit = Formatter.FormatSize(totalSizeDiff);
                string totalSizeUnit = Formatter.FormatSize(updateSize);

                if (!AppConfig.Config.Silent)
                {
                    Console.WriteLine($"* Found {sophonAssets.Count} assets");
                    if (!string.IsNullOrEmpty(newManifestUrl))
                    {
                        Console.WriteLine($"* Update data is {totalSizeDiffUnit}");
                        Console.WriteLine($"* Because the full assets will be downloaded, total download size is {totalSizeUnit}");
                    }
                    else
                    {
                        Console.WriteLine($"* Total download size is {totalSizeUnit}");
                    }

                    Console.Write("Continue? (y/n): ");
                    var input = Console.ReadLine()?.Trim().ToLower();
                    if (input != "y" && input != "yes")
                    {
                        Console.WriteLine("Aborting...");
                        return 0;
                    }
                }

                long currentRead = 0;
                Task exitTask = Task.Run(() => AppExitTrigger(tokenSource));

                var stopwatch = Stopwatch.StartNew();

                try
                {
                    foreach (string tempFile in Directory.EnumerateFiles(outputDir, "*_tempUpdate", SearchOption.AllDirectories))
                        File.Delete(tempFile);

                    var downloadTaskQueue = new ActionBlock<Tuple<SophonAsset, HttpClient>>(async ctx =>
                    {
                        var asset = ctx.Item1;
                        var client = ctx.Item2;

                        await asset.WriteUpdateAsync(
                            client,
                            outputDir,
                            outputDir,
                            outputDir,
                            false,
                            read =>
                            {
                                Interlocked.Add(ref currentRead, read);
                                string sizeUnit = Formatter.FormatSize(currentRead);
                                string speedUnit = Formatter.FormatSize(currentRead / stopwatch.Elapsed.TotalSeconds);

                                if (!AppConfig.Config.Silent)
                                {
                                    Console.Write($"{_cancelMessage} | {sizeUnit}/{totalSizeUnit} ({totalSizeDiffUnit} diff) ({speedUnit}/s) \r");
                                }
                            },
                            null, null,
                            tokenSource.Token
                        );

                        string outputPath = Path.Combine(outputDir, asset.AssetName);
                        string outputTempPath = outputPath + "_tempUpdate";

                        File.Move(outputTempPath, outputPath, true);
                    },
                    new ExecutionDataflowBlockOptions
                    {
                        CancellationToken = tokenSource.Token,
                        MaxDegreeOfParallelism = AppConfig.Config.Threads,
                        MaxMessagesPerTask = AppConfig.Config.Threads
                    });

                    foreach (var asset in sophonAssets)
                        await downloadTaskQueue.SendAsync(Tuple.Create(asset, httpClient), tokenSource.Token);

                    downloadTaskQueue.Complete();
                    await downloadTaskQueue.Completion;
                }
                catch (OperationCanceledException)
                {
                    if (!AppConfig.Config.Silent)
                    {
                        Console.WriteLine("\nCancelled!");
                    }
                }
                finally
                {
                    stopwatch.Stop();
                    await exitTask;
                }
            }

            if (_isRetry)
                goto StartDownload;

            return 0;
        }

        private static void AppExitTrigger(CancellationTokenSource tokenSource)
        {
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.C:
                        _cancelMessage = "Canceling...";
                        tokenSource.Cancel();
                        return;
                    case ConsoleKey.R:
                        _isRetry = true;
                        tokenSource.Cancel();
                        return;
                }
            }
        }
    }
}
