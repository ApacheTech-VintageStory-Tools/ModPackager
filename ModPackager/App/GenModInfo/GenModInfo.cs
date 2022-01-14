using System;
using System.IO;
using Newtonsoft.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace ModPackager.App.GenModInfo
{
    internal class GenModInfo
    {
        public static string Version { get; set; } = "0.1.0";

        public static string Configuration { get; set; } = "";

        private readonly CommandLineArgs _args;

        private readonly FileInfo _assemblyFile;

        private readonly string _outputPath;

        public GenModInfo(CommandLineArgs args, string tempAssemblyPath, string outputPath)
        {
            _args = args;
            _outputPath = outputPath;
            _assemblyFile = new FileInfo(tempAssemblyPath);
            if (!_assemblyFile.Exists)
                throw new FileNotFoundException("No file was found at the given location", _assemblyFile.FullName);

            if (!_assemblyFile.Extension.Equals(".dll"))
                throw new DllNotFoundException("The selected file is not a .dll file.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ExecuteAndUnload(string assemblyPath, out WeakReference alcWeakRef)
        {
            var alc = new TestAssemblyLoadContext();
            Assembly a = alc.LoadFromAssemblyPath(assemblyPath);
            alcWeakRef = new WeakReference(alc, trackResurrection: true);

            var modInfo = a.PopulateJsonDto(_args.VersioningStyle);
            var json = JsonConvert.SerializeObject(modInfo, Formatting.Indented);
            File.WriteAllTextAsync(_outputPath, json);

            alc.Unload();
        }


        public Task DoStuff()
        {
            ExecuteAndUnload(_assemblyFile.FullName, out var testAlcWeakRef);
            for (var i = 0; testAlcWeakRef.IsAlive && (i < 10); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            return Task.CompletedTask;
        }
    }

    internal class TestAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver? _resolver;

        public TestAssemblyLoadContext(string mainAssemblyToLoadPath) : base(true)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath);
        }

        public TestAssemblyLoadContext() : base(true)
        {
        }

        protected override Assembly? Load(AssemblyName name)
        {
            var assemblyPath = _resolver?.ResolveAssemblyToPath(name);
            return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
        }
    }
}
