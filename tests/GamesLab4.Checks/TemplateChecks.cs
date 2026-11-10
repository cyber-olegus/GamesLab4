using System.Text.Json;
using System.Text.Json.Nodes;
using GamesLab4.Core.Persistence;
using GamesLab4.Core.Templates;

namespace GamesLab4.Checks;

internal static class TemplateChecks
{
    private static string Example(string name = "enemies.example.json") =>
        Path.Combine(AppContext.BaseDirectory, "Data", name);
    private static CNormalEnemyTemplate Normal(string name = "Обычный") => new(name, "goblin_1.png", 40, 1.1, 25, 1.1, 1);

    private sealed class Temp : IDisposable
    {
        private readonly string folder = Path.Combine(Path.GetTempPath(), "lab4-templates-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(folder);
        public string FileAt(string name) => Path.Combine(folder, name);
        public void Dispose() => Directory.Delete(folder, true);
    }
    private sealed class MemorySaver : ISaveList<List<CEnemyTemplate>>
    {
        public List<CEnemyTemplate> Data { get; private set; } = [];
        public string? SavedPath { get; private set; }
        public List<CEnemyTemplate> Load(string path) => new(Data);
        public void Save(List<CEnemyTemplate> data, string path) { Data = new(data); SavedPath = path; }
    }

    [Check("Редактор: четыре типа проходят полный цикл сохранения и загрузки")]
    public static void RoundTrip()
    {
        using var temp = new Temp();
        ISaveList<List<CEnemyTemplate>> saver = new JsonEnemySaver();
        List<CEnemyTemplate> mixed = saver.Load(Example());
        string path = temp.FileAt("mixed.json");
        saver.Save(mixed, path);
        var restored = saver.Load(path);
        Verify.Equal(4, restored.Count);
        Verify.True(restored[0] is CNormalEnemyTemplate);
        Verify.Equal(3, ((CArmoredEnemyTemplate)restored[1]).Armor);
        Verify.Near(.25, ((CHealingEnemyTemplate)restored[2]).HealChance);
        Verify.Equal(8, ((CHealingEnemyTemplate)restored[2]).HealAmount);
        Verify.Equal(2, ((CShieldedEnemyTemplate)restored[3]).ShieldHits);
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        Verify.Equal("CArmoredEnemyTemplate", json.RootElement[1].GetProperty("$type").GetString());
        Verify.True(!json.RootElement[1].TryGetProperty("EnemyClassName", out _));
        Verify.True(!json.RootElement[1].TryGetProperty("Summary", out _));
        for (int i = 0; i < mixed.Count; i++)
        {
            Verify.Equal(mixed[i].Name, restored[i].Name);
            Verify.Equal(mixed[i].IconName, restored[i].IconName);
            Verify.Equal(mixed[i].BaseLife, restored[i].BaseLife);
            Verify.Equal(mixed[i].BaseGold, restored[i].BaseGold);
        }
    }

    [Check("Редактор: старый JSON Лабы 1 без $type открывается")]
    public static void Legacy()
    {
        var saver = new JsonEnemySaver();
        var templates = saver.Load(Example("enemies.lab1.json"));
        Verify.Equal(3, templates.Count);
        Verify.True(templates.All(t => t is CNormalEnemyTemplate));
        Verify.Equal("goblin_1.png", templates[0].IconName);
        using var result = JsonDocument.Parse(saver.ToJson(templates));
        Verify.Equal("CNormalEnemyTemplate", result.RootElement[0].GetProperty("$type").GetString());
    }

    private static JsonObject FirstNormal()
    {
        var doc = JsonNode.Parse(new JsonEnemySaver().ToJson([Normal()]))!.AsArray();
        return doc[0]!.AsObject();
    }
    private static string One(JsonObject template) => "[" + template.ToJsonString() + "]";

    [Check("JSON: неизвестный дискриминатор выдаёт понятную ошибку")]
    public static void UnknownType()
    {
        var item = FirstNormal(); item["$type"] = "Неизвестный";
        Verify.Throws<JsonException>(() => new JsonEnemySaver().FromJson(One(item)));
    }
    [Check("JSON: дискриминатор обязан быть строкой")]
    public static void InvalidDiscriminator()
    {
        var item = FirstNormal(); item["$type"] = 12;
        Verify.Throws<JsonException>(() => new JsonEnemySaver().FromJson(One(item)));
    }
    [Check("JSON: специальное свойство подтипа обязательно")]
    public static void MissingArmor()
    {
        var item = FirstNormal(); item["$type"] = "CArmoredEnemyTemplate";
        Verify.Throws<JsonException>(() => new JsonEnemySaver().FromJson(One(item)));
    }
    [Check("JSON: недопустимое значение подтипа отклоняется")]
    public static void BadShield()
    {
        var item = FirstNormal(); item["$type"] = "CShieldedEnemyTemplate"; item["ShieldHits"] = 0;
        Verify.Throws<JsonException>(() => new JsonEnemySaver().FromJson(One(item)));
    }
    [Check("JSON: пустые элементы и повторяющиеся имена отклоняются")]
    public static void InvalidList()
    {
        var saver = new JsonEnemySaver();
        Verify.Throws<JsonException>(() => saver.FromJson("[null]"));
        Verify.Throws<JsonException>(() => saver.ToJson([Normal("Боец"), Normal("боец")]));
    }
    [Check("JSON: обязательное поле награды не заменяется нулём")]
    public static void RequiredCommonProperty()
    {
        var item = FirstNormal(); item.Remove("BaseGold");
        Verify.Throws<JsonException>(() => new JsonEnemySaver().FromJson(One(item)));
    }
    [Check("Редактор: пустой список можно сохранить и прочитать")]
    public static void EmptyList()
    {
        var saver = new JsonEnemySaver();
        Verify.Equal(0, saver.FromJson(saver.ToJson([])).Count);
    }
    [Check("Редактор: создание, замена типа и удаление шаблона")]
    public static void EditList()
    {
        var list = new CEnemyTemplateList(new MemorySaver());
        list.Add(Normal());
        list.Replace(0, new CArmoredEnemyTemplate("Боец", "troll_1.png", 50, 1.1, 30, 1.1, 1, 6));
        Verify.Equal(6, ((CArmoredEnemyTemplate)list.Items[0]).Armor);
        list.RemoveAt(0);
        Verify.Equal(0, list.Items.Count);
    }
    [Check("Редактор: ошибка загрузки не удаляет открытые шаблоны")]
    public static void FailedLoadKeepsList()
    {
        using var temp = new Temp();
        var list = new CEnemyTemplateList(new JsonEnemySaver());
        list.Add(Normal());
        string path = temp.FileAt("broken.json"); File.WriteAllText(path, "не JSON");
        Verify.Throws<JsonException>(() => list.Load(path));
        Verify.Equal("Обычный", list.Items.Single().Name);
    }
    [Check("ISaveList: редактор работает с другой реализацией хранения")]
    public static void UsesInterface()
    {
        var saver = new MemorySaver();
        var list = new CEnemyTemplateList(saver);
        list.Add(Normal("Сохранённый")); list.Save("в памяти");
        Verify.Equal("в памяти", saver.SavedPath);
        list.RemoveAt(0); list.Load("в памяти");
        Verify.Equal("Сохранённый", list.Items.Single().Name);
    }
}
