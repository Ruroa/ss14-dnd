using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Server.Effects;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DND14;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared._Starlight.Camera; // Starlight | ES Screenshake

namespace Content.Server.Projectiles;

public sealed class ProjectileSystem : SharedProjectileSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ColorFlashEffectSystem _color = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly DestructibleSystem _destructibleSystem = default!;
    [Dependency] private readonly GunSystem _guns = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ScreenshakeSystem _shake = default!; // Starlight | ES Screenshake

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(EntityUid uid, ProjectileComponent component, ref StartCollideEvent args)
    {
        // This is so entities that shouldn't get a collision are ignored.
        if (args.OurFixtureId != ProjectileFixture || !args.OtherFixture.Hard
            || component.ProjectileSpent || component is { Weapon: null, OnlyCollideWhenShot: true })
            return;

        var target = args.OtherEntity;
        // it's here so this check is only done once before possible hit
        var attemptEv = new ProjectileReflectAttemptEvent(uid, component, false);
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            SetShooter(uid, component, target);
            return;
        }

        if (TryDodgeProjectile(uid, component, target, out var dodgeChance))
        {
            component.ProjectileSpent = true;
            var shooterName = component.Shooter == null ? "unknown" : ToPrettyString(component.Shooter.Value);
            _popup.PopupEntity("(missed)", target, PopupType.SmallCaution);

            _adminLogger.Add(LogType.BulletHit,
                LogImpact.Low,
                $"Projectile {ToPrettyString(uid):projectile} shot by {shooterName:user} missed {ToPrettyString(target):target} due to DND14 dodge ({dodgeChance:P0})");

            if (component.DeleteOnCollide)
                QueueDel(uid);

            return;
        }

        var ev = new ProjectileHitEvent(component.Damage * _damageableSystem.UniversalProjectileDamageModifier, target, component.Shooter);
        RaiseLocalEvent(uid, ref ev);

        var otherName = ToPrettyString(target);
        var damageRequired = _destructibleSystem.DestroyedAt(target);
        if (TryComp<DamageableComponent>(target, out var damageableComponent))
        {
            damageRequired -= damageableComponent.TotalDamage;
            damageRequired = FixedPoint2.Max(damageRequired, FixedPoint2.Zero);
        }
        var deleted = Deleted(target);

        if (_damageableSystem.TryChangeDamage((target, damageableComponent), ev.Damage, out var damage, component.IgnoreResistances, origin: component.Shooter) && Exists(component.Shooter))
        {
            if (!deleted)
            {
                _color.RaiseEffect(Color.Red, new List<EntityUid> { target }, Filter.Pvs(target, entityManager: EntityManager));
            }

            _adminLogger.Add(LogType.BulletHit,
                LogImpact.Medium,
                $"Projectile {ToPrettyString(uid):projectile} shot by {ToPrettyString(component.Shooter!.Value):user} hit {otherName:target} and dealt {damage:damage} damage");

            component.ProjectileSpent = !TryPenetrate((uid, component), damage, damageRequired);
        }
        else
        {
            component.ProjectileSpent = true;
        }

        if (!deleted)
        {
            _guns.PlayImpactSound(target, damage, component.SoundHit, component.ForceSound);

            //Starlight begin | ES Screenshake
            var shakeParams = new ScreenshakeParameters
            {
                Trauma = 0.45f, 
                DecayRate = 1.1f,
                Frequency = 0.04f,
            };
            if (!args.OurBody.LinearVelocity.IsLengthZero())
                _sharedCameraRecoil.KickCamera(target, args.OurBody.LinearVelocity.Normalized() * 0.08f);
            _shake.Screenshake(target, shakeParams, null);
            //Starlight end
        }

        if (component.DeleteOnCollide && component.ProjectileSpent)
            QueueDel(uid);

        if (component.ImpactEffect != null && TryComp(uid, out TransformComponent? xform))
        {
            RaiseNetworkEvent(new ImpactEffectEvent(component.ImpactEffect, GetNetCoordinates(xform.Coordinates)), Filter.Pvs(xform.Coordinates, entityMan: EntityManager));
        }
    }

    private bool TryDodgeProjectile(EntityUid projectileUid, ProjectileComponent projectile, EntityUid target, out float finalDodgeChance)
    {
        var dodgeChance = GetDodgeChance(target);
        var hitChance = GetHitChance(projectileUid, projectile);

        finalDodgeChance = Math.Clamp(dodgeChance - hitChance, 0f, 1f);
        return finalDodgeChance > 0f && _random.Prob(finalDodgeChance);
    }

    private float GetHitChance(EntityUid projectileUid, ProjectileComponent projectile)
    {
        var hitChance = GetHitChanceModifier(projectileUid);

        if (projectile.Shooter != null)
        {
            if (TryComp<Dnd14CharacterSheetComponent>(projectile.Shooter.Value, out var shooterSheet))
                hitChance += Dnd14AgilitySystem.GetHitChanceBonus(shooterSheet);

            hitChance += GetHitChanceModifier(projectile.Shooter.Value);
        }

        if (projectile.Weapon != null)
            hitChance += GetHitChanceModifier(projectile.Weapon.Value);

        return hitChance;
    }

    private float GetDodgeChance(EntityUid target)
    {
        var dodgeChance = GetDodgeChanceModifier(target);

        if (TryComp<Dnd14CharacterSheetComponent>(target, out var targetSheet))
            dodgeChance += Dnd14AgilitySystem.GetDodgeChanceBonus(targetSheet);

        return dodgeChance;
    }

    private float GetHitChanceModifier(EntityUid uid)
    {
        return TryComp<Dnd14HitChanceComponent>(uid, out var hitChance)
            ? hitChance.Modifier
            : 0f;
    }

    private float GetDodgeChanceModifier(EntityUid uid)
    {
        return TryComp<Dnd14DodgeChanceComponent>(uid, out var dodgeChance)
            ? dodgeChance.Modifier
            : 0f;
    }

    private bool TryPenetrate(Entity<ProjectileComponent> projectile, DamageSpecifier damage, FixedPoint2 damageRequired)
    {
        // If penetration is to be considered, we need to do some checks to see if the projectile should stop.
        if (projectile.Comp.PenetrationThreshold == 0)
            return false;

        // If a damage type is required, stop the bullet if the hit entity doesn't have that type.
        if (projectile.Comp.PenetrationDamageTypeRequirement != null)
        {
            foreach (var requiredDamageType in projectile.Comp.PenetrationDamageTypeRequirement)
            {
                if (damage.DamageDict.Keys.Contains(requiredDamageType))
                    continue;

                return false;
            }
        }

        // If the object won't be destroyed, it "tanks" the penetration hit.
        if (damage.GetTotal() < damageRequired)
        {
            return false;
        }

        if (!projectile.Comp.ProjectileSpent)
        {
            projectile.Comp.PenetrationAmount += damageRequired;
            // The projectile has dealt enough damage to be spent.
            if (projectile.Comp.PenetrationAmount >= projectile.Comp.PenetrationThreshold)
            {
                return false;
            }
        }

        return true;
    }
}
