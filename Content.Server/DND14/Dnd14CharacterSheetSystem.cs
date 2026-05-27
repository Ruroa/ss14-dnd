using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.DND14;
using Content.Shared.Humanoid;

namespace Content.Server.DND14;

/// <summary>
/// Copies character-creator DND14 sheets onto spawned player mobs.
/// Also gives directly spawned humanoids a default sheet component so dev/admin spawned characters can be inspected and used.
/// NPCs and creatures can also define Dnd14CharacterSheetComponent directly in YAML.
/// </summary>
public sealed class Dnd14CharacterSheetSystem : EntitySystem
{
    private float _fallbackTimer;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, after: [typeof(SpawnPointSystem)]);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _fallbackTimer -= frameTime;
        if (_fallbackTimer > 0f)
            return;

        _fallbackTimer = 1f;
        EnsureDefaultSheetsOnHumanoids();
    }

    private void EnsureDefaultSheetsOnHumanoids()
    {
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (HasComp<Dnd14CharacterSheetComponent>(uid))
                continue;

            var comp = EnsureComp<Dnd14CharacterSheetComponent>(uid);
            Dirty(uid, comp);
        }
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
