using GamesLab4.Core.Numbers;

namespace GamesLab4.Core.Combat;

public sealed class CNormalEnemy : CEnemy
{
    public CNormalEnemy(string name, BigNumber baseLife, BigNumber gold, string iconName)
        : base(name, baseLife, gold, iconName) { }
    public override string TypeLabel => "Обычный";
}

public sealed class CArmoredEnemy : CEnemy
{
    public int Armor { get; }
    public CArmoredEnemy(string name, BigNumber baseLife, BigNumber gold, string iconName, int armor)
        : base(name, baseLife, gold, iconName)
    {
        if (armor <= 0) throw new ArgumentOutOfRangeException(nameof(armor));
        Armor = armor;
    }
    public override string TypeLabel => "Бронированный";
    public override string Details => $"Броня уменьшает каждый удар на {Armor}. Минимальный урон: 1.";
    public override HitResult TakeDamage(BigNumber damage)
    {
        if (!CanReceive(damage)) return base.TakeDamage(damage);
        var armor = new BigNumber(Armor);
        BigNumber reduced = damage > armor ? damage - armor : BigNumber.One;
        HitResult result = ApplyDamage(reduced);
        return result with { Message = $"Броня поглотила {damage - reduced}. {result.Message}" };
    }
}

public sealed class CHealingEnemy : CEnemy
{
    private readonly Random random;
    public double HealChance { get; }
    public int HealAmount { get; }
    public CHealingEnemy(string name, BigNumber baseLife, BigNumber gold, string iconName,
        double healChance, int healAmount, Random random)
        : base(name, baseLife, gold, iconName)
    {
        if (!double.IsFinite(healChance) || healChance is < 0 or > 1 || healAmount <= 0)
            throw new ArgumentException("Недопустимые параметры лечения.");
        ArgumentNullException.ThrowIfNull(random);
        HealChance = healChance; HealAmount = healAmount; this.random = random;
    }
    public override string TypeLabel => "Лечащийся";
    public override string Details => $"С вероятностью {HealChance:P0} восстанавливает {HealAmount} здоровья вместо урона.";
    public override HitResult TakeDamage(BigNumber damage) =>
        CanReceive(damage) && HealChance > 0 && random.NextDouble() < HealChance
            ? Heal(new BigNumber(HealAmount)) : base.TakeDamage(damage);
}

public sealed class CShieldedEnemy : CEnemy
{
    public int RemainingBlocks { get; private set; }
    public CShieldedEnemy(string name, BigNumber baseLife, BigNumber gold, string iconName, int shieldHits)
        : base(name, baseLife, gold, iconName)
    {
        if (shieldHits is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(shieldHits));
        RemainingBlocks = shieldHits;
    }
    public override string TypeLabel => "Со щитом";
    public override string Details => RemainingBlocks > 0 ? $"Осталось блоков щитом: {RemainingBlocks}." : "Щит разрушен.";
    public override HitResult TakeDamage(BigNumber damage)
    {
        if (CanReceive(damage) && RemainingBlocks > 0)
        {
            RemainingBlocks--;
            return NoDamage(RemainingBlocks == 0 ? "Щит заблокировал удар и разрушился." : $"Щит заблокировал удар. Осталось блоков: {RemainingBlocks}.");
        }
        return base.TakeDamage(damage);
    }
}
