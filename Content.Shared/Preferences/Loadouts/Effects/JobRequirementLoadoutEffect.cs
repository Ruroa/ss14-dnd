using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Checks for a job requirement to be met such as playtime.
/// </summary>
public sealed partial class JobRequirementLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public JobRequirement Requirement = default!;

    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, ICommonSession? session, IDependencyCollection collection, out FormattedMessage reason) // Starlight: Always return reason
    {
        // SS14 DND: private campaign server. Lobby loadout cosmetics should not be locked behind playtime.
        reason = FormattedMessage.Empty;
        return true;
    }
}
