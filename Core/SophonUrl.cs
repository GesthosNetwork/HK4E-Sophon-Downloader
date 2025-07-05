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
        public string message { get; set; }
        public BranchesData data { get; set; }
    }

    public class BranchesData
    {
        public List<BranchesGameBranch> game_branches { get; set; }
    }

    public class BranchesGameBranch
    {
        public BranchesGame game { get; set; }
        public BranchesMain main { get; set; }
        public BranchesMain pre_download { get; set; }
    }

    public class BranchesGame
    {
        public string id { get; set; }
        public string biz { get; set; }
    }

    public class BranchesMain
    {
        public string package_id { get; set; }
        public string branch { get; set; }
        public string password { get; set; }
        public string tag { get; set; }
        public List<string> diff_tags { get; set; }
        public List<BranchesCategory> categories { get; set; }
    }

    public class BranchesCategory
    {
        public string category_id { get; set; }
        public string matching_field { get; set; }
    }

    public enum Region
    {
        OSREL,
        CNREL
    }

    public enum BranchType
    {
        Main,
        PreDownload
    }

    public class SophonUrl
    {
        private string apiBase { get; set; }
        private string sophonBase { get; set; }
        private string gameId { get; set; }
        private BranchType branch { get; set; }
        private string launcherId { get; set; }
        private string platApp { get; set; }
        private string gameBiz { get; set; } = "";
        private string packageId { get; set; } = "";
        private string password { get; set; } = "";
        private BranchesRoot branchBackup { get; set; } = new BranchesRoot();

        public SophonUrl(Region region, string gameId, BranchType branch = BranchType.Main, string launcherIdOverride = "", string platAppOverride = "")
        {
            UpdateRegion(region);
            this.gameId = gameId;
            this.branch = branch;
            this.launcherId = !string.IsNullOrEmpty(launcherIdOverride) ? launcherIdOverride : AppConfig.Config.LauncherId;
            this.platApp = !string.IsNullOrEmpty(platAppOverride) ? platAppOverride : AppConfig.Config.PlatApp;
        }

        public void UpdateRegion(Region region)
        {
            switch (region)
            {
                case Region.OSREL:
                    apiBase = "https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGameBranches";
                    sophonBase = "https://sg-public-api.hoyoverse.com/downloader/sophon_chunk/api/getBuild";
                    break;
                case Region.CNREL:
                    apiBase = "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameBranches";
                    sophonBase = "https://api-takumi.mihoyo.com/downloader/sophon_chunk/api/getBuild";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(region), region, null);
            }
        }

        public async Task<int> GetBuildData()
        {
            var uri = new UriBuilder(apiBase);
            var query = HttpUtility.ParseQueryString(uri.Query);

            query["game_ids[]"] = gameId;
            query["launcher_id"] = launcherId;
            uri.Query = query.ToString();

            string json = await FetchUrl(uri.ToString());
            var obj = JsonSerializer.Deserialize<BranchesRoot>(json);

            string[] data = ParseBuildData(obj, branch);
            if (data[0] != "OK")
            {
                Console.WriteLine($"Error: {data[1]}");
                return -1;
            }

            gameBiz = data[1];
            packageId = data[2];
            password = data[3];
            branchBackup = obj;

            return 0;
        }

        private string[] ParseBuildData(BranchesRoot obj, BranchType searchBranch)
        {
            if (obj == null || obj.retcode != 0 || obj.message != "OK")
                return new[] { "ERROR", obj?.message ?? "Unknown error" };

            var branchObj = GetBranch(obj, searchBranch);
            if (branchObj == null)
                return new[] { "ERROR", $"Branch {searchBranch} not found" };

            var gameObj = GetBranchGame(obj);
            return new[] { "OK", gameObj.biz, branchObj.package_id, branchObj.password };
        }

        public string GetBuildUrl(string version, bool isUpdate = false)
        {
            var uri = new UriBuilder(sophonBase);
            var query = HttpUtility.ParseQueryString(uri.Query);

            query["branch"] = branch.ToString().ToLower();
            query["package_id"] = packageId;
            query["password"] = password;
            query["plat_app"] = platApp;
            query["tag"] = version;

            uri.Query = query.ToString();
            return uri.ToString();
        }

        private static async Task<string> FetchUrl(string url)
        {
            using var client = new HttpClient();
            return await client.GetStringAsync(url);
        }

        private static BranchesGame GetBranchGame(BranchesRoot obj)
        {
            return obj.data?.game_branches?.FirstOrDefault()?.game;
        }

        private static BranchesMain GetBranch(BranchesRoot obj, BranchType searchBranch)
        {
            var branchObj = obj.data?.game_branches?.FirstOrDefault();
            return searchBranch switch
            {
                BranchType.Main => branchObj?.main,
                BranchType.PreDownload => branchObj?.pre_download,
                _ => null
            };
        }
    }
}
