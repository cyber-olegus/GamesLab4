using GamesLab4.Core.Numbers;

namespace GamesLab4.Core.Combat;

public sealed record HitResult(BigNumber Damage, BigNumber Healing, BigNumber GoldReward, bool DefeatedNow, string Message);

/// <summary>Кликер атакует противника через контракт и не проверяет конкретный тип.</summary>
public interface IEnemy
{
    string Name { get; }
    string IconName { get; }
    BigNumber BaseLife { get; }
    BigNumber CurrentLife { get; }
    BigNumber Gold { get; }
    bool IsDead { get; }
    string TypeLabel { get; }
    string Details { get; }
    HitResult TakeDamage(BigNumber damage);
}
