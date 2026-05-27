namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    private static readonly Dictionary<string, Dnd14CharacterSheet> Dnd14SheetCache = new();

    private Dnd14CharacterSheet _dnd14Sheet = Dnd14CharacterSheet.Default();

    private string Dnd14SheetCacheKey => $"{Name}|{Species}|{Age}|{Sex}|{Gender}";

    [DataField("dnd14Sheet")]
    public Dnd14CharacterSheet Dnd14Sheet
    {
        get
        {
            if (IsNonDefaultDnd14Sheet(_dnd14Sheet))
                return _dnd14Sheet;

            if (Dnd14SheetCache.TryGetValue(Dnd14SheetCacheKey, out var cached))
                return cached;

            return _dnd14Sheet;
        }
        set
        {
            _dnd14Sheet = value?.Clone() ?? Dnd14CharacterSheet.Default();
            _dnd14Sheet.EnsureValid();

            if (IsNonDefaultDnd14Sheet(_dnd14Sheet))
                Dnd14SheetCache[Dnd14SheetCacheKey] = _dnd14Sheet;
        }
    }

    private static bool IsNonDefaultDnd14Sheet(Dnd14CharacterSheet sheet)
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
