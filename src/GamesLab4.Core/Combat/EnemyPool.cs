using GamesLab4.Core.Templates;

namespace GamesLab4.Core.Combat;

public sealed class EnemyPool
{
    private readonly CEnemyTemplate[] templates;
    private readonly double[] chances;
    public int Count => templates.Length;

    public EnemyPool(IEnumerable<CEnemyTemplate> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        templates = source.ToArray();
        CEnemyTemplateList.Validate(templates);
        double sum = templates.Sum(t => t.SpawnChance);
        if (sum <= 0 || !double.IsFinite(sum))
            throw new ArgumentException("Для боя нужен хотя бы один шаблон с положительным шансом появления.");
        chances = templates.Select(t => t.SpawnChance / sum).ToArray();
    }

    public CEnemyTemplate Select(double roll)
    {
        if (!double.IsFinite(roll) || roll is < 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(roll));
        double cumulative = 0;
        CEnemyTemplate? last = null;
        for (int i = 0; i < templates.Length; i++)
        {
            if (chances[i] <= 0) continue;
            last = templates[i]; cumulative += chances[i];
            if (roll < cumulative) return templates[i];
        }
        return last!; // Закрываем возможный зазор округления у правой границы.
    }
}
