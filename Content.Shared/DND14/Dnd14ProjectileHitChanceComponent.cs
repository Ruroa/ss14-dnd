using Robust.Shared.GameStates;

namespace Content.Shared.DND14;

/// <summary>
/// Stores the final DND14 hit chance carried by a fired projectile.
/// Value is expressed as a fraction, so 0.20 means 20% hit chance.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class Dnd14ProjectileHitChanceComponent : Component
{
    [DataField]
    public float HitChance;
}
