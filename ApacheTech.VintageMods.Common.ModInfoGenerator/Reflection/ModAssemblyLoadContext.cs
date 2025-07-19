namespace ApacheTech.VintageMods.Common.ModInfoGenerator.Reflection;

internal class ModAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver? _resolver;
    private static readonly object ThreadLock = new();

    public ModAssemblyLoadContext(string mainAssemblyToLoadPath) : base(true)
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath);
    }

    protected override Assembly? Load(AssemblyName name)
    {
        var assemblyPath = _resolver?.ResolveAssemblyToPath(name);
        return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
    }

    internal static Assembly? OnAssemblyResolve(AssemblyLoadContext context, AssemblyName assembly)
    {
        lock (ThreadLock)
        {
            var baseDir = Environment.GetEnvironmentVariable("VINTAGE_STORY");
            if (baseDir is null) throw new InvalidOperationException("%VINTAGE_STORY% not set");
            var directories = new List<string>
            {
                baseDir,
                Path.Combine(baseDir, "Mods"),
                Path.Combine(baseDir, "Lib"),
                Path.Combine(baseDir, "Lib64"),
                Path.Combine(baseDir, "Lib32")
            };

            return directories
                .Select(directory => Path.Combine(directory, $"{assembly.Name}.dll"))
                .Where(File.Exists)
                .Select(context.LoadFromAssemblyPath)
                .FirstOrDefault();
        }
    }
}