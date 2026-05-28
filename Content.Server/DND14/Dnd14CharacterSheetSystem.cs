using System.Linq;
using Content.Server.Preferences.Managers;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.DND14;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server.DND14;

/// <summary>
/// Copies character-creator DND14 sheets onto spawned player mobs.
/// Also gives directly spawned humanoids a default sheet component so dev/admin spawned characters can be inspected and used.
/// NPCs and creatures can also define Dnd14CharacterSheetComponent directly in YAML.
/// </summary>
public sealed class Dnd14CharacterSheetSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;
    [Dependency] private readonly Dnd14EnduranceThresholdSystem _endurance = default!;

    private float _fallbackTimer;

    public override void Initialize()
    {
        base.Initialize();
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
        EnsureSheetsOnAttachedPlayerMobs();
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
            _endurance.ApplyEnduranceThresholds(uid, comp);
        }
    }

    private void EnsureSheetsOnAttachedPlayerMobs()
    {
        foreach (var session in _playerManager.Sessions)
        {
            var attached = session.AttachedEntity;
            if (attached == null || !Exists(attached.Value))
                continue;

            var comp = EnsureComp<Dnd14CharacterSheetComponent>(attached.Value);
            if (!IsDefaultSheet(comp))
            {
                _endurance.ApplyEnduranceThresholds(attached.Value, comp);
                continue;
            }

            if (!TryGetPreferredSheet(session, out var sheet))
                continue;

            comp.LoadFromSheet(sheet);
            Dirty(attached.Value, comp);
            _endurance.ApplyEnduranceThresholds(attached.Value, comp);
        }
    }

    private bool TryGetPreferredSheet(ICommonSession session, out Dnd14CharacterSheet sheet)
    {
        sheet = Dnd14CharacterSheet.Default();

        var preferences = _preferences.GetPreferencesOrNull(session.UserId);
        if (preferences == null)
            return false;

        foreach (var (_, profile) in preferences.Characters.OrderBy(pair => pair.Key))
        {
            if (profile is not HumanoidCharacterProfile humanoid)
                continue;

            if (!humanoid.Enabled)
                continue;

            var candidate = humanoid.Dnd14Sheet.Clone();
            candidate.EnsureValid();

            if (!IsNonDefaultSheet(candidate))
                continue;

            sheet = candidate;
            return true;
        }

        return false;
    }

    private static bool IsDefaultSheet(Dnd14CharacterSheetComponent sheet)
    {
        return !sheet.Finalized
               && sheet.Level == 1
               && sheet.Experience == 0
               && string.IsNullOrWhiteSpace(sheet.Background)
               && string.IsNullOrWhiteSpace(sheet.Notes)
               && sheet.Strength == Dnd14CharacterSheet.StartingStat
               && sheet.Agility == Dnd14CharacterSheet.StartingStat
               && sheet.Endurance == Dnd14CharacterSheet.StartingStat
               && sheet.Intelligence == Dnd14CharacterSheet.StartingStat
               && sheet.Wisdom == Dnd14CharacterSheet.StartingStat
               && sheet.Social == Dnd14CharacterSheet.StartingStat
               && sheet.TrainedSkills.Count == 0
               && sheet.MasteredSkills.Count == 0;
    }

    private static bool IsNonDefaultSheet(Dnd14CharacterSheet sheet)
    {
        return sheet.Finalized
               || sheet.Level != 1
               || sheet.Experience != 0
               || !string.IsNullOrWhiteSpace(sheet.Background)
               || !string.IsNullOrWhiteSpace(sheet.Notes)
               || sheet.Strength != Dnd14CharacterSheet.StartingStat
               || sheet.Agility != Dnd14CharacterSheet.StartingStat
               || sheet.Endurance != Dnd14CharacterSheet.StartingStat
               || sheet.Intelligence != Dnd14CharacterSheet.StartingStat
               || sheet.Wisdom != Dnd14CharacterSheet.StartingStat
               || sheet.Social != Dnd14CharacterSheet.StartingStat
               || sheet.TrainedSkills.Count != 0
               || sheet.MasteredSkills.Count != 0;
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
        _endurance.ApplyEnduranceThresholds(uid, comp);
    }
}
