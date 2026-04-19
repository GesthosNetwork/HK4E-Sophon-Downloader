using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Core.Utils;
using Sophon;

namespace Core
{
    internal class Downloader
    {
        private static string _statusMessage = "Downloading...";
        private static int _lastLineLength = 0;

        public static async Task<int> StartDownload(string prevManifestUrl, string newManifestUrl, string outputDir, string matchingField)
        {
            bool isFirstRun = true;

            using var httpCheckClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

            async Task<bool> HasInternet()
            {
                try
                {
                    using var res = await httpCheckClient.GetAsync("https://example.com");
                    return res.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }

            async Task WaitInternet(Action<string> write)
            {
                while (!await HasInternet())
                {
                    write("[ERROR] Waiting for internet connection...");
                    await Task.Delay(1000);
                }
            }

            while (true)
            {
                using var tokenSource = new CancellationTokenSource();
                using var httpHandler = new HttpClientHandler { MaxConnectionsPerServer = AppConfig.Config.MaxHttpHandle };
                using var httpClient = new HttpClient(httpHandler)
                {
                    DefaultRequestVersion = HttpVersion.Version30,
                    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
                };

                Directory.CreateDirectory(outputDir);

                if (!AppConfig.Config.Silent)
                    Console.WriteLine("Fetching assets...");

                var result = await Assets.GetAssetsFromManifests(httpClient, matchingField, prevManifestUrl, newManifestUrl, tokenSource);

                if (result?.Item1 == null)
                    return Error("Failed to fetch manifest.");

                var assets = result.Item1;
                int total = assets.Count, done = 0;
                long currentRead = 0;

                if (!AppConfig.Config.Silent && isFirstRun)
                {
                    Console.WriteLine($"* Found {total} assets");
                    Console.WriteLine($"* Total download size is {Formatter.FormatSize(result.Item2)}");
                    Console.Write("Continue? (yes/no): ");
                    var input = Console.ReadLine()?.Trim().ToLower();
                    if (input != "y" && input != "yes") return 0;
                    isFirstRun = false;
                }

                var sw = Stopwatch.StartNew();

                void WriteLineOverwrite(string text)
                {
                    if (AppConfig.Config.Silent) return;
                    int pad = Math.Max(0, _lastLineLength - text.Length);
                    Console.Write(text + new string(' ', pad) + "\r");
                    _lastLineLength = text.Length;
                }

                void Render()
                {
                    double sec = Math.Max(1, sw.Elapsed.TotalSeconds);
                    WriteLineOverwrite($"{_statusMessage} | {done}/{total} files ({Formatter.FormatSize(currentRead / sec)}/s)");
                }

                try
                {
                    foreach (var asset in assets)
                    {
                        string path = Path.Combine(outputDir, asset.AssetName);

                        if (File.Exists(path))
                        {
                            done++;
                            continue;
                        }

                        int retry = 0;

                    RETRY:
                        try
                        {
                            await asset.WriteUpdateAsync(
                                httpClient, outputDir, outputDir, outputDir,
                                false,
                                read =>
                                {
                                    Interlocked.Add(ref currentRead, read);
                                    Render();
                                },
                                null, null,
                                tokenSource.Token
                            );
                        }
                        catch (Exception ex) when (
                            ex is HttpRequestException ||
                            ex is TaskCanceledException ||
                            ex is IOException)
                        {
                            await WaitInternet(msg =>
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                WriteLineOverwrite(msg);
                                Console.ResetColor();
                            });

                            retry++;

                            if (!AppConfig.Config.Silent)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                WriteLineOverwrite($"[ERROR] Network issue, retrying... ({retry})");
                                Console.ResetColor();
                            }

                            string temp = path + "_tempUpdate";
                            try { if (File.Exists(temp)) File.Delete(temp); }
                            catch { await Task.Delay(1000); }

                            await Task.Delay(300);
                            goto RETRY;
                        }

                        string tempPath = path + "_tempUpdate";
                        if (File.Exists(tempPath))
                            File.Move(tempPath, path, true);

                        done++;
                        Render();
                    }

                    if (!AppConfig.Config.Silent)
                    {
                        WriteLineOverwrite($"{_statusMessage} | {total}/{total} files");
                        Console.WriteLine();
                        Console.WriteLine("Download completed.");
                        Console.WriteLine($"Elapsed time: {sw.Elapsed:hh\\:mm\\:ss}");

                        while (Console.KeyAvailable)
                            Console.ReadKey(true);

                        Console.ReadKey(true);
                    }

                    return 0;
                }
                catch (OperationCanceledException)
                {
                    return Error("Cancelled!");
                }
                catch (Exception ex)
                {
                    return Error(ex.Message);
                }
            }
        }

        private static int Error(string msg)
        {
            if (!AppConfig.Config.Silent)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {msg}");
                Console.ResetColor();
                Console.ReadKey(true);
            }
            return 1;
        }
    }
}
