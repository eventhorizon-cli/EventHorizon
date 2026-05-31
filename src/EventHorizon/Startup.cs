using EventHorizon.Configuration;
using EventHorizon.Engine;
using EventHorizon.Pricing;
using EventHorizon.Prompting;
using EventHorizon.Providers;
using EventHorizon.Workspace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace EventHorizon;

internal static class Startup
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var pathEnvironment = ResolvePathEnvironment(configuration);
        services
            .AddEventHorizonConfiguration(pathEnvironment)
            .AddEventHorizonWorkspace()
            .AddEventHorizonPrompting()
            .AddEventHorizonProviders()
            .AddEventHorizonPricing()
            .AddEventHorizonEngine();
    }

    public static void Configure(WebApplication app)
    {
        var fileProvider = GetStaticFileProvider();
        MapStaticFiles(app, fileProvider);
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

    private static void MapStaticFiles(WebApplication app, IFileProvider fileProvider)
    {
        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = fileProvider,
        });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            ContentTypeProvider = new FileExtensionContentTypeProvider(),
        });
    }

    private static IFileProvider GetStaticFileProvider()
    {
        var webRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        IFileProvider fileProvider = new ManifestEmbeddedFileProvider(typeof(Startup).Assembly, "wwwroot");
        if (Directory.Exists(webRootPath))
        {
            fileProvider = new CompositeFileProvider(new PhysicalFileProvider(webRootPath), fileProvider);
        }

        return fileProvider;
    }

    private static IPathEnvironment ResolvePathEnvironment(IConfiguration configuration)
        => configuration.GetSection(nameof(PathEnvironment)).Get<ConfiguredPathEnvironment>()?.ToPathEnvironment()
           ?? new PathEnvironment();

    private sealed class ConfiguredPathEnvironment
    {
        public string? CurrentDirectory { get; set; }

        public string? HomeDirectory { get; set; }

        public IPathEnvironment ToPathEnvironment()
            => new ConfiguredPathEnvironmentValue(
                string.IsNullOrWhiteSpace(CurrentDirectory) ? Environment.CurrentDirectory : CurrentDirectory,
                string.IsNullOrWhiteSpace(HomeDirectory)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    : HomeDirectory);
    }

    private sealed class ConfiguredPathEnvironmentValue : IPathEnvironment
    {
        public ConfiguredPathEnvironmentValue(string currentDirectory, string homeDirectory)
        {
            CurrentDirectory = currentDirectory;
            HomeDirectory = homeDirectory;
        }

        public string CurrentDirectory { get; }

        public string HomeDirectory { get; }
    }

    internal static string NormalizePath(string? path)
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
