using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ModPackager.DataStructures;
using Vintagestory.API.Common;

namespace ModPackager.Extensions;

internal static class AssemblyExtensions
{
    internal static string GetVersion(this Assembly assembly, VersioningStyle versionType)
    {
        var modInfo = assembly.GetCustomAttribute<ModInfoAttribute>()
            ?? throw new CustomAttributeFormatException($"No ModInfoAttribute found in assembly: {assembly.FullName}.");

        return versionType == VersioningStyle.Static
            ? modInfo.Version
            : FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion!;
    }

    internal static string GetConfigurationSuffix(this Assembly assembly)
    {
        var debugMode = assembly.GetCustomAttributes(false)
            .OfType<DebuggableAttribute>()
            .Any(da => da.IsJITTrackingEnabled);
        return debugMode ? "_DEBUG" : "";
    }
}
