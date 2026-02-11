using Microsoft.Extensions.Logging;
using WatsonWebserver.Core;
using WatsonWebserver.Lite;
using HttpMethod = WatsonWebserver.Core.HttpMethod;

namespace RefleCDN;

static class Network {
    public const String BASE_DIR = "/";

    private static WebserverLite server;
    private static WebserverLite serverSsl;

    internal static String FileDir;
    
    public static void Initialize() {
        Log.Network.LogDebug("Initializing network...");

        int port = Configuration.GetInt("Settings", "Port");
        FileDir = Configuration.Get("Settings", "FilesDirectory");

        server = new WebserverLite(new WebserverSettings("0.0.0.0", port), Routes.HandleFileAccess);
        server.Events.Logger += Logger;
        server.Settings.Debug.Responses = true;
        server.Settings.Debug.Requests = true;
        server.Settings.Debug.Routing = true;

        server.Routes.PreAuthentication.Static.Add(HttpMethod.GET, BASE_DIR, Routes.ShowServer, Routes.DefaultErrorRoute);
        server.Routes.Exception = Routes.DefaultErrorRoute;

        Log.Network.LogInformation("Starting webserver on {H}:{P}", server.Settings.Hostname, server.Settings.Port);
        server.Start();

        if (Configuration.GetBool("Settings", "SSLEnable")) {
            int portSsl = Configuration.GetInt("Settings", "PortSSL");
            serverSsl = new WebserverLite(new WebserverSettings("0.0.0.0", portSsl, true) {
                Ssl = new WebserverSettings.SslSettings() {
                    Enable = true,
                    AcceptInvalidAcertificates = true,
                    PfxCertificateFile = Configuration.Get("Settings", "SSLFileName"),
                    PfxCertificatePassword = Configuration.Get("Settings", "SSLPassword")
                }
            }, Routes.HandleFileAccess);

            serverSsl.Events.Logger += Logger;
            server.Settings.Debug.Responses = true;
            server.Settings.Debug.Requests = true;
            server.Settings.Debug.Routing = true;

            serverSsl.Routes.PreAuthentication.Static.Add(HttpMethod.GET, BASE_DIR, Routes.ShowServer, Routes.DefaultErrorRoute);
            serverSsl.Routes.Exception = Routes.DefaultErrorRoute;

            Log.Network.LogInformation("Starting webserver on {H}:{P}", serverSsl.Settings.Hostname, serverSsl.Settings.Port);
            serverSsl.Start();
        }

        Log.Network.LogDebug("Started.");
    }

    private static void Logger(string obj) {
        Log.Network.LogInformation(obj);
    }

    public static void Stop() {
        Log.Network.LogDebug("Stopping webserver");
        server.Stop();
        serverSsl?.Stop();
    }
}

static class Routes {
    internal static async Task DefaultNotFoundRoute(HttpContextBase ctx) {
        ctx.Response.StatusCode = 404;
        await ctx.Response.Send("RefleCDN - not found");
    }

    internal static async Task DefaultErrorRoute(HttpContextBase ctx, Exception e) {
        ctx.Response.StatusCode = 500;
        Log.Network.LogError("Failed to handle request to {q} from {s}: {e}", ctx.Request.Url.RawWithQuery, ctx.Request.Source, e);
        Log.Network.LogInformation("Request content: {c}", ctx.Request.DataAsString);
        await ctx.Response.Send(e.Message);
    }

    internal static async Task ShowServer(HttpContextBase ctx) {
        ctx.Response.StatusCode = 200;
        await ctx.Response.Send("RefleCDN");
    }
    
    internal static async Task HandleFileAccess(HttpContextBase ctx) {
        Stream file = null;
        try {
            string path = ctx.Request.Url.RawWithoutQuery;
            Log.Main.LogInformation("Path access: " + path);

            if (!path.StartsWith("/" + Network.FileDir)) {
                Log.Main.LogWarning("Path does not start with the configured directory: " + path);
                await DefaultNotFoundRoute(ctx);
                return;
            }

            string localFile = path.Substring(1);

            if (!File.Exists(localFile)) {
                Log.Main.LogWarning("File does not exist: " + path);
                await DefaultNotFoundRoute(ctx);
                return;
            }

            long length = new FileInfo(localFile).Length;
            file = File.OpenRead(localFile);

            Log.Network.LogDebug("Sending " + length + " bytes");
            await ctx.Response.Send(length, file);
            Log.Network.LogDebug("OK");
        } catch (Exception ex) {
            await DefaultErrorRoute(ctx, ex);
        } finally {
            file?.Close();
        }
    }
}