using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RefleCDN;

static class Log {

    public static ILogger Main { get; private set; }
    public static ILogger Network { get; private set; }

    public static void Initialize() {

        IConfigurationSection loggingConfig = Configuration.Current.GetSection("Logging");

        ILoggerFactory factory = LoggerFactory.Create(builder => builder
            .AddConfiguration(loggingConfig)
            .AddSimpleConsole(options =>
            {
                options.SingleLine = true;
            })
            .AddDebug()
        );
        Main = factory.CreateLogger("Main");
        Network = factory.CreateLogger("Net ");

        Main.LogInformation("Logging started.");
    }

}