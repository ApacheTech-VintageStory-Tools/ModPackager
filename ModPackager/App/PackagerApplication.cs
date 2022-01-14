using System.Collections;
using System.Collections.Generic;
using CommandLine;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

[assembly: InternalsVisibleTo("ModPackager.Tests")]

namespace ModPackager.App
{
    public class PackagerApplication : IPackagerApplication
    {
        private readonly ILogger<PackagerApplication> _logger;
        private readonly IConfiguration _config;

        [ActivatorUtilitiesConstructor]
        public PackagerApplication(ILogger<PackagerApplication> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task RunAsync(params string[] args)
        {
            await Parser.Default
                .ParseArguments<CommandLineArgs>(args)
                .WithParsedAsync(async p => await PackageMod(p));
        }

        internal async Task PackageMod(CommandLineArgs args)
        {
            _logger.LogInformation("Packaging Mod...");
            var assemblyPath = new FileInfo(args.AssemblyPath!);
            var assemblyDir = new DirectoryInfo(assemblyPath.DirectoryName!);
            var assemblyName = assemblyPath.Name;
            var projectName = assemblyPath.Name.Replace(assemblyPath.Extension, "");
            var saprojPath = Path.ChangeExtension(assemblyPath.FullName, ".saproj");
            var tempDir = new DirectoryInfo(Path.Combine(assemblyDir.FullName, "tmp"));
            var outputDir = new DirectoryInfo(Path.Combine(assemblyDir.FullName, "release"));
            var tempAssemblyPath = Path.Combine(tempDir.FullName, assemblyName);

            if (!assemblyPath.Exists)
            {
                throw new FileNotFoundException("Assembly file not found.");
            }

            _logger.LogInformation("Creating output folder...");
            if (!outputDir.Exists) outputDir.Create();
            outputDir.Purge();

            _logger.LogInformation("Creating intermediate folder...");
            if (!tempDir.Exists) tempDir.Create();
            tempDir.Purge();

            _logger.LogInformation("Parsing SmartAssembly Project file...");
            var xmlSaProj = new XmlDocument();
            xmlSaProj.Load(saprojPath);
            var root = xmlSaProj["SmartAssemblyProject"];

            _logger.LogInformation("Fixing SmartAssembly Project input assembly...");
            var mainAssemblyFileName = root!["MainAssemblyFileName"]!;
            mainAssemblyFileName.InnerText = assemblyPath.FullName;

            _logger.LogInformation("Fixing SmartAssembly Project output folder...");
            var destination = root["Configuration"]!["Destination"];
            destination?.SetAttribute("DestinationFileName", tempAssemblyPath);

            _logger.LogInformation("Collating list of merged assemblies...");
            var assemblies = root["Configuration"]!["Assemblies"]!.ChildNodes;
            var mergedAssemblies = assemblies.FilterMergedAssemblies(assemblyDir);

            _logger.LogInformation("Collating list of embedded assemblies...");
            var embeddedAssemblies = assemblies.FilterEmbeddedAssemblies(assemblyDir);

            _logger.LogInformation("Saving SmartAssembly Project file...");
            xmlSaProj.Save(saprojPath);

            _logger.LogInformation("Running SmartAssembly Project file...");
            var saConsole = _config.GetSection("SmartAssembly").GetValue<string>("Location");
            await Process.Start($@"{saConsole}", saprojPath).WaitForExitAsync();

            _logger.LogInformation("Generating `modinfo.json` file...");
            Thread.Sleep(1000);
            var generator = new GenModInfo.GenModInfo(args, tempAssemblyPath, Path.Combine(tempDir.FullName, "modinfo.json"));
            await generator.DoStuff();
            var version = GenModInfo.GenModInfo.Version;
            var configuration = GenModInfo.GenModInfo.Configuration;

            _logger.LogInformation("Removing files already handled by SmartAssembly...");
            assemblyPath.Delete();
            File.Delete(saprojPath);
            foreach (var path in mergedAssemblies)
            {
                if (path.Exists) path.Delete();
            }
            foreach (var path in embeddedAssemblies)
            {
                if (path.Exists) path.Delete();
            }

            _logger.LogInformation("Removing CompileTime libraries...");
            var compileTimeLibraries = _config
                .GetSection("Packager")
                .GetSection("CompileTimeOnlyAssemblies")
                .Get<string[]>();
            foreach (var file in compileTimeLibraries)
            {
                var path = new FileInfo(Path.Combine(assemblyDir.FullName, file));
                if (path.Exists) path.Delete();
            }

            _logger.LogInformation("Removing junk files...");
            var junkFiles = _config
                .GetSection("Packager")
                .GetSection("JunkFiles")
                .Get<string[]>();
            foreach (var file in junkFiles
                .SelectMany(searchTerm => assemblyDir.EnumerateFiles(searchTerm, SearchOption.AllDirectories)))
            {
                file.Delete();
            }

            _logger.LogInformation("Moving `_Includes` Folder to Temp Directory...");
            var includesDir = new DirectoryInfo(Path.Combine(assemblyDir.FullName, "_Includes"));
            includesDir.MoveFilesAndFoldersTo(tempDir.FullName);

            _logger.LogInformation("Moving Libraries Not Handled by SmartAssembly to Temp Directory...");
            var files = assemblyDir.EnumerateFiles("*.dll");
            foreach (var file in files)
            {
                File.Move(file.FullName, Path.Combine(tempDir.FullName, file.Name));
            }

            _logger.LogInformation("Creating Mod Archive...");
            var archiveName = $"{projectName}_v{version}{configuration}.zip";
            var archivePath = Path.Combine(outputDir.FullName, archiveName);
            ZipFile.CreateFromDirectory(tempDir.FullName, archivePath);

            _logger.LogInformation("Cleaning temporary folders...");
            tempDir.Delete(true);

            _logger.LogInformation("Packaging complete. Mod Archive created successfully.");
        }
    }

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

    public static class FileSystemExtensions
    {
        public static void Purge(this DirectoryInfo directory)
        {
            foreach (var file in directory.GetFiles())
            {
                file.Delete();
            }
            foreach (var dir in directory.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        public static void CopyFilesAndFoldersTo(this DirectoryInfo srcPath, string destPath)
        {
            Directory.CreateDirectory(destPath);
            Parallel.ForEach(srcPath.GetDirectories("*", SearchOption.AllDirectories),
              srcInfo => Directory.CreateDirectory($"{destPath}{srcInfo.FullName[srcPath.FullName.Length..]}"));
            Parallel.ForEach(srcPath.GetFiles("*", SearchOption.AllDirectories),
              srcInfo => File.Copy(srcInfo.FullName, $"{destPath}{srcInfo.FullName[srcPath.FullName.Length..]}", true));
        }

        public static void MoveFilesAndFoldersTo(this DirectoryInfo srcPath, string destPath)
        {
            Directory.CreateDirectory(destPath);
            Parallel.ForEach(srcPath.GetDirectories("*", SearchOption.AllDirectories),
              srcInfo => Directory.CreateDirectory($"{destPath}{srcInfo.FullName[srcPath.FullName.Length..]}"));
            Parallel.ForEach(srcPath.GetFiles("*", SearchOption.AllDirectories),
              srcInfo => File.Move(srcInfo.FullName, $"{destPath}{srcInfo.FullName[srcPath.FullName.Length..]}", true));
            srcPath.Delete(true);
        }
    }
}