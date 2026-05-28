using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DND14;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server.DND14;

/// <summary>
/// Resolves DND14 hit-vs-dodge from the common damage path.
/// This intentionally runs after damage is applied and reverses positive damage on a successful dodge,
/// which makes it cover projectile, hitscan, and other attack implementations that do not share one hit event.
/// </summary>
public sealed class Dnd14CombatResolutionSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly HashSet<EntityUid> _revertingDamage = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Dnd14CharacterSheetComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(EntityUid uid, Dnd14CharacterSheetComponent sheet, DamageChangedEvent args)
    {
        if (_revertingDamage.Contains(uid))
            return;

        if (args.DamageDelta == null)
            return;

        if (args.Origin == null)
            return;

        var totalDamage = args.DamageDelta.GetTotal();
        if (totalDamage <= FixedPoint2.Zero)
            return;

        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return;

        var finalDodgeChance = GetDodgeChance(uid, sheet) - GetHitChance(args.Origin.Value);
        finalDodgeChance = Math.Clamp(finalDodgeChance, 0f, 1f);

        if (finalDodgeChance <= 0f || !_random.Prob(finalDodgeChance))
            return;

        var undoDamage = args.DamageDelta * -1;
        _revertingDamage.Add(uid);
        try
        {
            _damageable.TryChangeDamage((uid, damageable), undoDamage, out _, true, interruptsDoAfters: false);
        }
        finally
        {
            _revertingDamage.Remove(uid);
        }

        _popup.PopupEntity("(missed)", uid, PopupType.SmallCaution);
    }

    private float GetDodgeChance(EntityUid target, Dnd14CharacterSheetComponent sheet)
    {
        var dodgeChance = Dnd14AgilitySystem.GetDodgeChanceBonus(sheet);

        if (TryComp<Dnd14DodgeChanceComponent>(target, out var dodgeModifier))
            dodgeChance += dodgeModifier.Modifier;

        return dodgeChance;
    }

    private float GetHitChance(EntityUid attacker)
    {
        var hitChance = 0f;

        if (TryComp<Dnd14CharacterSheetComponent>(attacker, out var sheet))
            hitChance += Dnd14AgilitySystem.GetHitChanceBonus(sheet);

        if (TryComp<Dnd14HitChanceComponent>(attacker, out var hitModifier))
            hitChance += hitModifier.Modifier;

        return hitChance;
    }
}
