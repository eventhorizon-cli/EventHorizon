using System.Text.Json.Serialization;
using EventHorizon.Configuration;
using EventHorizon.Engine;
using EventHorizon.Pricing;
using EventHorizon.Prompting;
using EventHorizon.Providers;
using EventHorizon.Workspace;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace EventHorizon;

public static class Program
{
    public static void Main(string[] args)
    {
        using var host = BuildHost(args, new PathEnvironment());
        host.Run();
    }

    internal static IHost BuildHost(string[] args, IPathEnvironment pathEnvironment)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddEventHorizonFiles(pathEnvironment);
        ConfigureLogging(builder, pathEnvironment);
        ConfigureServices(builder.Services, builder.Configuration, pathEnvironment);

        var app = builder.Build();
        ConfigureMiddleware(app);
        return app;
    }

    private static void ConfigureLogging(WebApplicationBuilder builder, IPathEnvironment pathEnvironment)
    {
        var logFilePath = Path.Combine(pathEnvironment.HomeDirectory, ".config", "eventhorizon", "error.log");
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Error()
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                restrictedToMinimumLevel: LogEventLevel.Error);

        var logger = loggerConfiguration.CreateLogger();

        Log.Logger = logger;
        builder.Logging.AddSerilog(logger, dispose: true);
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration,
        IPathEnvironment pathEnvironment)
    {
        services.AddHttpClient();
        services.AddControllers().AddJsonOptions(jsonOptions =>
        {
            jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        services
            .AddEventHorizonConfiguration(pathEnvironment)
            .AddEventHorizonWorkspace()
            .AddEventHorizonPrompting()
            .AddEventHorizonProviders()
            .AddEventHorizonPricing()
            .AddEventHorizonEngine();
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        var fileProvider = GetStaticFileProvider();

        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider, ContentTypeProvider = new FileExtensionContentTypeProvider(),
        });

        app.MapControllers();
        app.MapFallback(async context =>
        {
            var requestPath = NormalizePath(context.Request.Path.Value);
            if (requestPath.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var indexFile = fileProvider.GetFileInfo("index.html");
            if (!indexFile.Exists)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await using var stream = indexFile.CreateReadStream();
            await stream.CopyToAsync(context.Response.Body, context.RequestAborted).ConfigureAwait(false);
        });
    }

    private static IFileProvider GetStaticFileProvider()
    {
        var webRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        IFileProvider fileProvider = new ManifestEmbeddedFileProvider(typeof(Program).Assembly, "wwwroot");
        if (Directory.Exists(webRootPath))
        {
            fileProvider = new CompositeFileProvider(new PhysicalFileProvider(webRootPath), fileProvider);
        }

        return fileProvider;
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "/")
        {
            return "/";
        }

        var normalized = path.Trim();
        if (!normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = "/" + normalized;
        }

        return normalized.TrimEnd('/');
    }
}
