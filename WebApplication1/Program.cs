using System.Text;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;


namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            if (builder.Environment.IsDevelopment())
            {
                builder.Configuration.AddUserSecrets<Program>();
            }

            var accessKey = builder.Configuration.GetValue<string>("TableKey");


            // Add services to the container.
            builder.Services.AddAuthorization();
            builder.Services.AddScoped(sp =>
            {
                return new TableServiceClient(
                     new Uri("https://spaceinvader.table.core.windows.net/"),
                     new TableSharedKeyCredential("spaceinvader", accessKey));
            });

            builder.Services.AddScoped<ScoreService>();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            ConfigureRestEndpoints(app);
            // Serve a lightweight HTML page
            ConfigureLightWeightHtmlPage(app);
            app.UseStaticFiles();

            app.Run();
        }

        private static void ConfigureLightWeightHtmlPage(WebApplication app)
        {
            app.MapGet("/", async (HttpContext context, ScoreService scoreService) =>
            {
                var users = await scoreService.GetScores();
                var sb = new StringBuilder();

                foreach (var (user, i) in users.Select((value, i) => (value, i)))
                {
                    var iconOrNum = i == 0 ? "👑" : (i + 1).ToString();
                    sb.Append($@"
                    <li class=""list-group-item d-flex align-items-center"">
                        <span style=""width:2.5em; text-align:right;"" class=""mr-2"">{iconOrNum}</span>
                        <span class=""flex-grow-1 text-left"">{user.UserName}</span>
                        <span class=""font-weight-bold ml-2"">{user.Score}</span>
                    </li>");
                }

                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "LeaderboardTemplate.html");
                string htmlTemplate = await File.ReadAllTextAsync(templatePath);

                // Replace placeholder with leaderboard rows
                string htmlContent = htmlTemplate.Replace("{LEADERBOARD_ROWS}", sb.ToString());

                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(htmlContent);
            });
        }

        private static void ConfigureRestEndpoints(WebApplication app)
        {
            app.MapPost("/scores/", async (HttpContext httpContext, ScoreService scoreService, [FromBody] UserEntity payload) =>
            {
                var user = await scoreService.UpsertScore(payload);

                return user;
            });

            app.MapGet("/scores", (HttpContext httpContext, ScoreService scoreService) =>
            {
                return scoreService.GetScores();
            })
                .WithOpenApi()
                .WithName("GetScores");
        }
    }
}
