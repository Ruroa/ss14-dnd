using Content.Shared.Damage.Systems;
using Content.Shared.DND14;
using Content.Shared.Popups;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanBasicDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicDamageComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanBasicDamageComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        if (TryDnd14Dodge(ent.Owner, args.Data.HitEntity.Value, args.Data.Shooter, args.Data.Gun,
                out var finalDodgeChance, out var hitChance, out var dodgeChance))
        {
            Log.Info($"DND14 hitscan miss: shot={ToPrettyString(ent.Owner)} target={ToPrettyString(args.Data.HitEntity.Value)} hit={hitChance:P0} dodge={dodgeChance:P0} finalDodge={finalDodgeChance:P0}");
            _popup.PopupEntity("(missed)", args.Data.HitEntity.Value, PopupType.SmallCaution);
            return;
        }

        var dmg = ent.Comp.Damage * _damage.UniversalHitscanDamageModifier;

        // var damageDealt = _damage.TryChangeDamage(args.Data.HitEntity.Value, dmg, origin: args.Data.Gun); // Starlight - we redefine this
        // Starlight start
        var damageDealt = _damage.ChangeDamage(
                args.Data.HitEntity.Value,
                dmg,
                ignoreResistances: ent.Comp.IgnoreResistances,
                origin: args.Data.Gun,
                armorPenetration: ent.Comp.ArmorPenetration,
                canHeal: false
            );
        // Starlight end

        if (damageDealt == null)
            return;

        var damageEvent = new HitscanDamageDealtEvent
        {
            Target = args.Data.HitEntity.Value,
            DamageDealt = damageDealt,
            Data = args.Data, // Starlight
        };

        RaiseLocalEvent(ent, ref damageEvent);
    }

    private bool TryDnd14Dodge(EntityUid shotUid, EntityUid target, EntityUid? shooter, EntityUid gun,
        out float finalDodgeChance, out float hitChance, out float dodgeChance)
    {
        dodgeChance = GetDnd14DodgeChance(target);
        hitChance = GetDnd14HitscanHitChance(shotUid, shooter, gun);
        finalDodgeChance = Math.Clamp(dodgeChance - hitChance, 0f, 1f);

        Log.Info($"DND14 hitscan check: shot={ToPrettyString(shotUid)} target={ToPrettyString(target)} hit={hitChance:P0} dodge={dodgeChance:P0} finalDodge={finalDodgeChance:P0}");
        return finalDodgeChance > 0f && _random.Prob(finalDodgeChance);
    }

    private float GetDnd14HitscanHitChance(EntityUid shotUid, EntityUid? shooter, EntityUid gun)
    {
        var hitChance = GetDnd14StoredHitChance(shotUid);

        if (shooter != null)
            hitChance += GetDnd14HitChanceFromEntity(shooter.Value);

        hitChance += GetDnd14HitChanceModifier(gun);

        return hitChance;
    }

    private float GetDnd14StoredHitChance(EntityUid uid)
    {
        if (TryComp<Dnd14ProjectileHitChanceComponent>(uid, out var projectileHitChance))
            return projectileHitChance.HitChance;

        return GetDnd14HitChanceModifier(uid);
    }

    private float GetDnd14HitChanceFromEntity(EntityUid uid)
    {
        var hitChance = GetDnd14HitChanceModifier(uid);

        if (TryComp<Dnd14CharacterSheetComponent>(uid, out var sheet))
            hitChance += Dnd14AgilitySystem.GetHitChanceBonus(sheet);

        return hitChance;
    }

    private float GetDnd14DodgeChance(EntityUid target)
    {
        var dodgeChance = GetDnd14DodgeChanceModifier(target);

        if (TryComp<Dnd14CharacterSheetComponent>(target, out var targetSheet))
            dodgeChance += Dnd14AgilitySystem.GetDodgeChanceBonus(targetSheet);

        return dodgeChance;
    }

    private float GetDnd14HitChanceModifier(EntityUid uid)
    {
        return TryComp<Dnd14HitChanceComponent>(uid, out var hitChance)
            ? hitChance.Modifier
            : 0f;
    }

    private float GetDnd14DodgeChanceModifier(EntityUid uid)
    {
        return TryComp<Dnd14DodgeChanceComponent>(uid, out var dodgeChance)
            ? dodgeChance.Modifier
            : 0f;
    }
}
