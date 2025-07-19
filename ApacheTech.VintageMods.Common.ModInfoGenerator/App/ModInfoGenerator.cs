using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ApacheTech.VintageMods.Common.ModInfoGenerator.CommandLine;
using ApacheTech.VintageMods.Common.ModInfoGenerator.Converters;
using ApacheTech.VintageMods.Common.ModInfoGenerator.Reflection;
using CommandLine;

namespace ApacheTech.VintageMods.Common.ModInfoGenerator.App;

internal class ModInfoGenerator
{
    internal async Task RunAsync(params string[] args)
    {
        await Parser.Default
            .ParseArguments<CommandLineArguments>(args)
            .WithParsedAsync(Execute);
    }

    private static Task Execute(CommandLineArguments args)
    {
        ExecuteAndUnload(args, out var testAlcWeakRef);
        for (var i = 0; testAlcWeakRef.IsAlive && i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        return Task.CompletedTask;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ExecuteAndUnload(CommandLineArguments args, out WeakReference alcWeakRef)
    {
        var alc = new ModAssemblyLoadContext(args.AssemblyPath);
        var a = alc.LoadFromAssemblyPath(args.AssemblyPath);
        alcWeakRef = new WeakReference(alc, true);

        var modInfo = a.PopulateJsonDto(args.VersioningStyle);
        var json = JsonConvert.SerializeObject(modInfo, Formatting.Indented);

        using var writer = File.CreateText(Path.Combine(args.OutputDir, "modinfo.json"));
        writer.Write(json);
        alc.Unload();
    }
}