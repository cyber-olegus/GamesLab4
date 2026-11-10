using System.Text.Json;
using GamesLab4.Core.Persistence;

namespace GamesLab4.Core.Templates;

public sealed class CEnemyTemplateList
{
    private readonly ISaveList<List<CEnemyTemplate>> serializer;
    private List<CEnemyTemplate> items = new();
    public IReadOnlyList<CEnemyTemplate> Items => items.AsReadOnly();

    public CEnemyTemplateList(ISaveList<List<CEnemyTemplate>> serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        this.serializer = serializer;
    }

    public void Load(string path)
    {
        var loaded = serializer.Load(path);
        Validate(loaded);
        items = new(loaded); // Ошибка загрузки оставляет открытый список без изменений.
    }

    public void Save(string path) => serializer.Save(new(items), path);

    public void Add(CEnemyTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        var candidate = new List<CEnemyTemplate>(items) { template };
        Validate(candidate);
        items = candidate;
    }

    public void Replace(int index, CEnemyTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        var candidate = new List<CEnemyTemplate>(items);
        candidate[index] = template;
        Validate(candidate);
        items = candidate;
    }

    public void RemoveAt(int index) => items.RemoveAt(index);

    internal static void Validate(IEnumerable<CEnemyTemplate> templates)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var template in templates)
        {
            if (template is null) throw new JsonException("В списке не должно быть пустых шаблонов.");
            if (!names.Add(template.Name)) throw new JsonException($"Имя «{template.Name}» уже есть в списке.");
        }
    }
}
