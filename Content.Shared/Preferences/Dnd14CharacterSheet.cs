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
    public const int RequiredTrainedSkills = 3;

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

    [DataField]
    public int Perception { get; set; } = StartingStat;

    [DataField]
    public int Social { get; set; } = StartingStat;

    [DataField]
    public HashSet<string> TrainedSkills { get; set; } = new();

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
            Perception = Perception,
            Social = Social,
            TrainedSkills = new HashSet<string>(TrainedSkills),
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
               && Perception == other.Perception
               && Social == other.Social
               && TrainedSkills.SetEquals(other.TrainedSkills);
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
        Perception = Math.Clamp(Perception, StartingStat, CreationMaxStat);
        Social = Math.Clamp(Social, StartingStat, CreationMaxStat);
        TrainedSkills ??= new HashSet<string>();

        if (TrainedSkills.Count > RequiredTrainedSkills)
            TrainedSkills = TrainedSkills.Take(RequiredTrainedSkills).ToHashSet();
    }
}
