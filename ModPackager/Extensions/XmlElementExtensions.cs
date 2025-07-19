using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace ModPackager.Extensions
{
    public static class XmlElementExtensions
    {

        private static List<FileInfo> FilterAssemblies(IEnumerable assemblies, FileSystemInfo assemblyDir, string action, string identity)
        {
            return assemblies.Cast<XmlElement>()
                .Where(assembly => assembly[action]?.GetAttribute(identity) == "1")
                .Select(assembly => assembly.GetAttribute("AssemblyName").Split(',', 2)[0])
                .Select(file => Path.Combine(assemblyDir.FullName, $"{file}.dll"))
                .Select(path => new FileInfo(path)).ToList();
        }

        public static List<FileInfo> FilterEmbeddedAssemblies(this XmlNodeList assemblies, FileSystemInfo assemblyDir)
        {
            return FilterAssemblies(assemblies, assemblyDir, "Embedding", "Embed");
        }

        public static List<FileInfo> FilterMergedAssemblies(this XmlNodeList assemblies, FileSystemInfo assemblyDir)
        {
            return FilterAssemblies(assemblies, assemblyDir, "Merging", "Merge");
        }
    }
}