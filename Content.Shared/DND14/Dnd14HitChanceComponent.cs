using Robust.Shared.GameStates;

namespace Content.Shared.DND14;

/// <summary>
/// Adds a flat hit chance modifier to weapons, ammo, projectiles, mobs, or other DND14 entities.
/// Value is expressed as a fraction, so 0.05 means +5% hit chance.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class Dnd14HitChanceComponent : Component
{
    [DataField]
    public float Modifier;
}
