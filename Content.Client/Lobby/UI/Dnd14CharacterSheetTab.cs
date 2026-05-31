using System.Linq;
using System.Numerics;
using Content.Shared.Preferences;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

/// <summary>
/// SS14 DND character sheet UI for character creation.
/// Uses the current character slot so different character slots have different sheets.
/// </summary>
public sealed class Dnd14CharacterSheetTab : BoxContainer
{
    private static readonly Dictionary<int, Dnd14CharacterSheet> SlotSheets = new();

    private const int StartingStat = Dnd14CharacterSheet.StartingStat;
    private const int CreationMaxStat = Dnd14CharacterSheet.CreationMaxStat;
    private const int StartingPoints = Dnd14CharacterSheet.StartingPoints;
    private const int SkillPointBudget = Dnd14CharacterSheet.SkillPointBudget;

    private readonly Label _statusLabel;
    private readonly Label _remainingPointsLabel;
    private readonly Label _finalizeRequirementsLabel;
    private readonly OptionButton _classButton;
    private readonly Label _classDescriptionLabel;
    private readonly LineEdit _backgroundEdit;
    private readonly TextEdit _notesEdit;
    private readonly Button _finalizeButton;
    private readonly Button _reloadButton;

    private readonly Dictionary<string, StatRow> _stats = new();
    private readonly Dictionary<string, SkillRow> _skills = new();
    private bool _finalized;
    private bool _loading;
    private int? _loadedSlot;
    private string _selectedClass = Dnd14CharacterSheet.DefaultClassId;

    private readonly (string Id, string Name, string Description)[] _classDefinitions =
    {
        (Dnd14CharacterSheet.DefaultClassId, "No Class", "No DND14 class selected."),
        (Dnd14CharacterSheet.MedicClassId, "Medic", "Emergency medical responder. Empty placeholder; abilities will be added later."),
        (Dnd14CharacterSheet.OperativeClassId, "Operative", "Heavy combat field operative. Empty placeholder; abilities will be added later."),
        (Dnd14CharacterSheet.SpecialistClassId, "Specialist", "Light recon and mobility specialist. Empty placeholder; abilities will be added later."),
        (Dnd14CharacterSheet.EngineerClassId, "Engineer", "Field repair and deployable equipment expert. Empty placeholder; abilities will be added later."),
        (Dnd14CharacterSheet.ResearcherClassId, "Researcher", "Science, anomaly, and unknown hazard analyst. Empty placeholder; abilities will be added later."),
        (Dnd14CharacterSheet.HazardSpecialistClassId, "Hazard Specialist", "Radiation, toxin, atmos, and biohazard response specialist. Empty placeholder; abilities will be added later."),
        (Dnd14CharacterSheet.LogisticsTechnicianClassId, "Logistics Technician", "Supplies, equipment, batteries, and field resource support. Empty placeholder; abilities will be added later."),
    };

    private readonly (string Id, string Name)[] _statDefinitions =
    {
        ("Strength", "Strength"),
        ("Agility", "Agility"),
        ("Endurance", "Endurance"),
        ("Intelligence", "Intelligence"),
        ("Wisdom", "Wisdom"),
        ("Social", "Social"),
    };

    private readonly (string Id, string Name, string Stat)[] _skillDefinitions =
    {
        ("Perception", "Perception", "Wisdom"),
        ("Investigation", "Investigation", "Intelligence"),
        ("Persuasion", "Persuasion", "Social"),
        ("Deception", "Deception", "Social"),
        ("Performance", "Performance", "Social"),
        ("Intimidation", "Intimidation", "Strength"),
        ("Medicine", "Medicine", "Intelligence"),
        ("Engineering", "Engineering", "Intelligence"),
        ("Technology", "Technology", "Intelligence"),
        ("Science", "Science", "Intelligence"),
        ("Survival", "Survival", "Wisdom"),
        ("Stealth", "Stealth", "Agility"),
        ("SleightOfHand", "Sleight of Hand", "Agility"),
    };

