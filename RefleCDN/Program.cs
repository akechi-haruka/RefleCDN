using System.Reflection;
using Microsoft.Extensions.Logging;

namespace RefleCDN;

static class Program {
    
    public static readonly String NAME;
    
    static Program() {

        string gitHash = Assembly.Load(typeof(Program).Assembly.FullName)
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attr => attr.Key == "GitHash")?.Value;

        AssemblyName assemblyInfo = Assembly.GetExecutingAssembly().GetName();
        NAME = assemblyInfo.Name + "/" + assemblyInfo.Version + "-" + gitHash + " - Akechi Haruka";
    }
    
    static void Main(string[] args) {
        try {
            Configuration.Initialize();
        } catch (Exception ex) {
            Console.WriteLine("An error occurred during loading the configuration:\n" + ex.Message);
#if DEBUG
            Console.WriteLine(ex);
#endif
            Console.ReadLine();
            return;
        }
        
        Log.Initialize();

        string fileDir = Configuration.Get("Settings", "FilesDirectory");

        if (!Directory.Exists(fileDir)) {
            Log.Main.LogDebug("Directory not found: {d}", fileDir);
            Directory.CreateDirectory(fileDir);
            Log.Main.LogInformation("Created directory: {d}", fileDir);
        }

        try {
            Network.Initialize();
        } catch (Exception ex) {
            Log.Main.LogCritical("An error occurred while starting the webserver:\n{ex}", ex.Message);
#if DEBUG
            Log.Main.LogCritical(ex.ToString());
#endif
            Console.ReadLine();
            return;
        }

        Log.Main.LogInformation(NAME);
        Log.Main.LogInformation("Initialization complete.");
        Log.Main.LogInformation("Place files in: {d}", fileDir);
        Log.Main.LogInformation("Press any key to exit.");

        if (Configuration.GetBool("Settings", "PressKeyToExit")) {
            Console.ReadKey();

            Log.Main.LogInformation("Exiting");

            Network.Stop();
        } else {
            Thread.Sleep(Int32.MaxValue);
        }
    }
}