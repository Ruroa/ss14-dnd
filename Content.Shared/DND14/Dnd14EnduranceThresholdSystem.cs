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
        SubscribeLocalEvent<Dnd14CharacterSheetComponent, MapInitEvent>(OnSheetMapInit);
        SubscribeLocalEvent<Dnd14CharacterSheetComponent, MobThresholdChecked>(OnMobThresholdChecked);
    }

    private void OnSheetStartup(EntityUid uid, Dnd14CharacterSheetComponent component, ComponentStartup args)
    {
        ApplyEnduranceThresholds(uid, component);
    }

    private void OnSheetMapInit(EntityUid uid, Dnd14CharacterSheetComponent component, MapInitEvent args)
    {
        ApplyEnduranceThresholds(uid, component);
    }

    private void OnMobThresholdChecked(EntityUid uid, Dnd14CharacterSheetComponent component, ref MobThresholdChecked args)
    {
        ApplyEnduranceThresholds(uid, component, args.Threshold, false);
    }

    public void ApplyEnduranceThresholds(
        EntityUid uid,
        Dnd14CharacterSheetComponent? sheet = null,
        MobThresholdsComponent? thresholds = null,
        bool verifyThresholds = true)
    {
        if (!Resolve(uid, ref sheet, ref thresholds, false))
            return;

        var enduranceModifier = Dnd14CharacterSheetComponent.Modifier(sheet.Endurance);
        if (enduranceModifier < 0)
            enduranceModifier = 0;

        var bonus = enduranceModifier * ThresholdBonusPerEnduranceModifier;
        var crit = FixedPoint2.New(BaseCriticalThreshold + bonus);
        var death = FixedPoint2.New(BaseDeathThreshold + bonus);

        _thresholds.SetMobStateThreshold(uid, crit, MobState.Critical, thresholds);
        _thresholds.SetMobStateThreshold(uid, death, MobState.Dead, thresholds);

        if (verifyThresholds)
            _thresholds.VerifyThresholds(uid, thresholds);
    }
}
