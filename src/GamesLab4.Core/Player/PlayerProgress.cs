using GamesLab4.Core.Numbers;

namespace GamesLab4.Core.Player;

/// <summary>Данные сохранения. Живые объекты боя и WPF в файл не попадают.</summary>
public sealed record PlayerProgress(BigNumber Gold, BigNumber Damage, int Level, BigNumber UpgradeCost,
    int Victories, double DamageModifier, double UpgradeModifier);
