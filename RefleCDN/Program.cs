using System.Reflection;
using Microsoft.Extensions.Logging;

namespace RefleCDN;

static class Program {
    public const string FILES_DIR = "Files";
    
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

        if (!Directory.Exists(FILES_DIR)) {
            Log.Main.LogDebug("Directory not found: {d}", FILES_DIR);
            Directory.CreateDirectory(FILES_DIR);
            Log.Main.LogInformation("Created directory: {d}", FILES_DIR);
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
        Log.Main.LogInformation("Place files in: {d}", FILES_DIR);
        Log.Main.LogInformation("Press any key to exit.");

        Console.ReadKey();
        
        Log.Main.LogInformation("Exiting");
        
        Network.Stop();
    }
}