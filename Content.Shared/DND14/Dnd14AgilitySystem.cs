using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.DND14;

/// <summary>
/// Applies DND14 Agility modifier effects that can be handled safely from shared code.
/// Each +1 Agility modifier adds +2% movement speed.
/// </summary>
public sealed class Dnd14AgilitySystem : EntitySystem
{
    private const float MovementBonusPerAgilityModifier = 0.02f;

    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Dnd14CharacterSheetComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
    }

    private void OnRefreshMovementSpeed(EntityUid uid, Dnd14CharacterSheetComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        var agilityModifier = GetPositiveAgilityModifier(component);
        if (agilityModifier <= 0)
            return;

        var speedModifier = 1f + agilityModifier * MovementBonusPerAgilityModifier;
        args.ModifySpeed(speedModifier);
    }

    public void RefreshAgilityModifiers(EntityUid uid)
    {
        if (TryComp<MovementSpeedModifierComponent>(uid, out var movement))
            _movement.RefreshMovementSpeedModifiers(uid, movement);
    }

    public static int GetPositiveAgilityModifier(Dnd14CharacterSheetComponent sheet)
    {
        var agilityModifier = Dnd14CharacterSheetComponent.Modifier(sheet.Agility);
        return Math.Max(agilityModifier, 0);
    }

    public static float GetHitChanceBonus(Dnd14CharacterSheetComponent sheet)
    {
        return GetPositiveAgilityModifier(sheet) * 0.05f;
    }

    public static float GetDodgeChanceBonus(Dnd14CharacterSheetComponent sheet)
    {
        return GetPositiveAgilityModifier(sheet) * 0.05f;
    }

    public static float GetHandlingModifier(Dnd14CharacterSheetComponent sheet)
    {
        return Math.Max(0f, 1f - GetPositiveAgilityModifier(sheet) * 0.02f);
    }
}
