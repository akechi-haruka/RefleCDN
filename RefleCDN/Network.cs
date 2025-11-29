using Microsoft.Extensions.Logging;
using WatsonWebserver.Core;
using WatsonWebserver.Lite;
using HttpMethod = WatsonWebserver.Core.HttpMethod;

namespace RefleCDN;

static class Network {
    public const String BASE_DIR = "/";

    private static WebserverLite server;
    private static WebserverLite serverSsl;

    public static void Initialize() {
        Log.Network.LogDebug("Initializing network...");

        int port = Configuration.GetInt("Settings", "Port");
        string fileDir = Configuration.Get("Settings", "FilesDirectory");

        server = new WebserverLite(new WebserverSettings("0.0.0.0", port), Routes.DefaultNotFoundRoute);
        server.Events.Logger += Logger;
        server.Settings.Debug.Responses = true;

        server.Routes.PreAuthentication.Static.Add(HttpMethod.GET, BASE_DIR, Routes.ShowServer, Routes.DefaultErrorRoute);
        server.Routes.PreAuthentication.Content.Add(BASE_DIR + fileDir + "/", true);

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
            }, Routes.DefaultNotFoundRoute);

            serverSsl.Events.Logger += Logger;
            serverSsl.Settings.Debug.Responses = true;

            serverSsl.Routes.PreAuthentication.Static.Add(HttpMethod.GET, BASE_DIR, Routes.ShowServer, Routes.DefaultErrorRoute);
            serverSsl.Routes.PreAuthentication.Content.Add(BASE_DIR + fileDir + "/", true);

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
}