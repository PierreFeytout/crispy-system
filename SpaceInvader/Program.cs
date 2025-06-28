using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace SpaceInvader;

public static class EmbeddedConfigExtensions
{
    public static IConfigurationBuilder AddEmbeddedJson(this IConfigurationBuilder config, string resourceName, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(
            assembly.GetManifestResourceNames().First(n => n.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase))
        ) ?? throw new Exception($"Resource {resourceName} not found.");
        var json = new StreamReader(stream).ReadToEnd();
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, json);
        config.AddJsonFile(tempFile, optional: false, reloadOnChange: false);
        return config;
    }
}

internal class Program
{
    public static async Task Main()
    {
        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddEmbeddedJson(
                "settings.json",
                assembly: Assembly.GetExecutingAssembly()
            );
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<MainApp>();
            services.AddSingleton<LeaderBoardApi>();
            services.AddHttpClient<LeaderBoardApi>((sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var url = config.GetValue<string>("ApiUrl");
#if DEBUG
                var dum = "nL2mU9jx6_-r_2Zy0wQaC9Fz2uAxPyUKbRt3Wz0B7QrOgxNRzINXIsQEGTKaI3pa7GLQ4N34hBrQAnrfUJ_mfQ";
#else
                var timer = config.GetValue<string>("RandomTimer");
                var dum = ApiKeyLoader.LoadDecryptedApiKey(timer);
#endif
                client.BaseAddress = new Uri(url!);
                client.DefaultRequestHeaders.Add("dummy", dum);
            }).ConfigurePrimaryHttpMessageHandler(_ => SocketHandlerBuilder.Build());
        });

        var host = builder.Build();

        var app = host.Services.GetRequiredService<MainApp>();

        app.Init();

        await host.StopAsync();
    }
}
