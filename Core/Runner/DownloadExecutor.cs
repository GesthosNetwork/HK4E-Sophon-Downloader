using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Runner
{
    public static class DownloadExecutor
    {
        public static async Task RunDownload(string[] args)
        {
            string action = args[0];
            string gameId = args[1];
            string matchingField = args[2];
            string updateFrom = args[3];
            string updateTo = args.Length >= 6 ? args[4] : "";
            string outputDir = args[^1];

            if (!AppConfig.Config.Silent)
            {
                string encoded = "SEs0RSBTb3Bob24gRG93bmxvYWRlciBDb3B5cmlnaHQgKEMpIDIwMjUgR2VzdGhvc05ldHdvcms=";
                Console.WriteLine(Encoding.UTF8.GetString(Convert.FromBase64String(encoded)));
            }

            Enum.TryParse(AppConfig.Config.Region, out Region region);
            BranchType branch = Enum.Parse<BranchType>(AppConfig.Config.Branch, true);
            Game game = new(region, gameId);
            SophonUrl url = new(region, game.GetGameId(), branch, AppConfig.Config.LauncherId, AppConfig.Config.PlatApp);

            if (updateFrom.Count(c => c == '.') == 1) updateFrom += ".0";
            if (!AppConfig.Config.Silent)
                Console.WriteLine("[INFO] Initializing region, branch, and game info...");

            await url.GetBuildData();
            string prevManifest = url.GetBuildUrl(updateFrom, false);
            string newManifest = "";

            if (action == "update")
            {
                if (updateTo.Count(c => c == '.') == 1) updateTo += ".0";
                newManifest = url.GetBuildUrl(updateTo, true);
            }

            if (!AppConfig.Config.Silent)
            {
                Console.WriteLine(action == "update"
                    ? $"[INFO] update mode:\nprev = {prevManifest}\nnew = {newManifest}"
                    : $"[INFO] full mode: manifest = {prevManifest}");
            }

            await Downloader.StartDownload(prevManifest, newManifest, outputDir, matchingField);
        }
    }
}
