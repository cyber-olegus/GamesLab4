using System.Text.Json;
using System.Text.Json.Serialization;
using GamesLab4.Core.Templates;

namespace GamesLab4.Core.Persistence;

/// <summary>Дискриминатор — известное имя шаблона, а не произвольный CLR-тип из файла.</summary>
public sealed class EnemyTemplateConverter : JsonConverter<CEnemyTemplate>
{
    private static readonly string[] CommonFields =
        ["Name", "IconName", "BaseLife", "LifeModifier", "BaseGold", "GoldModifier", "SpawnChance"];

    private static (Type Type, string[] Fields) Resolve(string typeName) => typeName switch
    {
        nameof(CNormalEnemyTemplate) => (typeof(CNormalEnemyTemplate), []),
        nameof(CArmoredEnemyTemplate) => (typeof(CArmoredEnemyTemplate), ["Armor"]),
        nameof(CHealingEnemyTemplate) => (typeof(CHealingEnemyTemplate), ["HealChance", "HealAmount"]),
        nameof(CShieldedEnemyTemplate) => (typeof(CShieldedEnemyTemplate), ["ShieldHits"]),
        _ => throw new JsonException($"Неизвестный тип шаблона: «{typeName}».")
    };

    public override CEnemyTemplate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        JsonElement item = document.RootElement;
        if (item.ValueKind != JsonValueKind.Object) throw new JsonException("Шаблон должен быть объектом JSON.");

        var fields = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty field in item.EnumerateObject())
            if (!fields.TryAdd(field.Name, field.Value))
                throw new JsonException($"Поле «{field.Name}» указано дважды.");

        // В файлах Лабы 1 дискриминатора ещё не было: это обычный противник.
        string typeName = nameof(CNormalEnemyTemplate);
        if (fields.TryGetValue("$type", out JsonElement discriminator))
        {
            if (discriminator.ValueKind != JsonValueKind.String)
                throw new JsonException("Поле $type должно быть строкой.");
            typeName = discriminator.GetString()!;
        }

        var (type, extra) = Resolve(typeName);
        foreach (string field in CommonFields.Concat(extra))
            if (!fields.TryGetValue(field, out var value) || value.ValueKind == JsonValueKind.Null)
                throw new JsonException($"Отсутствует обязательное поле «{field}» у типа «{typeName}».");

        try
        {
            // Конвертер применяется только к базовому типу. Здесь тип уже конкретный.
            return (CEnemyTemplate)(JsonSerializer.Deserialize(item, type, options)
                ?? throw new JsonException("Пустой шаблон противника."));
        }
        catch (ArgumentException exception)
        {
            throw new JsonException($"Недопустимый шаблон: {exception.Message}", exception);
        }
    }

    public override void Write(Utf8JsonWriter writer, CEnemyTemplate value, JsonSerializerOptions options)
    {
        Type concrete = value.GetType();
        if (Resolve(concrete.Name).Type != concrete)
            throw new JsonException("Этот тип шаблона не зарегистрирован.");
        JsonElement data = JsonSerializer.SerializeToElement(value, concrete, options);
        writer.WriteStartObject();
        writer.WriteString("$type", concrete.Name);
        foreach (JsonProperty field in data.EnumerateObject()) field.WriteTo(writer);
        writer.WriteEndObject();
    }
}
