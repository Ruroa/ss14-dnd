using Robust.Shared.Serialization;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    private static readonly Dictionary<string, Dnd14CharacterSheet> Dnd14SheetCache = new();

    private Dnd14CharacterSheet _dnd14Sheet = Dnd14CharacterSheet.Default();

    private string Dnd14SheetCacheKey => $"{Name}|{Species}|{Age}";

    [DataField("dnd14Sheet")]
    public Dnd14CharacterSheet Dnd14Sheet
    {
        get
        {
            if (Dnd14SheetCache.TryGetValue(Dnd14SheetCacheKey, out var cached))
                return cached;

            return _dnd14Sheet;
        }
        set
        {
            _dnd14Sheet = value?.Clone() ?? Dnd14CharacterSheet.Default();
            _dnd14Sheet.EnsureValid();
            Dnd14SheetCache[Dnd14SheetCacheKey] = _dnd14Sheet;
        }
    }

    public HumanoidCharacterProfile WithDnd14Sheet(Dnd14CharacterSheet sheet)
    {
        var profile = new HumanoidCharacterProfile(this);
        profile.Dnd14Sheet = sheet;
        return profile;
    }

    public HumanoidCharacterProfile WithUnlockedDnd14Sheet()
    {
        var profile = new HumanoidCharacterProfile(this);
        profile.Dnd14Sheet = Dnd14Sheet.UnlockedClone();
        return profile;
    }
}
