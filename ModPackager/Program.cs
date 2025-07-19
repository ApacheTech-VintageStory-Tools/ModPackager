using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModPackager.App;
using Serilog;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

var threadLock = new object();
AssemblyLoadContext.Default.Resolving += OnAssemblyResolve;

Assembly? OnAssemblyResolve(AssemblyLoadContext context, AssemblyName assembly)
{
    lock (threadLock)
    {
        return new DirectoryInfo(Environment.CurrentDirectory)
            .GetFiles($"{assembly.Name}.dll|{assembly.Name}.exe", SearchOption.TopDirectoryOnly)
            .Select(file => context.LoadFromAssemblyPath(file.FullName))
            .FirstOrDefault();
    }
}

var builder = new ConfigurationBuilder()
    .SetBasePath(Environment.CurrentDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .ReadFrom.Configuration(builder.Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((builder, services) =>
    {
        services.AddSingleton<IPackagerApplication, PackagerApplication>();
    })
    .UseSerilog()
    .Build();

var app = host.Services.GetRequiredService<IPackagerApplication>();
await app.RunAsync(args);