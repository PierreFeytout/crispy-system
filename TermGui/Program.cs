using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TermGui;

internal class Program
{

    public static async Task Main()
    {
        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddJsonFile("Settings.json");
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<MainApp>();
            services.AddSingleton<LeaderBoardApi>();
            services.AddHttpClient<LeaderBoardApi>((sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var url = config.GetValue<string>("ApiUrl");
                var key = config.GetValue<string>("ApiKey");
                client.BaseAddress = new Uri(url!);
                client.DefaultRequestHeaders.Add("x*-api-key", key);
            });
        });

        var host = builder.Build();

        var app = host.Services.GetRequiredService<MainApp>();

        app.Init();

        await host.StopAsync();
    }
}
