using Content.Server.Spawners.EntitySystems;
using Content.Shared.DND14;
using Robust.Shared.Prototypes;

namespace Content.Server.DND14;

/// <summary>
/// Copies character-creator DND14 sheets onto spawned player mobs.
/// NPCs and creatures can also define Dnd14CharacterSheetComponent directly in YAML.
/// </summary>
public sealed class Dnd14CharacterSheetSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, after: [typeof(SpawnPointSystem)]);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult == null || args.HumanoidCharacterProfile == null)
            return;

        var uid = args.SpawnResult.Value;
        var sheet = args.HumanoidCharacterProfile.Dnd14Sheet.Clone();
        sheet.EnsureValid();

        var comp = EnsureComp<Dnd14CharacterSheetComponent>(uid);
        comp.LoadFromSheet(sheet);
        Dirty(uid, comp);
    }
}
