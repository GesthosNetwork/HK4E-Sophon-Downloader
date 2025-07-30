using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Core.Runner
{
    public static class AppRunner
    {
        public static async Task<int> Run(string[] args)
        {
            Core.Utils.WindowUtils.CenterConsole();

            _ = AppConfig.Config;

            var version = Assembly
                .GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "unknown";

            Console.Title = $"HK4E Sophon Downloader v{version}";

            if (args.Length == 0)
                return await MenuUI.RunInteractiveMenu();

            return await CliHandler.RunWithArgs(args);
        }
    }
}
