using GamesLab4.Core.Numbers;

namespace GamesLab4.Core.Combat;

public abstract class CEnemy : IEnemy
{
    public string Name { get; }
    public string IconName { get; }
    public BigNumber BaseLife { get; }
    public BigNumber CurrentLife { get; private set; }
    public BigNumber Gold { get; }
    public bool IsDead => CurrentLife == BigNumber.Zero;
    public abstract string TypeLabel { get; }
    public virtual string Details => "Получает полный урон.";

    protected CEnemy(string name, BigNumber baseLife, BigNumber gold, string iconName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconName);
        ArgumentNullException.ThrowIfNull(baseLife);
        ArgumentNullException.ThrowIfNull(gold);
        if (baseLife <= BigNumber.Zero) throw new ArgumentException("Здоровье должно быть больше нуля.", nameof(baseLife));
        Name = name; BaseLife = CurrentLife = baseLife; Gold = gold; IconName = iconName;
    }

    public virtual HitResult TakeDamage(BigNumber damage) => ApplyDamage(damage);

    protected bool CanReceive(BigNumber damage)
    {
        ArgumentNullException.ThrowIfNull(damage);
        return !IsDead && damage > BigNumber.Zero;
    }

    protected HitResult ApplyDamage(BigNumber damage)
    {
        if (!CanReceive(damage)) return NoDamage(IsDead ? "Противник уже побеждён." : "Урон равен нулю.");
        BigNumber actual = damage < CurrentLife ? damage : CurrentLife;
        CurrentLife -= actual;
        return new HitResult(actual, BigNumber.Zero, IsDead ? Gold : BigNumber.Zero, IsDead,
            IsDead ? $"Победа над «{Name}»: +{Gold} золота." : $"Нанесено урона: {actual}.");
    }

    protected HitResult Heal(BigNumber amount)
    {
        BigNumber previous = CurrentLife;
        BigNumber after = CurrentLife + amount;
        CurrentLife = after > BaseLife ? BaseLife : after;
        BigNumber actual = CurrentLife - previous;
        return new HitResult(BigNumber.Zero, actual, BigNumber.Zero, false,
            actual == BigNumber.Zero ? "Противник уклонился лечением: здоровье уже полное." : $"Вместо урона противник восстановил {actual} здоровья.");
    }

    protected static HitResult NoDamage(string message) =>
        new(BigNumber.Zero, BigNumber.Zero, BigNumber.Zero, false, message);
}
