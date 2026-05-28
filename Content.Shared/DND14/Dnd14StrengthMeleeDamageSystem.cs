using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.DND14;

/// <summary>
/// Applies DND14 Strength modifier to melee and unarmed damage.
/// Each +1 Strength modifier adds +5 damage.
/// Unarmed/fist attacks receive the bonus as Blunt.
/// Melee weapons receive the bonus on top of their main existing damage type.
/// </summary>
public sealed class Dnd14StrengthMeleeDamageSystem : EntitySystem
{
    private const string BluntDamageType = "Blunt";
    private const int DamagePerStrengthModifier = 5;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MeleeWeaponComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
    }

    private void OnGetMeleeDamage(EntityUid uid, MeleeWeaponComponent component, ref GetMeleeDamageEvent args)
    {
        if (!TryComp<Dnd14CharacterSheetComponent>(args.User, out var sheet))
            return;

        var modifier = Dnd14CharacterSheetComponent.Modifier(sheet.Strength);
        if (modifier <= 0)
            return;

        var bonus = FixedPoint2.New(modifier * DamagePerStrengthModifier);

        if (uid == args.User)
        {
            AddDamage(args.Damage, BluntDamageType, bonus);
            return;
        }

        AddDamage(args.Damage, GetMainDamageType(args.Damage), bonus);
    }

    private static string GetMainDamageType(DamageSpecifier damage)
    {
        var bestType = BluntDamageType;
        var bestValue = FixedPoint2.Zero;

        foreach (var (type, value) in damage.DamageDict)
        {
            if (value <= bestValue)
                continue;

            bestType = type;
            bestValue = value;
        }

        return bestType;
    }

    private static void AddDamage(DamageSpecifier damage, string damageType, FixedPoint2 amount)
    {
        if (damage.DamageDict.TryGetValue(damageType, out var existing))
            damage.DamageDict[damageType] = existing + amount;
        else
            damage.DamageDict[damageType] = amount;
    }
}