    public static void UnlockAllCachedSheets()
    {
        foreach (var key in SlotSheets.Keys.ToArray())
            SlotSheets[key] = SlotSheets[key].UnlockedClone();
    }

    public Dnd14CharacterSheetTab()
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        VerticalExpand = true;

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(10),
        };

        scroll.AddChild(root);
        AddChild(scroll);

        root.AddChild(new RichTextLabel
        {
            Text = "[bold]Character Sheet[/bold]\n[color=gray]Spend 8 core stat points, spend 4 skill points, select a class, add a background, then finalize to lock the sheet. Proficiency costs 1 point and gives +2. Mastery costs 2 points and gives +4.[/color]",
        });

        var header = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            Margin = new Thickness(0, 8, 0, 4),
        };

        _statusLabel = new Label();
        _remainingPointsLabel = new Label();
        _finalizeRequirementsLabel = new Label();
        _finalizeButton = new Button { Text = "Finalize Sheet" };
        _reloadButton = new Button { Text = "Load Current Character" };
        _finalizeButton.OnPressed += _ => FinalizeSheet();
        _reloadButton.OnPressed += _ => LoadFromCurrentCharacter(force: true);

        header.AddChild(_statusLabel);
        header.AddChild(new Control { HorizontalExpand = true });
        header.AddChild(_remainingPointsLabel);
        header.AddChild(_reloadButton);
        header.AddChild(_finalizeButton);
        root.AddChild(header);
        root.AddChild(_finalizeRequirementsLabel);

        var topGrid = new GridContainer { Columns = 2 };
        topGrid.AddChild(new Label { Text = "Level" });
        topGrid.AddChild(new Label { Text = "1" });
        topGrid.AddChild(new Label { Text = "XP" });
        topGrid.AddChild(new Label { Text = "0 / 100" });
        topGrid.AddChild(new Label { Text = "Class" });
        _classButton = new OptionButton { MinSize = new Vector2(220, 0) };
        for (var i = 0; i < _classDefinitions.Length; i++)
            _classButton.AddItem(_classDefinitions[i].Name, i);

        _classButton.OnItemSelected += args =>
        {
            if (_finalized)
                return;

            _classButton.SelectId(args.Id);
            _selectedClass = _classDefinitions[args.Id].Id;
            Refresh();
            SaveCurrentSheet();
        };
        topGrid.AddChild(_classButton);
        topGrid.AddChild(new Label { Text = "Class Info" });
        _classDescriptionLabel = new Label();
        topGrid.AddChild(_classDescriptionLabel);
        topGrid.AddChild(new Label { Text = "Background" });
        _backgroundEdit = new LineEdit
        {
            MinSize = new Vector2(360, 0),
            PlaceHolder = "Example: Ex-security contractor, field medic, station drifter...",
        };
        _backgroundEdit.OnTextChanged += _ =>
        {
            Refresh();
            SaveCurrentSheet();
        };
        topGrid.AddChild(_backgroundEdit);
        root.AddChild(topGrid);

        AddSpacer(root);
        root.AddChild(new RichTextLabel { Text = "[bold]Core Stats[/bold]" });

        var statsGrid = new GridContainer { Columns = 5 };
        statsGrid.AddChild(new Label { Text = "Stat" });
        statsGrid.AddChild(new Label { Text = "Value" });
        statsGrid.AddChild(new Label { Text = "Mod" });
        statsGrid.AddChild(new Label { Text = "" });
        statsGrid.AddChild(new Label { Text = "" });

        foreach (var stat in _statDefinitions)
        {
            var row = new StatRow(stat.Id, stat.Name);
            _stats[stat.Id] = row;

            statsGrid.AddChild(new Label { Text = stat.Name });
            statsGrid.AddChild(row.ValueLabel);
            statsGrid.AddChild(row.ModLabel);
            statsGrid.AddChild(row.MinusButton);
            statsGrid.AddChild(row.PlusButton);

            row.MinusButton.OnPressed += _ => ChangeStat(stat.Id, -1);
            row.PlusButton.OnPressed += _ => ChangeStat(stat.Id, 1);
        }

        root.AddChild(statsGrid);

        AddSpacer(root);
        root.AddChild(new RichTextLabel { Text = $"[bold]Skills[/bold]\n[color=gray]Spend exactly {SkillPointBudget} skill points. Proficiency costs 1 point. Mastery costs 2 points and replaces proficiency.[/color]" });

        var skillsGrid = new GridContainer { Columns = 6 };
        skillsGrid.AddChild(new Label { Text = "Skill" });
        skillsGrid.AddChild(new Label { Text = "Core" });
        skillsGrid.AddChild(new Label { Text = "Mod" });
        skillsGrid.AddChild(new Label { Text = "Prof" });
        skillsGrid.AddChild(new Label { Text = "Mastery" });
        skillsGrid.AddChild(new Label { Text = "Total" });

        foreach (var skill in _skillDefinitions)
        {
            var row = new SkillRow(skill.Id, skill.Name, skill.Stat);
            _skills[skill.Id] = row;

            skillsGrid.AddChild(new Label { Text = skill.Name });
            skillsGrid.AddChild(new Label { Text = skill.Stat });
            skillsGrid.AddChild(row.ModLabel);
            skillsGrid.AddChild(row.TrainedButton);
            skillsGrid.AddChild(row.MasteryButton);
            skillsGrid.AddChild(row.TotalLabel);

            row.TrainedButton.OnPressed += _ => ToggleTrained(skill.Id);
            row.MasteryButton.OnPressed += _ => ToggleMastery(skill.Id);
        }

        root.AddChild(skillsGrid);

        AddSpacer(root);
        root.AddChild(new RichTextLabel { Text = "[bold]Notes[/bold]" });
        _notesEdit = new TextEdit
        {
            MinSize = new Vector2(520, 140),
            HorizontalExpand = true,
        };
        _notesEdit.OnTextChanged += _ => SaveCurrentSheet();
        root.AddChild(_notesEdit);

        Refresh();
    }

    private static void AddSpacer(BoxContainer parent)
    {
        parent.AddChild(new Control { MinSize = new Vector2(0, 14) });
    }

    private HumanoidProfileEditor? FindEditor()
    {
        Control? control = this;
        while (control != null)
        {
            if (control is HumanoidProfileEditor editor)
                return editor;

            control = control.Parent;
        }

        return null;
    }

    private void LoadFromCurrentCharacter(bool force = false)
    {
        if (_loading)
            return;

        var editor = FindEditor();
        if (editor?.Profile == null || editor.CharacterSlot == null)
            return;

        var slot = editor.CharacterSlot.Value;
        if (!force && _loadedSlot == slot)
            return;

        if (!SlotSheets.TryGetValue(slot, out var sheet))
        {
            sheet = editor.Profile.Dnd14Sheet.Clone();
            sheet.EnsureValid();
            SlotSheets[slot] = sheet;
        }

        _loadedSlot = slot;
        ApplySheet(sheet);
    }

    private void ApplySheet(Dnd14CharacterSheet sheet)
    {
        _loading = true;
        sheet.EnsureValid();

        _finalized = sheet.Finalized;
        _selectedClass = sheet.ClassId;
        _backgroundEdit.Text = sheet.Background;
        _notesEdit.TextRope = new Rope.Leaf(sheet.Notes);

        _stats["Strength"].Value = sheet.Strength;
        _stats["Agility"].Value = sheet.Agility;
        _stats["Endurance"].Value = sheet.Endurance;
        _stats["Intelligence"].Value = sheet.Intelligence;
        _stats["Wisdom"].Value = sheet.Wisdom;
        _stats["Social"].Value = sheet.Social;

        foreach (var skill in _skills.Values)
        {
            skill.Trained = sheet.TrainedSkills.Contains(skill.Id);
            skill.Mastered = sheet.MasteredSkills.Contains(skill.Id);
        }

        _loading = false;
        Refresh();
    }

    private Dnd14CharacterSheet BuildSheet()
    {
        var sheet = new Dnd14CharacterSheet
        {
            Finalized = _finalized,
            ClassId = _selectedClass,
            Background = _backgroundEdit.Text.Trim(),
            Notes = Rope.Collapse(_notesEdit.TextRope).Trim(),
            Strength = _stats["Strength"].Value,
            Agility = _stats["Agility"].Value,
            Endurance = _stats["Endurance"].Value,
            Intelligence = _stats["Intelligence"].Value,
            Wisdom = _stats["Wisdom"].Value,
            Social = _stats["Social"].Value,
            TrainedSkills = _skills.Values.Where(skill => skill.Trained).Select(skill => skill.Id).ToHashSet(),
            MasteredSkills = _skills.Values.Where(skill => skill.Mastered).Select(skill => skill.Id).ToHashSet(),
        };

        sheet.EnsureValid();
        return sheet;
    }

    private void SaveCurrentSheet()
    {
        if (_loading)
            return;

        var editor = FindEditor();
        if (editor?.Profile == null || editor.CharacterSlot == null)
            return;

        var sheet = BuildSheet();
        SlotSheets[editor.CharacterSlot.Value] = sheet;
        editor.Profile = editor.Profile.WithDnd14Sheet(sheet);
        editor.IsDirty = true;
    }

    private void ChangeStat(string id, int delta)
    {
        LoadFromCurrentCharacter();

        if (_finalized)
            return;

        var row = _stats[id];
        var newValue = row.Value + delta;
        if (newValue < StartingStat || newValue > CreationMaxStat)
            return;

        if (delta > 0 && RemainingPoints() <= 0)
            return;

        row.Value = newValue;
        Refresh();
        SaveCurrentSheet();
    }

    private void ToggleTrained(string id)
    {
        LoadFromCurrentCharacter();

        if (_finalized)
            return;

        var row = _skills[id];
        if (row.Mastered)
            return;

        if (!row.Trained && SkillPointsSpent() + 1 > SkillPointBudget)
            return;

        row.Trained = !row.Trained;
        Refresh();
        SaveCurrentSheet();
    }

    private void ToggleMastery(string id)
    {
        LoadFromCurrentCharacter();

        if (_finalized)
            return;

        var row = _skills[id];
        var currentCost = row.Mastered ? 2 : row.Trained ? 1 : 0;
        var newCost = row.Mastered ? 0 : 2;

        if (SkillPointsSpent() - currentCost + newCost > SkillPointBudget)
            return;

        row.Mastered = !row.Mastered;
        if (row.Mastered)
            row.Trained = false;

        Refresh();
        SaveCurrentSheet();
    }

    private void FinalizeSheet()
    {
        LoadFromCurrentCharacter();

        if (_finalized || !CanFinalize())
            return;

        _finalized = true;
        Refresh();
        SaveCurrentSheet();
    }

    private bool CanFinalize()
    {
        return RemainingPoints() == 0
               && SkillPointsSpent() == SkillPointBudget
               && _selectedClass != Dnd14CharacterSheet.DefaultClassId
               && !string.IsNullOrWhiteSpace(_backgroundEdit.Text);
    }

    private string GetFinalizeRequirementText()
    {
        if (_finalized)
            return "Sheet finalized. Editing is locked.";

        var missing = new List<string>();
        if (RemainingPoints() != 0)
            missing.Add($"spend all stat points ({RemainingPoints()} left)");
        if (SkillPointsSpent() != SkillPointBudget)
            missing.Add($"spend {SkillPointBudget} skill points ({SkillPointsSpent()}/{SkillPointBudget})");
        if (_selectedClass == Dnd14CharacterSheet.DefaultClassId)
            missing.Add("select a class");
        if (string.IsNullOrWhiteSpace(_backgroundEdit.Text))
            missing.Add("fill Background");

        return missing.Count == 0
            ? "Ready to finalize. This will lock the sheet."
            : $"Finalize requires: {string.Join(", ", missing)}.";
    }

    private int RemainingPoints()
    {
        return StartingPoints - _stats.Values.Sum(stat => stat.Value - StartingStat);
    }

    private int SkillPointsSpent()
    {
        return _skills.Values.Sum(skill => (skill.Trained ? 1 : 0) + (skill.Mastered ? 2 : 0));
    }

    private static int Modifier(int stat)
    {
        return (int)Math.Floor((stat - 10) / 2f);
    }

    private static string Signed(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private void Refresh()
    {
        if (!_loading)
            LoadFromCurrentCharacter();

        var remaining = RemainingPoints();
        var skillPoints = SkillPointsSpent();
        var classIndex = Math.Max(0, Array.FindIndex(_classDefinitions, definition => definition.Id == _selectedClass));
        _classButton.SelectId(classIndex);
        _classButton.Disabled = _finalized;
        _classDescriptionLabel.Text = _classDefinitions[classIndex].Description;
        _statusLabel.Text = _finalized ? "Status: Finalized" : "Status: Draft";
        _remainingPointsLabel.Text = $"Points: {remaining} | Skills: {skillPoints}/{SkillPointBudget}";
        _finalizeRequirementsLabel.Text = GetFinalizeRequirementText();

        foreach (var row in _stats.Values)
        {
            var modifier = Modifier(row.Value);
            row.ValueLabel.Text = row.Value.ToString();
            row.ModLabel.Text = Signed(modifier);
            row.MinusButton.Disabled = _finalized || row.Value <= StartingStat;
            row.PlusButton.Disabled = _finalized || row.Value >= CreationMaxStat || remaining <= 0;
        }

        foreach (var row in _skills.Values)
        {
            var statMod = Modifier(_stats[row.Stat].Value);
            var total = statMod + (row.Mastered ? 4 : row.Trained ? 2 : 0);
            row.ModLabel.Text = Signed(statMod);
            row.TotalLabel.Text = Signed(total);
            row.TrainedButton.Text = row.Trained ? "Yes" : "No";
            row.MasteryButton.Text = row.Mastered ? "Yes" : "No";
            row.TrainedButton.Pressed = row.Trained;
            row.MasteryButton.Pressed = row.Mastered;
            row.TrainedButton.Disabled = _finalized || row.Mastered || (!row.Trained && skillPoints + 1 > SkillPointBudget);
            row.MasteryButton.Disabled = _finalized || (!row.Mastered && skillPoints - (row.Trained ? 1 : 0) + 2 > SkillPointBudget);
        }

        _backgroundEdit.Editable = !_finalized;
        _notesEdit.Editable = !_finalized;
        _finalizeButton.Disabled = _finalized || !CanFinalize();
    }

    private sealed class StatRow
    {
        public readonly string Id;
        public readonly string Name;
        public int Value = StartingStat;
        public readonly Label ValueLabel = new();
        public readonly Label ModLabel = new();
        public readonly Button MinusButton = new() { Text = "-" };
        public readonly Button PlusButton = new() { Text = "+" };

        public StatRow(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    private sealed class SkillRow
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Stat;
        public bool Trained;
        public bool Mastered;
        public readonly Label ModLabel = new();
        public readonly Button TrainedButton = new() { ToggleMode = true };
        public readonly Button MasteryButton = new() { ToggleMode = true };
        public readonly Label TotalLabel = new();

        public SkillRow(string id, string name, string stat)
        {
            Id = id;
            Name = name;
            Stat = stat;
        }
    }
}
