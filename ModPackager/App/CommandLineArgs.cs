using CommandLine;

namespace ModPackager.App
{
    public class CommandLineArgs
    {
        [Option('a', "assembly", 
            Required = false, 
            MetaValue = "$(TargetPath)",
            HelpText = "The absolute path to the Mod's main assembly file. MSBuild: $(TargetPath)")]
        public string? AssemblyPath { get; set; }

        [Option('o', "output",
            Required = false,
            MetaValue = "$(VINTAGE_STORY_DATA)\\MyMods",
            HelpText = "The absolute path to the directory in which to save the resulting zip file. MSBuild: $(VINTAGE_STORY_DATA)\\MyMods")]
        public string? OutputDir { get; set; }

        [Option('v', "versioning",
            Required = false,
            MetaValue = "Static|Assembly",
            Default = VersioningStyle.Static,
            HelpText = "The versioning style to use for the Mod. Static = Version taken from ModInfo. Assembly = Version taken from AssemblyInfo")]
        public VersioningStyle VersioningStyle { get; set; }

        [Option('s', "saproj",
            Required = false,
            MetaValue = "$(TargetDir)$(ProjectName).saproj",
            HelpText = "The fully qualified path to the .saproj to use for mod assembly packing. MSBuild: $(TargetDir)$(ProjectName).saproj")]
        public string? SmartAssemblyProjectPath { get; set; }
    }
}