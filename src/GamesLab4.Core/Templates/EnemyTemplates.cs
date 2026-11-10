using System.Text.Json.Serialization;

namespace GamesLab4.Core.Templates;

public sealed class CNormalEnemyTemplate : CEnemyTemplate
{
    [JsonConstructor]
    public CNormalEnemyTemplate(string name, string iconName, int baseLife, double lifeModifier,
        int baseGold, double goldModifier, double spawnChance)
        : base(name, iconName, baseLife, lifeModifier, baseGold, goldModifier, spawnChance,
            "CNormalEnemy", "Обычный") { }
}

public sealed class CArmoredEnemyTemplate : CEnemyTemplate, ITemplateSummary, IEnemySpawnOptions
{
    public int Armor { get; }
    [JsonIgnore] public string Summary => $"Снижает урон на {Armor}; минимум — 1.";

    [JsonConstructor]
    public CArmoredEnemyTemplate(string name, string iconName, int baseLife, double lifeModifier,
        int baseGold, double goldModifier, double spawnChance, int armor)
        : base(name, iconName, baseLife, lifeModifier, baseGold, goldModifier, spawnChance,
            "CArmoredEnemy", "Бронированный")
    {
        if (armor <= 0) throw new ArgumentException("Броня должна быть больше нуля.");
        Armor = armor;
    }
    public object[] AdditionalArguments(Random random) => [Armor];
}

public sealed class CHealingEnemyTemplate : CEnemyTemplate, ITemplateSummary, IEnemySpawnOptions
{
    public double HealChance { get; }
    public int HealAmount { get; }
    [JsonIgnore] public string Summary => $"Шанс лечения {HealChance:P0}: +{HealAmount} здоровья вместо удара.";

    [JsonConstructor]
    public CHealingEnemyTemplate(string name, string iconName, int baseLife, double lifeModifier,
        int baseGold, double goldModifier, double spawnChance, double healChance, int healAmount)
        : base(name, iconName, baseLife, lifeModifier, baseGold, goldModifier, spawnChance,
            "CHealingEnemy", "Лечащийся")
    {
        if (!double.IsFinite(healChance) || healChance is < 0 or > 1 || healAmount <= 0)
            throw new ArgumentException("Шанс лечения должен быть от 0 до 1, объём лечения — больше нуля.");
        HealChance = healChance; HealAmount = healAmount;
    }
    public object[] AdditionalArguments(Random random) => [HealChance, HealAmount, random];
}

public sealed class CShieldedEnemyTemplate : CEnemyTemplate, ITemplateSummary, IEnemySpawnOptions
{
    public int ShieldHits { get; }
    [JsonIgnore] public string Summary => $"Щит блокирует первые {ShieldHits} попаданий.";

    [JsonConstructor]
    public CShieldedEnemyTemplate(string name, string iconName, int baseLife, double lifeModifier,
        int baseGold, double goldModifier, double spawnChance, int shieldHits)
        : base(name, iconName, baseLife, lifeModifier, baseGold, goldModifier, spawnChance,
            "CShieldedEnemy", "Со щитом")
    {
        if (shieldHits is < 1 or > 100) throw new ArgumentException("Число блокируемых попаданий: от 1 до 100.");
        ShieldHits = shieldHits;
    }
    public object[] AdditionalArguments(Random random) => [ShieldHits];
}
