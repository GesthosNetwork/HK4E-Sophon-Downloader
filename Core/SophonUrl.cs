using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Core
{
    public class BranchesRoot
    {
        public int retcode { get; set; }
        public string? message { get; set; }
        public BranchesData? data { get; set; }
    }

    public class BranchesData
    {
        public List<BranchesGameBranch>? game_branches { get; set; }
    }

    public class BranchesGameBranch
    {
        public BranchesGame? game { get; set; }
        public BranchesMain? main { get; set; }
        public BranchesMain? pre_download { get; set; }
    }

    public class BranchesGame
    {
        public string? id { get; set; }
        public string? biz { get; set; }
    }

    public class BranchesMain
    {
        public string? package_id { get; set; }
        public string? branch { get; set; }
        public string? password { get; set; }
        public string? tag { get; set; }
        public List<string>? diff_tags { get; set; }
        public List<BranchesCategory>? categories { get; set; }
    }

    public class BranchesCategory
    {
        public string? category_id { get; set; }
        public string? matching_field { get; set; }
    }

    public enum Region { OSREL, CNREL }
    public enum BranchType { Main, PreDownload }

    public class SophonUrl
    {
        private string apiBase = "", sophonBase = "", gameId = "", launcherId = "", platApp = "";
        private string gameBiz = "", packageId = "", password = "";
        private BranchType branch;
        private BranchesRoot branchBackup = new();

        public SophonUrl(Region region, string gameId, BranchType branch = BranchType.Main, string launcherIdOverride = "", string platAppOverride = "")
        {
            UpdateRegion(region);
            this.gameId = gameId;
            this.branch = branch;
            launcherId = string.IsNullOrEmpty(launcherIdOverride) ? AppConfig.Config.LauncherId : launcherIdOverride;
            platApp = string.IsNullOrEmpty(platAppOverride) ? AppConfig.Config.PlatApp : platAppOverride;
        }

        public void UpdateRegion(Region region) =>
            (apiBase, sophonBase) = region switch
            {
                Region.OSREL => (
                    "https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGameBranches",
                    "https://sg-public-api.hoyoverse.com:443/downloader/sophon_chunk/api/getBuild"
                ),
                Region.CNREL => (
                    "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameBranches",
                    "https://api-takumi.mihoyo.com/downloader/sophon_chunk/api/getBuild"
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(region))
            };

        public async Task<int> GetBuildData()
        {
            var uri = new UriBuilder(apiBase);
            var q = HttpUtility.ParseQueryString(uri.Query);

            q["game_ids[]"] = gameId;
            q["launcher_id"] = launcherId;
            uri.Query = q.ToString();

            var obj = JsonSerializer.Deserialize<BranchesRoot>(await FetchUrl(uri.ToString()));
            var (ok, biz, pkg, pass, err) = ParseBuildData(obj, branch);

            if (!ok)
            {
                if (branch == BranchType.PreDownload)
                    (packageId, password) = ("ScSYQBFhu9", "ZOJpUiKu4Sme");
                else if (branch == BranchType.Main)
                    (packageId, password) = ("ScSYQBFhu9", "bDL4JUHL625x");
                else
                {
                    Console.WriteLine($"Error: {err}");
                    return -1;
                }

                branchBackup = new();
                return 0;
            }

            gameBiz = biz;
            packageId = string.IsNullOrEmpty(pkg) ? "ScSYQBFhu9" : pkg;
            password = string.IsNullOrEmpty(pass)
                ? (branch == BranchType.PreDownload ? "ZOJpUiKu4Sme" : "bDL4JUHL625x")
                : pass;

            branchBackup = obj!;
            return 0;
        }

        private static (bool ok, string biz, string pkg, string pass, string err)
            ParseBuildData(BranchesRoot? obj, BranchType type)
        {
            if (obj?.retcode != 0 || obj.message != "OK")
                return (false, "", "", "", obj?.message ?? "Unknown error");

            var branch = GetBranch(obj, type);
            if (branch == null)
                return (false, "", "", "", $"Branch {type} not found");

            var game = obj.data?.game_branches?.FirstOrDefault()?.game;
            return (true, game?.biz ?? "", branch.package_id ?? "", branch.password ?? "", "");
        }

        public string GetBuildUrl(string version, bool isUpdate = false)
        {
            var uri = new UriBuilder(sophonBase);
            var q = HttpUtility.ParseQueryString(uri.Query);

            q["branch"] = branch.ToString().ToLower();
            q["package_id"] = packageId;
            q["password"] = password;
            q["plat_app"] = platApp;

            if (branch != BranchType.PreDownload)
                q["tag"] = version;

            uri.Query = q.ToString();
            return uri.ToString();
        }

        private static async Task<string> FetchUrl(string url)
        {
            using var client = new HttpClient();
            return await client.GetStringAsync(url);
        }

        private static BranchesMain? GetBranch(BranchesRoot obj, BranchType type)
        {
            var b = obj.data?.game_branches?.FirstOrDefault();
            return type == BranchType.Main ? b?.main : b?.pre_download;
        }
    }
}
