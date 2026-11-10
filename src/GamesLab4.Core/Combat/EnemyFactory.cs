using System.Reflection;
using GamesLab4.Core.Numbers;
using GamesLab4.Core.Templates;

namespace GamesLab4.Core.Combat;

public static class EnemyFactory
{
    // Допускаем только конкретных публичных наследников из своей сборки.
    private static readonly Dictionary<string, Type> Types = typeof(EnemyFactory).Assembly.GetTypes()
        .Where(t => t.IsPublic && !t.IsAbstract && typeof(CEnemy).IsAssignableFrom(t))
        .ToDictionary(t => t.Name, StringComparer.Ordinal);

    public static IEnemy CreateEnemy(string typeName, params object[] args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        if (!Types.TryGetValue(typeName, out Type? type))
            throw new NotSupportedException($"Тип противника «{typeName}» не найден.");
        try
        {
            return (IEnemy)(Activator.CreateInstance(type, args) ??
                throw new InvalidOperationException("Фабрика не получила экземпляр противника."));
        }
        catch (MissingMethodException e)
        {
            throw new ArgumentException($"Параметры не подходят конструктору «{typeName}».", nameof(args), e);
        }
        catch (TargetInvocationException e)
        {
            throw new InvalidOperationException($"Не удалось создать «{typeName}»: {e.InnerException?.Message ?? e.Message}", e);
        }
    }

    public static IEnemy Create(CEnemyTemplate template, int victories, Random random)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(random);
        if (victories < 0) throw new ArgumentOutOfRangeException(nameof(victories));
        BigNumber life = new(template.BaseLife), gold = new(template.BaseGold);
        // Сохраняем поразрядное масштабирование Лабы 2. Ограничиваем число шагов
        // роста при загрузке вручную изменённого файла с огромным счётчиком.
        for (int i = 0; i < Math.Min(victories, 1000); i++)
        {
            BigNumber nextLife = life * template.LifeModifier, nextGold = gold * template.GoldModifier;
            if (nextLife == life && nextGold == gold) break;
            life = nextLife; gold = nextGold;
        }
        if (life == BigNumber.Zero) life = BigNumber.One;
        var args = new List<object> { template.Name, life, gold, template.IconName };
        if (template is IEnemySpawnOptions extra) args.AddRange(extra.AdditionalArguments(random));
        return CreateEnemy(template.EnemyClassName, args.ToArray());
    }
}
