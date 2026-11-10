using System.Text.Json;
using GamesLab4.Core.Templates;

namespace GamesLab4.Core.Persistence;

public sealed class JsonEnemySaver : ISaveList<List<CEnemyTemplate>>
{
    private readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true, PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new EnemyTemplateConverter() }
    };

    public List<CEnemyTemplate> Load(string path) => FromJson(File.ReadAllText(path));
    public void Save(List<CEnemyTemplate> data, string path) => JsonFile.Write(path, ToJson(data));

    public string ToJson(List<CEnemyTemplate> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        CEnemyTemplateList.Validate(data);
        return JsonSerializer.Serialize(data, options);
    }

    public List<CEnemyTemplate> FromJson(string json)
    {
        var templates = JsonSerializer.Deserialize<List<CEnemyTemplate>>(json, options)
            ?? throw new JsonException("Ожидается массив шаблонов.");
        CEnemyTemplateList.Validate(templates);
        return templates;
    }
}
