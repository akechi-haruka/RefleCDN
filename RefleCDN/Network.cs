using Microsoft.Extensions.Logging;
using WatsonWebserver.Core;
using WatsonWebserver.Lite;
using HttpMethod = WatsonWebserver.Core.HttpMethod;

namespace RefleCDN;

static class Network {

    public const int LOCAL_PORT = 80;
    public const String BASE_DIR = "/";

    private static WebserverLite server;

    public static void Initialize() {

        Log.Network.LogDebug("Initializing network...");

        server = new WebserverLite(new WebserverSettings("0.0.0.0", LOCAL_PORT), Routes.DefaultNotFoundRoute);

        server.Routes.PreAuthentication.Static.Add(HttpMethod.GET, BASE_DIR, Routes.ShowServer, Routes.DefaultErrorRoute);
        server.Routes.PreAuthentication.Content.Add(BASE_DIR + Program.FILES_DIR + "/", true);
        server.Routes.PostRouting = Routes.PostRouting;

        Log.Network.LogInformation("Starting webserver on {H}:{P}", server.Settings.Hostname, server.Settings.Port);
        server.Start();
        Log.Network.LogDebug("Started.");
    }

    public static void Stop() {
        Log.Network.LogDebug("Stopping webserver");
        server.Stop();
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

    internal static async Task PostRouting(HttpContextBase ctx) {
        Log.Network.LogDebug("{Method} {Url}: {ResponseCode} {ResponseLength} {UserAgent}", ctx.Request.Method, ctx.Request.Url.RawWithQuery, ctx.Response.StatusCode, ctx.Response.ContentLength, ctx.Request.Useragent);
    }
    
}