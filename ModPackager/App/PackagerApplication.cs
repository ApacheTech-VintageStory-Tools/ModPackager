using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModPackager.AssemblyLoad;
using ModPackager.DataStructures;
using ModPackager.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

[assembly: InternalsVisibleTo("ModPackager.Tests")]

namespace ModPackager.App;

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
            .WithParsedAsync(PackageModAsync);
    }

    internal async Task PackageModAsync(CommandLineArgs args)
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
        var alc = new CustomAssemblyLoadContext2();

        ValidateAssemblyPath(assemblyPath);
        UpdateVanillaDependencies(assemblyDir, alc);
        await GenerateModInfoFileAsync(args, assemblyPath, tempDir);
        CreateOutputFolder(outputDir);
        CreateIntermediateFolder(tempDir);
        var xmlSaProj = ParseSmartAssemblyProjectFile(saprojPath);
        var root = xmlSaProj["SmartAssemblyProject"]!;
        FixSmartAssemblyProjectInputAssembly(root, assemblyPath);
        FixSmartAssemblyProjectOutputFolder(root, tempAssemblyPath);
        var assemblies = root["Configuration"]!["Assemblies"]!.ChildNodes;
        var mergedAssemblies = CollateMergedAssemblies(assemblies, assemblyDir);
        var embeddedAssemblies = CollateEmbeddedAssemblies(assemblies, assemblyDir);
        SaveSmartAssemblyProjectFile(xmlSaProj, saprojPath);
        await RunSmartAssemblyProjectFileAsync(saprojPath);
        RemoveFilesHandledBySmartAssembly(assemblyPath, saprojPath, mergedAssemblies, embeddedAssemblies);
        RemoveCompileTimeLibraries(assemblyDir);
        RemoveJunkFiles(assemblyDir);
        MoveIncludesFolder(assemblyDir, tempDir.FullName);
        MoveLibrariesNotHandledBySmartAssembly(assemblyDir, tempDir.FullName);
        CopyMergedAssemblyToDebugDirectory(tempAssemblyPath, args.DebugOutputDir);
        CreateModArchive(args, tempDir.FullName, outputDir.FullName, projectName);
        CleanTemporaryFolders(tempDir);
        _logger.LogInformation("Packaging complete. Mod Archive created successfully.");
    }

    private static void ValidateAssemblyPath(FileInfo assemblyPath)
    {
        if (!assemblyPath.Exists)
        {
            throw new FileNotFoundException("Assembly file not found.");
        }
    }

    private void UpdateVanillaDependencies(DirectoryInfo _, CustomAssemblyLoadContext2 __)
    {
        _logger.LogInformation("Updating vanilla dependencies...");
        var dependencies = _config
            .GetSection("Packager")
            .GetSection("VanillaDependencies")
            .Get<string[]>()!;
        var vanillaDependencies = dependencies.Select(d => new FileInfo(Path.Combine(Environment.CurrentDirectory, d))).ToList();
        var installDirFiles = dependencies.Select(d => new FileInfo(Path.Combine(Environment.GetEnvironmentVariable("VINTAGE_STORY")!, d)));
        vanillaDependencies.LockStep(installDirFiles, (packagerDirFile, installDirFile) =>
        {
            if (packagerDirFile.IsDuplicate(installDirFile)) return;
            installDirFile.CopyTo(packagerDirFile.FullName, overwrite: true);
        });
        //foreach (var assembly in vanillaDependencies)
        //{
        //    alc.LoadAssemblyFromFileInfo(assembly);
        //}
    }

    private void CreateOutputFolder(DirectoryInfo outputDir)
    {
        _logger.LogInformation("Creating output folder...");
        if (!outputDir.Exists) outputDir.Create();
        outputDir.Purge();
    }

    private void CreateIntermediateFolder(DirectoryInfo tempDir)
    {
        _logger.LogInformation("Creating intermediate folder...");
        if (!tempDir.Exists) tempDir.Create();
        tempDir.Purge();
    }

    private XmlDocument ParseSmartAssemblyProjectFile(string saprojPath)
    {
        _logger.LogInformation("Parsing SmartAssembly Project file...");
        var xmlSaProj = new XmlDocument();
        xmlSaProj.Load(saprojPath);
        return xmlSaProj;
    }

    private void FixSmartAssemblyProjectInputAssembly(XmlElement root, FileInfo assemblyPath)
    {
        _logger.LogInformation("Fixing SmartAssembly Project input assembly...");
        var mainAssemblyFileName = root["MainAssemblyFileName"]!;
        mainAssemblyFileName.InnerText = assemblyPath.FullName;
    }

    private void FixSmartAssemblyProjectOutputFolder(XmlElement root, string tempAssemblyPath)
    {
        _logger.LogInformation("Fixing SmartAssembly Project output folder...");
        var destination = root["Configuration"]!["Destination"];
        destination?.SetAttribute("DestinationFileName", tempAssemblyPath);
    }

    private List<FileInfo> CollateMergedAssemblies(XmlNodeList assemblies, DirectoryInfo assemblyDir)
    {
        _logger.LogInformation("Collating list of merged assemblies...");
        return assemblies.FilterMergedAssemblies(assemblyDir);
    }

    private List<FileInfo> CollateEmbeddedAssemblies(XmlNodeList assemblies, DirectoryInfo assemblyDir)
    {
        _logger.LogInformation("Collating list of embedded assemblies...");
        return assemblies.FilterEmbeddedAssemblies(assemblyDir);
    }

    private void SaveSmartAssemblyProjectFile(XmlDocument xmlSaProj, string saprojPath)
    {
        _logger.LogInformation("Saving SmartAssembly Project file...");
        xmlSaProj.Save(saprojPath);
    }

    private async Task RunSmartAssemblyProjectFileAsync(string saprojPath)
    {
        _logger.LogInformation("Running SmartAssembly Project file...");
        var saConsole = _config.GetSection("SmartAssembly").GetValue<string>("Location");
        await Process.Start($@"{saConsole}", saprojPath).WaitForExitAsync();
    }

    private async Task GenerateModInfoFileAsync(CommandLineArgs args, FileInfo targetPath, DirectoryInfo targetDir)
    {
        if (File.Exists(Path.Combine(targetDir.FullName, "modinfo.json"))) return;
        _logger.LogInformation("Generating `modinfo.json` file...");
        await ModInfoFileGenerator.App.ConvertAsync(new()
        {
            TargetPath = targetPath.FullName,
            TargetDir = targetDir.FullName,
            VersionType = args.VersioningStyle == VersioningStyle.Static ? "static" : "assembly"
        });
    }

    private void RemoveFilesHandledBySmartAssembly(FileInfo assemblyPath, string saprojPath, List<FileInfo> mergedAssemblies, List<FileInfo> embeddedAssemblies)
    {
        _logger.LogInformation("Removing files already handled by SmartAssembly...");
        assemblyPath.Delete();
        File.Delete(saprojPath);
        foreach (var path in mergedAssemblies.Where(path => path.Exists))
        {
            path.Delete();
        }
        foreach (var path in embeddedAssemblies.Where(path => path.Exists))
        {
            path.Delete();
        }
    }

    private void RemoveCompileTimeLibraries(DirectoryInfo assemblyDir)
    {
        _logger.LogInformation("Removing CompileTime libraries...");
        var compileTimeLibraries = _config
            .GetSection("Packager")
            .GetSection("CompileTimeOnlyAssemblies")
            .Get<string[]>();
        if (compileTimeLibraries?.Length > 0)
        {
            foreach (var file in compileTimeLibraries)
            {
                var path = new FileInfo(Path.Combine(assemblyDir.FullName, file));
                if (path.Exists) path.Delete();
            }
        }
    }

    private void RemoveJunkFiles(DirectoryInfo assemblyDir)
    {
        _logger.LogInformation("Removing junk files...");
        var junkFiles = _config
            .GetSection("Packager")
            .GetSection("JunkFiles")
            .Get<string[]>();
        if (junkFiles?.Length > 0)
        {
            foreach (var file in junkFiles
            .SelectMany(searchTerm => assemblyDir.EnumerateFiles(searchTerm, SearchOption.AllDirectories)))
            {
                file.Delete();
            }
        }
    }

    private void MoveIncludesFolder(DirectoryInfo assemblyDir, string tempDir)
    {
        _logger.LogInformation("Moving `_Includes` Folder to Temp Directory...");
        var includesDir = new DirectoryInfo(Path.Combine(assemblyDir.FullName, "_Includes"));
        includesDir.MoveFilesAndFoldersTo(tempDir);
    }

    private void MoveLibrariesNotHandledBySmartAssembly(DirectoryInfo assemblyDir, string tempDir)
    {
        _logger.LogInformation("Moving Libraries Not Handled by SmartAssembly to Temp Directory...");
        var files = assemblyDir.EnumerateFiles("*.dll");
        foreach (var file in files)
        {
            File.Move(file.FullName, Path.Combine(tempDir, file.Name));
        }
    }

    private void CopyMergedAssemblyToDebugDirectory(string tempAssemblyPath, string? debugOutputDir)
    {
        if (string.IsNullOrEmpty(debugOutputDir)) return;
        _logger.LogInformation("Copying merged assembly to Debug directory...");
        var debugDir = new DirectoryInfo(debugOutputDir);
        if (!debugDir.Exists) debugDir.Create();
        var debugAssemblyPath = Path.Combine(debugDir.FullName, Path.GetFileName(tempAssemblyPath));
        var unmergedDebugDir = Path.Combine(debugDir.FullName, "unmerged");
        Directory.CreateDirectory(unmergedDebugDir);
        var unmergedDebugAssemblyPath = Path.Combine(unmergedDebugDir, Path.GetFileName(tempAssemblyPath));
        File.Copy(debugAssemblyPath, unmergedDebugAssemblyPath, true);
        File.Copy(tempAssemblyPath, debugAssemblyPath, true);
    }

    private void CreateModArchive(CommandLineArgs args, string tempDir, string outputDir, string projectName)
    {
        var assemblyPath = Path.Combine(tempDir, $"{projectName}.dll");
        if (!File.Exists(assemblyPath))
        {
            _logger.LogError("Assembly file {FilePath} does not exist. Cannot create mod archive.", assemblyPath);
            return;
        }
        var assembly = Assembly.LoadFrom(assemblyPath);
        var version = assembly.GetVersion(args.VersioningStyle);
        var configuration = assembly.GetConfigurationSuffix();

        _logger.LogInformation("Creating Mod Archive...");
        var archiveName = $"{projectName}_v{version}{configuration}.zip";
        var archivePath = Path.Combine(outputDir, archiveName);
        ZipFile.CreateFromDirectory(tempDir, archivePath);
    }

    private void CleanTemporaryFolders(DirectoryInfo tempDir)
    {
        _logger.LogInformation("Cleaning temporary folders...");
        tempDir.Delete(true);
    }
}