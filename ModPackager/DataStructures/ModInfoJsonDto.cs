using System.Collections.Generic;
using ModPackager.JsonConverters;
using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace ModPackager.DataStructures
{
    [JsonObject]
    public record ModInfoJsonDto
    {
        [JsonProperty("type", Required = Required.Always)]
        public string Type { get; init; } = "Code";

        [JsonProperty("name")]
        public string Name { get; init; } = "Untitled Mod";

        [JsonProperty("modId", Required = Required.Always)]
        public string ModId { get; init; } = "untitled";

        [JsonProperty("side", Required = Required.Always)]
        public string Side { get; init; } = "Universal";

        [JsonProperty("description", Required = Required.Always)]
        public string Description { get; init; } = "<Insert Description Here>";

        [JsonProperty("version", Required = Required.Always)]
        public string Version { get; init; } = "0.1.0";

        [JsonProperty("website", NullValueHandling = NullValueHandling.Ignore)]
        public string Website { get; init; } = "https://apachetech.co.uk";

        [JsonProperty("authors", Required = Required.Always)]
        public IReadOnlyList<string> Authors { get; init; } = new List<string> { "ApacheTech Solutions" };

        [JsonProperty("contributors", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyList<string> Contributors { get; init; } = new List<string> { "ApacheTech Solutions" };

        [JsonProperty("requiredOnClient", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RequiredOnClient { get; init; }

        [JsonProperty("requiredOnServer", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RequiredOnServer { get; init; }

        [JsonProperty("networkVersion", NullValueHandling = NullValueHandling.Ignore)]
        public string? NetworkVersion { get; init; }

        [JsonProperty("worldConfig", NullValueHandling = NullValueHandling.Ignore)]
        public string? WorldConfig { get; init; }

        [JsonProperty("iconPath", NullValueHandling = NullValueHandling.Ignore)]
        public string? IconPath { get; init; }

        [JsonProperty("dependencies", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonConverter(typeof(DependenciesConverter))]
        public IReadOnlyList<ModDependency>? Dependencies { get; init; }
    }
}
