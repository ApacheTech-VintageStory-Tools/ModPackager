using ApacheTech.VintageMods.Common.ModInfoGenerator.Reflection;

AssemblyLoadContext.Default.Resolving += ModAssemblyLoadContext.OnAssemblyResolve;

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
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton<ModInfoGenerator>();
    })
    .UseSerilog()
    .Build();

var app = host.Services.GetRequiredService<ModInfoGenerator>();
await app.RunAsync(args);