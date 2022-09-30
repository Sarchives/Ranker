using dotenv.net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

const string Cors = "_cors";

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: Cors,
        builder =>
        {
            builder
            .AllowAnyOrigin()
            .AllowAnyHeader();
        });
});

DotEnv.Load();

SQLitePCL.Batteries.Init();

string folder = "";

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Ranker");
else
    folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".ranker");

if (!Directory.Exists(folder))
{
    Directory.CreateDirectory(folder);
}

IRankerRepository database = new SQLiteRankerRepository(Path.Combine(folder, "Ranker.db"));

builder.Services.AddSingleton(database);

builder.Services.AddHostedService<BotService>();

builder.Services.AddControllers();

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(Cors);

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

await app.RunAsync("0.0.0.0");