using System.Text.Json.Serialization;

namespace GamesLab4.Core.Templates;

public interface ITemplateSummary
{
    string Summary { get; }
}

public interface IEnemySpawnOptions
{
    object[] AdditionalArguments(Random random);
}

/// <summary>Общие поля JSON из редактора Лабы 1. Шаблон не хранит состояние боя.</summary>
public abstract class CEnemyTemplate
{
    public string Name { get; }
    public string IconName { get; }
    public int BaseLife { get; }
    public double LifeModifier { get; }
    public int BaseGold { get; }
    public double GoldModifier { get; }
    public double SpawnChance { get; }
    [JsonIgnore] public string EnemyClassName { get; }
    [JsonIgnore] public string TypeLabel { get; }

    protected CEnemyTemplate(string name, string iconName, int baseLife, double lifeModifier,
        int baseGold, double goldModifier, double spawnChance, string enemyClassName, string typeLabel)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Укажите имя противника.");
        if (string.IsNullOrWhiteSpace(iconName) || iconName.Contains('/') || iconName.Contains('\\') || iconName.Contains(':'))
            throw new ArgumentException("Укажите имя PNG без пути, например goblin_1.png.");
        if (baseLife <= 0 || baseGold < 0) throw new ArgumentException("Здоровье должно быть положительным, награда — неотрицательной.");
        if (!double.IsFinite(lifeModifier) || lifeModifier <= 0)
            throw new ArgumentException("Множитель здоровья должен быть больше нуля.");
        if (!double.IsFinite(goldModifier) || goldModifier < 0)
            throw new ArgumentException("Множитель золота должен быть неотрицательным.");
        if (!double.IsFinite(spawnChance) || spawnChance is < 0 or > 1)
            throw new ArgumentException("Шанс появления указывается от 0 до 1.");
        Name = name.Trim(); IconName = iconName.Trim();
        BaseLife = baseLife; LifeModifier = lifeModifier; BaseGold = baseGold;
        GoldModifier = goldModifier; SpawnChance = spawnChance;
        EnemyClassName = enemyClassName; TypeLabel = typeLabel;
    }
}
