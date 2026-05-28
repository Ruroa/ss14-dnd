using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.DND14;

/// <summary>
/// Applies DND14 Endurance modifier to humanoid crit/death thresholds.
/// Each +1 Endurance modifier adds +20 to both crit and death thresholds.
/// </summary>
public sealed class Dnd14EnduranceThresholdSystem : EntitySystem
{
    private const int BaseCriticalThreshold = 100;
    private const int BaseDeathThreshold = 200;
    private const int ThresholdBonusPerEnduranceModifier = 20;

    [Dependency] private readonly MobThresholdSystem _thresholds = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Dnd14CharacterSheetComponent, ComponentStartup>(OnSheetStartup);
        SubscribeLocalEvent<MobThresholdsComponent, ComponentStartup>(OnThresholdStartup);
    }

    private void OnSheetStartup(EntityUid uid, Dnd14CharacterSheetComponent component, ComponentStartup args)
    {
        ApplyEnduranceThresholds(uid, component);
    }

    private void OnThresholdStartup(EntityUid uid, MobThresholdsComponent component, ComponentStartup args)
    {
        if (!TryComp<Dnd14CharacterSheetComponent>(uid, out var sheet))
            return;

        ApplyEnduranceThresholds(uid, sheet, component);
    }

    public void ApplyEnduranceThresholds(EntityUid uid, Dnd14CharacterSheetComponent? sheet = null, MobThresholdsComponent? thresholds = null)
    {
        if (!Resolve(uid, ref sheet, ref thresholds, false))
            return;

        var enduranceModifier = Dnd14CharacterSheetComponent.Modifier(sheet.Endurance);
        if (enduranceModifier < 0)
            enduranceModifier = 0;

        var bonus = enduranceModifier * ThresholdBonusPerEnduranceModifier;
        var crit = FixedPoint2.New(BaseCriticalThreshold + bonus);
        var death = FixedPoint2.New(BaseDeathThreshold + bonus);

        SetThreshold(thresholds, MobState.Critical, crit);
        SetThreshold(thresholds, MobState.Dead, death);
        Dirty(uid, thresholds);
        _thresholds.VerifyThresholds(uid, thresholds);
    }

    private static void SetThreshold(MobThresholdsComponent thresholds, MobState state, FixedPoint2 value)
    {
        FixedPoint2? existingKey = null;

        foreach (var (threshold, thresholdState) in thresholds.Thresholds)
        {
            if (thresholdState != state)
                continue;

            existingKey = threshold;
            break;
        }

        if (existingKey != null)
            thresholds.Thresholds.Remove(existingKey.Value);

        thresholds.Thresholds[value] = state;
    }
}
