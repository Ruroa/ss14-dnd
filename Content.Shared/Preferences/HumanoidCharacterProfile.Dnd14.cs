using Robust.Shared.Serialization;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    [DataField("dnd14Sheet")]
    public Dnd14CharacterSheet Dnd14Sheet { get; set; } = Dnd14CharacterSheet.Default();

    public HumanoidCharacterProfile WithDnd14Sheet(Dnd14CharacterSheet sheet)
    {
        var profile = new HumanoidCharacterProfile(this)
        {
            Dnd14Sheet = sheet.Clone(),
        };

        profile.Dnd14Sheet.EnsureValid();
        return profile;
    }

    public HumanoidCharacterProfile WithUnlockedDnd14Sheet()
    {
        var profile = new HumanoidCharacterProfile(this)
        {
            Dnd14Sheet = Dnd14Sheet.UnlockedClone(),
        };

        profile.Dnd14Sheet.EnsureValid();
        return profile;
    }
}
