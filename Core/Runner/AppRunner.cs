using System.Reflection;
using System;
using System.Threading.Tasks;

namespace Core.Runner
{
    public static class AppRunner
    {
        public static async Task<int> Run(string[] args)
        {
            Core.Utils.WindowUtils.CenterConsole();

            _ = AppConfig.Config;
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.Title = $"HK4E Sophon Downloader v{version?.Major}.{version?.Minor}";

            if (args.Length == 0)
                return await MenuUI.RunInteractiveMenu();

            return await CliHandler.RunWithArgs(args);
        }
    }
}
