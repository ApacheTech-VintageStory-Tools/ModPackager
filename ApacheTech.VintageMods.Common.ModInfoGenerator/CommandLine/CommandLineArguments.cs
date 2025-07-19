using ApacheTech.VintageMods.Common.ModInfoGenerator.Enums;
using CommandLine;

namespace ApacheTech.VintageMods.Common.ModInfoGenerator.CommandLine;

internal class CommandLineArguments
{
    [Option('a', "assembly",
        Required = true,
        MetaValue = "$(TargetPath)",
        HelpText = "The absolute path to the Mod's main assembly file. MSBuild: $(TargetPath)")]
    public string AssemblyPath { get; set; }

    [Option('o', "output",
        Required = true,
        MetaValue = "$(TargetDir)",
        HelpText = "The absolute path to the directory in which to save the resulting modinfo.json file. MSBuild: $(TargetDir)")]
    public string OutputDir { get; set; }

    [Option('v', "versioning",
        Required = false,
        MetaValue = "Static|Assembly",
        Default = VersioningStyle.Static,
        HelpText = "The versioning style to use for the Mod. Static = Version taken from ModInfo. Assembly = Version taken from AssemblyInfo")]
    public VersioningStyle VersioningStyle { get; set; }
}