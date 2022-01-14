using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModPackager.App;
using Serilog;

var builder = new ConfigurationBuilder()
    .SetBasePath(System.Environment.CurrentDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .ReadFrom.Configuration(builder.Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IPackagerApplication, PackagerApplication>();
    })
    .UseSerilog()
    .Build();

var app = host.Services.GetRequiredService<IPackagerApplication>();
await app.RunAsync(args);