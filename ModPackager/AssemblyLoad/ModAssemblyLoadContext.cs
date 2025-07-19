using System.Collections.Concurrent;
using System.IO;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using ModPackager.Extensions;
using Serilog;

namespace ModPackager.AssemblyLoad;

/// <summary>
///     Provides a custom assembly load context for mod packaging, enabling dynamic resolution and loading of assemblies
///     from multiple directories relevant to the Vintage Story modding environment. This context caches loaded assemblies
///     for efficiency and supports thread-safe management of search paths. It is designed to facilitate robust mod packaging
///     workflows by searching for both .dll and .exe assemblies, and can be extended for further custom resolution logic.
/// </summary>
public class ModAssemblyLoadContext : AssemblyLoadContext
{
    private readonly ConcurrentBag<string> _resolvePaths;
    private readonly ConcurrentDictionary<string, Assembly> _assemblyCache = new();

    /// <summary>
    ///     Initialises a new instance of the <see cref="ModAssemblyLoadContext"/> class, setting up default search paths
    ///     including the current directory, Vintage Story installation, Mods, and Lib folders.
    /// </summary>
    public ModAssemblyLoadContext() : base(true)
    {
        var vintageStoryPath = Environment.GetEnvironmentVariable("VINTAGE_STORY")!;
        _resolvePaths = new ConcurrentBag<string>(
        [
            Environment.CurrentDirectory,
            vintageStoryPath,
            Path.Combine(vintageStoryPath, "Mods"),
            Path.Combine(vintageStoryPath, "Lib")
        ]);
    }

    /// <summary>
    ///     Attempts to resolve and load an assembly by name, using cached results if available, otherwise searching all
    ///     configured paths for a matching .dll or .exe file. Logs an error if the assembly cannot be found.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly to resolve and load.</param>
    /// <returns>
    ///     The loaded <see cref="Assembly"/> if found; otherwise, <c>null</c>.
    /// </returns>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name!;
        if (_assemblyCache.TryGetValue(name, out var cached))
            return cached;
        var loaded = TryLoad(assemblyName);
        if (loaded != null)
        {
            _assemblyCache[name] = loaded;
            return loaded;
        }
        Log.Error("[ModAssemblyLoadContext] Failed to load assembly: {AssemblyName}", assemblyName);
        return null;
    }

    /// <summary>
    ///     Attempts to load an assembly by searching all resolve paths for .dll or .exe files matching the assembly name.
    ///     Uses LINQ to generate all possible file paths and loads the first one found. This method is used internally by
    ///     the load context to resolve assemblies dynamically.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly to load.</param>
    /// <returns>
    ///     The loaded <see cref="Assembly"/> if found; otherwise, <c>null</c>.
    /// </returns>
    private Assembly? TryLoad(AssemblyName assemblyName)
    {
        string[] extensions = [".dll", ".exe"];
        var filePath = _resolvePaths
            .SelectMany(path => extensions.Select(ext => Path.Combine(path, assemblyName.Name! + ext)))
            .FirstOrDefault(File.Exists);

        return filePath is not null 
            ? LoadFromAssemblyPath(filePath) 
            : null;
    }

    /// <summary>
    ///     Loads an assembly from a specified file, adding its directory to the resolve paths if necessary. This method is
    ///     useful for loading assemblies that are not in the default search locations.
    /// </summary>
    /// <param name="assemblyFile">The file information for the assembly to load.</param>
    /// <returns>
    ///     The loaded <see cref="Assembly"/> if found; otherwise, <c>null</c>.
    /// </returns>
    public Assembly? LoadAssemblyFromFileInfo(FileInfo assemblyFile)
    {
        var dir = Path.GetDirectoryName(assemblyFile.FullName)!;
        if (!_resolvePaths.Contains(dir)) _resolvePaths.Add(dir);
        var assemblyName = new AssemblyName(assemblyFile.NameWithoutExtension());
        return Load(assemblyName);
    }
}