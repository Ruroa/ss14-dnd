using Content.Shared.Preferences;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DND14;

/// <summary>
/// Runtime DND14 character sheet data attached to spawned players, NPCs, and creatures.
/// Player mobs receive this from their character-creator sheet on spawn.
/// NPCs/creatures can define this component directly in YAML later.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class Dnd14CharacterSheetComponent : Component
{
    [DataField]
    public bool Finalized;

    [DataField]
    public int Level = 1;

    [DataField]
    public int Experience;

    [DataField]
    public string Background = string.Empty;

    [DataField]
    public string Notes = string.Empty;

    [DataField]
    public int Strength = Dnd14CharacterSheet.StartingStat;

    [DataField]
    public int Agility = Dnd14CharacterSheet.StartingStat;

    [DataField]
    public int Endurance = Dnd14CharacterSheet.StartingStat;

    [DataField]
    public int Intelligence = Dnd14CharacterSheet.StartingStat;

    [DataField]
    public int Wisdom = Dnd14CharacterSheet.StartingStat;

    [DataField]
    public int Social = Dnd14CharacterSheet.StartingStat;

    [DataField]
    public HashSet<string> TrainedSkills = new();

    [DataField]
    public HashSet<string> MasteredSkills = new();

    public void LoadFromSheet(Dnd14CharacterSheet sheet)
    {
        sheet.EnsureValid();

        Finalized = sheet.Finalized;
        Level = sheet.Level;
        Experience = sheet.Experience;
        Background = sheet.Background;
        Notes = sheet.Notes;
        Strength = sheet.Strength;
        Agility = sheet.Agility;
        Endurance = sheet.Endurance;
        Intelligence = sheet.Intelligence;
        Wisdom = sheet.Wisdom;
        Social = sheet.Social;
        TrainedSkills = new HashSet<string>(sheet.TrainedSkills);
        MasteredSkills = new HashSet<string>(sheet.MasteredSkills);
    }

    public int GetStat(string stat)
    {
        return stat switch
        {
            "Strength" => Strength,
            "Agility" => Agility,
            "Endurance" => Endurance,
            "Intelligence" => Intelligence,
            "Wisdom" => Wisdom,
            "Social" => Social,
            _ => Dnd14CharacterSheet.StartingStat,
        };
    }

    public static int Modifier(int stat)
    {
        return (int)Math.Floor((stat - 10) / 2f);
    }

    public int GetModifier(string stat)
    {
        return Modifier(GetStat(stat));
    }

    public int GetSkillBonus(string skill, string stat)
    {
        var bonus = GetModifier(stat);

        if (MasteredSkills.Contains(skill))
            bonus += 4;
        else if (TrainedSkills.Contains(skill))
            bonus += 2;

        return bonus;
    }
}
