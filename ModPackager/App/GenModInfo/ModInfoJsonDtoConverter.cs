using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;

namespace ModPackager.App.GenModInfo
{
    internal static class ModInfoJsonDtoConverter
    {
        internal static ModInfoJsonDto PopulateJsonDto(this Assembly assembly, VersioningStyle versionType)
        {
            var modInfo = ExtractModFileInfo(assembly);
            if (modInfo == null) throw new CustomAttributeFormatException("No ModInfoAttribute found in assembly.");
            var dependencies = assembly.FindAllModDependencies();
            var debugMode = assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
            var version = versionType == VersioningStyle.Static
                ? modInfo.Version
                : FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion!;
            GenModInfo.Version = version;
            GenModInfo.Configuration = debugMode ? "_DEBUG" : "";
            return modInfo.ToDto(version, dependencies);
        }

        private static ModInfoAttribute? ExtractModFileInfo(Assembly assembly)
        {
            return assembly.GetCustomAttribute<ModInfoAttribute>();
        }

        private static ModInfoJsonDto ToDto(this ModInfoAttribute modInfo, string version,
            IReadOnlyList<ModDependency> dependencies)
        {
            if (!Enum.TryParse(modInfo.Side, true, out EnumAppSide _))
                throw new ArgumentException(
                    $"Cannot parse '{modInfo.Side}', must be either 'Client', 'Server' or 'Universal'. Defaulting to 'Universal'.");

            var dto = new ModInfoJsonDto
            {
                Type = "Code",
                Name = modInfo.Name,
                ModId = modInfo.ModID,
                Side = modInfo.Side,
                Description = modInfo.Description,
                Version = version,
                Authors = modInfo.Authors,
                Contributors = modInfo.Contributors,
                NetworkVersion = modInfo.NetworkVersion,
                RequiredOnClient = modInfo.RequiredOnClient,
                RequiredOnServer = modInfo.RequiredOnServer,
                WorldConfig = modInfo.WorldConfig,
                Website = modInfo.Website,
                IconPath = modInfo.IconPath,
                Dependencies = dependencies
            };
            return dto;
        }

        private static List<ModDependency> FindAllModDependencies(this Assembly assembly)
        {
            return assembly
                .GetCustomAttributes<ModDependencyAttribute>()
                .Select(p => new ModDependency(p.ModID, p.Version))
                .ToList();
        }
    }
}
