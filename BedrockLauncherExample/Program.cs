using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.Native;
using BedrockLauncher.Core.Network;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using BedrockLauncher.Core.FrameworkComplete;

namespace BedrockLauncherExample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                BedrockCore core = new BedrockCore();
                core.Init();
                InstallCallback callback = new InstallCallback()
                {
                    registerProcess_percent = ((s, u) =>
                    {
                        Console.WriteLine(s+u);
                    }),
                    result_callback = ((status, exception) =>
                    {
                        Console.WriteLine(exception);
                    })
                };
                var changeVersion = core.ChangeVersion("E:\\source\\BedrockLauncher.Core\\BedrockLauncherExample\\bin\\Debug\\net8.0-windows10.0.19041.0\\Version\\1.21.9401",callback);
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}
