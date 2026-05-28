using Robust.Shared.GameStates;

namespace Content.Shared.DND14;

/// <summary>
/// Adds a flat dodge chance modifier to mobs or other DND14 entities.
/// Value is expressed as a fraction, so 0.05 means +5% dodge chance.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class Dnd14DodgeChanceComponent : Component
{
    [DataField]
    public float Modifier;
}
