using System.Linq;
using System.Text;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    private const string Dnd14StartMarker = "[[DND14_SHEET:";
    private const string Dnd14EndMarker = "]]";

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

            if (TryReadPackedDnd14Sheet(Secrets, out var packed))
            {
                _dnd14Sheet = packed;
                _dnd14Sheet.EnsureValid();
                Dnd14SheetCache[Dnd14SheetCacheKey] = _dnd14Sheet;
                return _dnd14Sheet;
            }

            if (Dnd14SheetCache.TryGetValue(Dnd14SheetCacheKey, out var cached))
                return cached;

            return _dnd14Sheet;
        }
        set
        {
            _dnd14Sheet = value?.Clone() ?? Dnd14CharacterSheet.Default();
            _dnd14Sheet.EnsureValid();

            if (IsNonDefaultDnd14Sheet(_dnd14Sheet))
            {
                Dnd14SheetCache[Dnd14SheetCacheKey] = _dnd14Sheet;
                Secrets = WritePackedDnd14Sheet(Secrets, _dnd14Sheet);
            }
        }
    }

    private static bool IsNonDefaultDnd14Sheet(Dnd14CharacterSheet sheet)
    {
        return sheet.Finalized
               || sheet.Level != 1
               || sheet.Experience != 0
               || sheet.ClassId != Dnd14CharacterSheet.DefaultClassId
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

    private static string WritePackedDnd14Sheet(string text, Dnd14CharacterSheet sheet)
    {
        text = RemovePackedDnd14Sheet(text ?? string.Empty).TrimEnd();
        var packed = Convert.ToBase64String(Encoding.UTF8.GetBytes(PackDnd14Sheet(sheet)));

        return string.IsNullOrWhiteSpace(text)
            ? $"{Dnd14StartMarker}{packed}{Dnd14EndMarker}"
            : $"{text}\n{Dnd14StartMarker}{packed}{Dnd14EndMarker}";
    }

    private static string RemovePackedDnd14Sheet(string text)
    {
        var start = text.IndexOf(Dnd14StartMarker, StringComparison.Ordinal);
        if (start < 0)
            return text;

        var end = text.IndexOf(Dnd14EndMarker, start, StringComparison.Ordinal);
        if (end < 0)
            return text[..start];

        end += Dnd14EndMarker.Length;
        return (text[..start] + text[end..]).TrimEnd();
    }

    private static bool TryReadPackedDnd14Sheet(string text, out Dnd14CharacterSheet sheet)
    {
        sheet = Dnd14CharacterSheet.Default();

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var start = text.IndexOf(Dnd14StartMarker, StringComparison.Ordinal);
        if (start < 0)
            return false;

        start += Dnd14StartMarker.Length;
        var end = text.IndexOf(Dnd14EndMarker, start, StringComparison.Ordinal);
        if (end < 0)
            return false;

        try
        {
            var packed = text[start..end];
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(packed));
            sheet = UnpackDnd14Sheet(payload);
            sheet.EnsureValid();
            return true;
        }
        catch
        {
            sheet = Dnd14CharacterSheet.Default();
            return false;
        }
    }

    private static string PackText(string text)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(text ?? string.Empty));
    }

    private static string UnpackText(string text)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(text));
    }

    private static string PackDnd14Sheet(Dnd14CharacterSheet sheet)
    {
        sheet.EnsureValid();

        var builder = new StringBuilder();
        AppendPackedPart(builder, sheet.Finalized ? "1" : "0");
        AppendPackedPart(builder, sheet.Level.ToString());
        AppendPackedPart(builder, sheet.Experience.ToString());
        AppendPackedPart(builder, sheet.ClassId);
        AppendPackedPart(builder, PackText(sheet.Background));
        AppendPackedPart(builder, PackText(sheet.Notes));
        AppendPackedPart(builder, sheet.Strength.ToString());
        AppendPackedPart(builder, sheet.Agility.ToString());
        AppendPackedPart(builder, sheet.Endurance.ToString());
        AppendPackedPart(builder, sheet.Intelligence.ToString());
        AppendPackedPart(builder, sheet.Wisdom.ToString());
        AppendPackedPart(builder, sheet.Social.ToString());
        AppendPackedPart(builder, string.Join(',', sheet.TrainedSkills));
        AppendPackedPart(builder, string.Join(',', sheet.MasteredSkills));
        return builder.ToString();
    }

    private static void AppendPackedPart(StringBuilder builder, string value)
    {
        if (builder.Length > 0)
            builder.Append('|');

        builder.Append(value);
    }

    private static Dnd14CharacterSheet UnpackDnd14Sheet(string payload)
    {
        var parts = payload.Split('|');
        if (parts.Length < 13)
            return Dnd14CharacterSheet.Default();

        var offset = parts.Length >= 14 ? 1 : 0;
        var sheet = new Dnd14CharacterSheet
        {
            Finalized = parts[0] == "1",
            Level = int.TryParse(parts[1], out var level) ? level : 1,
            Experience = int.TryParse(parts[2], out var experience) ? experience : 0,
            ClassId = offset == 1 ? parts[3] : Dnd14CharacterSheet.DefaultClassId,
            Background = UnpackText(parts[3 + offset]),
            Notes = UnpackText(parts[4 + offset]),
            Strength = int.TryParse(parts[5 + offset], out var strength) ? strength : Dnd14CharacterSheet.StartingStat,
            Agility = int.TryParse(parts[6 + offset], out var agility) ? agility : Dnd14CharacterSheet.StartingStat,
            Endurance = int.TryParse(parts[7 + offset], out var endurance) ? endurance : Dnd14CharacterSheet.StartingStat,
            Intelligence = int.TryParse(parts[8 + offset], out var intelligence) ? intelligence : Dnd14CharacterSheet.StartingStat,
            Wisdom = int.TryParse(parts[9 + offset], out var wisdom) ? wisdom : Dnd14CharacterSheet.StartingStat,
            Social = int.TryParse(parts[10 + offset], out var social) ? social : Dnd14CharacterSheet.StartingStat,
            TrainedSkills = parts[11 + offset]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(),
            MasteredSkills = parts[12 + offset]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(),
        };

        sheet.EnsureValid();
        return sheet;
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
