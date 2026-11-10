using GamesLab4.Core.Numbers;

namespace GamesLab4.Core.Player;

public sealed class CPlayer
{
    public BigNumber Gold { get; private set; } = BigNumber.Zero;
    public BigNumber Damage { get; private set; } = new(10);
    public BigNumber UpgradeCost { get; private set; } = new(20);
    public int Level { get; private set; } = 1;
    public double DamageModifier { get; private set; } = 1.2;
    public double UpgradeModifier { get; private set; } = 1.2;
    public bool CanUpgrade => Gold >= UpgradeCost && Level < int.MaxValue;

    public void AddGold(BigNumber reward)
    {
        ArgumentNullException.ThrowIfNull(reward);
        Gold += reward;
    }
    public BigNumber DealDamage() => Damage;

    public bool TryUpgrade()
    {
        if (!CanUpgrade) return false;
        BigNumber newDamage = Damage * DamageModifier;
        BigNumber newCost = (UpgradeCost * Level) * UpgradeModifier;
        Gold -= UpgradeCost;
        Damage = newDamage > Damage ? newDamage : Damage + BigNumber.One;
        UpgradeCost = newCost > UpgradeCost ? newCost : UpgradeCost + BigNumber.One;
        Level++;
        return true;
    }

    public PlayerProgress Snapshot(int victories) =>
        new(Gold, Damage, Level, UpgradeCost, victories, DamageModifier, UpgradeModifier);

    public static CPlayer Restore(PlayerProgress progress)
    {
        Validate(progress);
        return new CPlayer
        {
            Gold = progress.Gold, Damage = progress.Damage, Level = progress.Level,
            UpgradeCost = progress.UpgradeCost, DamageModifier = progress.DamageModifier,
            UpgradeModifier = progress.UpgradeModifier
        };
    }

    internal static void Validate(PlayerProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(progress.Gold);
        ArgumentNullException.ThrowIfNull(progress.Damage);
        ArgumentNullException.ThrowIfNull(progress.UpgradeCost);
        if (progress.Damage <= BigNumber.Zero || progress.UpgradeCost <= BigNumber.Zero ||
            progress.Level < 1 || progress.Victories < 0 ||
            !double.IsFinite(progress.DamageModifier) || progress.DamageModifier <= 1 ||
            !double.IsFinite(progress.UpgradeModifier) || progress.UpgradeModifier <= 1)
            throw new ArgumentException("Сохранение содержит недопустимые характеристики игрока.");
    }
}
