using System.Threading.Tasks;

namespace Core
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await Runner.AppRunner.Run(args);
        }
    }
}
