using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using ModPackager.Extensions;

namespace ModPackager.AssemblyLoad
{
    internal class CustomAssemblyLoadContext : AssemblyLoadContext
    {
        public CustomAssemblyLoadContext() : base(true)
        {
        }
    }

    internal class CustomAssemblyLoadContext2 : AssemblyLoadContext
    {
        private readonly List<string> _resolvePaths;

        public CustomAssemblyLoadContext2() : base(true)
        {
            _resolvePaths = new List<string>
            {
                Environment.CurrentDirectory,
                Environment.GetEnvironmentVariable("VINTAGE_STORY")!
            };
        }

        private Assembly? TryLoad(AssemblyName assemblyName)
        {
            foreach (var assemblyPath in _resolvePaths.Select(path => Path.Combine(path, assemblyName.Name!)))
            {
                if (File.Exists(assemblyPath + ".dll"))
                {
                    return LoadFromAssemblyPath(assemblyPath + ".dll");
                }
                if (File.Exists(assemblyPath + ".exe"))
                {
                    return LoadFromAssemblyPath(assemblyPath + ".exe");
                }
            }
            return null;
        }

        public Assembly? LoadAssemblyFromFileInfo(FileInfo assemblyFile)
        {
            var dir = Path.GetDirectoryName(assemblyFile.FullName)!;
            if (!_resolvePaths.Contains(dir)) _resolvePaths.Add(dir);
            var assemblyName = new AssemblyName(assemblyFile.NameWithoutExtension());
            return TryLoad(assemblyName);
        }
    }
}