using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using ModPackager.App;
using ModPackager.AssemblyLoad;
using ModPackager.JsonConverters;
using Newtonsoft.Json;

namespace ModPackager.Helpers
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
        private void ExecuteAndUnload(CustomAssemblyLoadContext2 alc, out WeakReference alcWeakRef)
        {
            //var a = alc.LoadAssemblyFromFileInfo(_assemblyFile);
            var a = alc.LoadFromAssemblyPath(_assemblyFile.FullName);
            alcWeakRef = new WeakReference(alc, trackResurrection: true);

            var modInfo = a.PopulateJsonDto(_args.VersioningStyle);
            var json = JsonConvert.SerializeObject(modInfo, Formatting.Indented);
            File.WriteAllTextAsync(_outputPath, json);

            alc.Unload();
        }


        public Task ResolveDependencies(CustomAssemblyLoadContext2 alc)
        {
            ExecuteAndUnload(alc, out var testAlcWeakRef);
            for (var i = 0; testAlcWeakRef.IsAlive && i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            return Task.CompletedTask;
        }
    }
}
