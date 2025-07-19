namespace ApacheTech.VintageMods.Common.ModInfoGenerator.Converters;

public class DependenciesConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(IEnumerable<ModDependency>).IsAssignableFrom(objectType);
    }

    public override object ReadJson(
        JsonReader reader, 
        Type objectType, 
        object? existingValue,
        JsonSerializer serializer)
    {
        return JObject
            .Load(reader)
            .Properties()
            .Select(prop => new ModDependency(prop.Name, (string)prop.Value!))
            .ToList()
            .AsReadOnly();
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        foreach (var modDependency in (IEnumerable<ModDependency>)value!)
        {
            writer.WritePropertyName(modDependency.ModID);
            writer.WriteValue(modDependency.Version);
        }

        writer.WriteEndObject();
    }
}
