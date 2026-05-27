using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class Dnd14CharacterSheet
{
    public const int StartingStat = 10;
    public const int CreationMaxStat = 16;
    public const int StartingPoints = 8;
    public const int RequiredTrainedSkills = 4;
    public const int SkillPointBudget = 4;

    public static readonly HashSet<string> ValidSkillIds = new()
    {
        "Perception",
        "Investigation",
        "Persuasion",
        "Deception",
        "Performance",
        "Intimidation",
        "Medicine",
        "Engineering",
        "Technology",
        "Science",
        "Survival",
        "Stealth",
        "SleightOfHand",
    };

    [DataField]
    public bool Finalized { get; set; }

    [DataField]
    public int Level { get; set; } = 1;

    [DataField]
    public int Experience { get; set; }

    [DataField]
    public string Background { get; set; } = string.Empty;

    [DataField]
    public string Notes { get; set; } = string.Empty;

    [DataField]
    public int Strength { get; set; } = StartingStat;

    [DataField]
    public int Agility { get; set; } = StartingStat;

    [DataField]
    public int Endurance { get; set; } = StartingStat;

    [DataField]
    public int Intelligence { get; set; } = StartingStat;

    // Serialized as "perception" for compatibility with older saved sheets.
    [DataField("perception")]
    public int Wisdom { get; set; } = StartingStat;

    [DataField]
    public int Social { get; set; } = StartingStat;

    [DataField]
    public HashSet<string> TrainedSkills { get; set; } = new();

    [DataField]
    public HashSet<string> MasteredSkills { get; set; } = new();

    public static Dnd14CharacterSheet Default() => new();

    public Dnd14CharacterSheet Clone()
    {
        return new Dnd14CharacterSheet
        {
            Finalized = Finalized,
            Level = Level,
            Experience = Experience,
            Background = Background,
            Notes = Notes,
            Strength = Strength,
            Agility = Agility,
            Endurance = Endurance,
            Intelligence = Intelligence,
            Wisdom = Wisdom,
            Social = Social,
            TrainedSkills = new HashSet<string>(TrainedSkills),
            MasteredSkills = new HashSet<string>(MasteredSkills),
        };
    }

    public Dnd14CharacterSheet UnlockedClone()
    {
        var clone = Clone();
        clone.Finalized = false;
        return clone;
    }

    public bool MemberwiseEquals(Dnd14CharacterSheet? other)
    {
        if (other == null)
            return false;

        return Finalized == other.Finalized
               && Level == other.Level
               && Experience == other.Experience
               && Background == other.Background
               && Notes == other.Notes
               && Strength == other.Strength
               && Agility == other.Agility
               && Endurance == other.Endurance
               && Intelligence == other.Intelligence
               && Wisdom == other.Wisdom
               && Social == other.Social
               && TrainedSkills.SetEquals(other.TrainedSkills)
               && MasteredSkills.SetEquals(other.MasteredSkills);
    }

    public void EnsureValid()
    {
        Level = Math.Max(1, Level);
        Experience = Math.Max(0, Experience);
        Background ??= string.Empty;
        Notes ??= string.Empty;
        Strength = Math.Clamp(Strength, StartingStat, CreationMaxStat);
        Agility = Math.Clamp(Agility, StartingStat, CreationMaxStat);
        Endurance = Math.Clamp(Endurance, StartingStat, CreationMaxStat);
        Intelligence = Math.Clamp(Intelligence, StartingStat, CreationMaxStat);
        Wisdom = Math.Clamp(Wisdom, StartingStat, CreationMaxStat);
        Social = Math.Clamp(Social, StartingStat, CreationMaxStat);
        TrainedSkills ??= new HashSet<string>();
        MasteredSkills ??= new HashSet<string>();

        TrainedSkills = TrainedSkills.Where(ValidSkillIds.Contains).ToHashSet();
        MasteredSkills = MasteredSkills.Where(ValidSkillIds.Contains).ToHashSet();

        // Mastery replaces proficiency/training. A skill may not be in both sets.
        TrainedSkills.ExceptWith(MasteredSkills);

        while (SkillPointsSpent() > SkillPointBudget && TrainedSkills.Count > 0)
            TrainedSkills.Remove(TrainedSkills.Last());

        while (SkillPointsSpent() > SkillPointBudget && MasteredSkills.Count > 0)
            MasteredSkills.Remove(MasteredSkills.Last());
    }

    public int SkillPointsSpent()
    {
        return TrainedSkills.Count + MasteredSkills.Count * 2;
    }
}
