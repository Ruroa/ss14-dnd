using System.Linq;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

/// <summary>
/// First-pass SS14 DND character sheet UI for character creation.
/// Persistence will be wired into the profile after the tab layout is validated in-game.
/// </summary>
public sealed class Dnd14CharacterSheetTab : BoxContainer
{
    private const int StartingStat = 10;
    private const int CreationMaxStat = 16;
    private const int StartingPoints = 8;
    private const int RequiredTrainedSkills = 3;

    private readonly Label _statusLabel;
    private readonly Label _remainingPointsLabel;
    private readonly Label _finalizeRequirementsLabel;
    private readonly LineEdit _backgroundEdit;
    private readonly TextEdit _notesEdit;
    private readonly Button _finalizeButton;

    private readonly Dictionary<string, StatRow> _stats = new();
    private readonly Dictionary<string, SkillRow> _skills = new();
    private bool _finalized;

    private readonly (string Id, string Name)[] _statDefinitions =
    {
        ("Strength", "Strength"),
        ("Agility", "Agility"),
        ("Endurance", "Endurance"),
        ("Intelligence", "Intelligence"),
        ("Perception", "Perception"),
        ("Social", "Social"),
    };

    private readonly (string Id, string Name, string Stat)[] _skillDefinitions =
    {
        ("Perception", "Perception", "Perception"),
        ("Investigation", "Investigation", "Intelligence"),
        ("Persuasion", "Persuasion", "Social"),
        ("Intimidation", "Intimidation", "Social"),
        ("Medicine", "Medicine", "Intelligence"),
        ("Engineering", "Engineering", "Intelligence"),
        ("Science", "Science", "Intelligence"),
        ("Security", "Security", "Agility"),
        ("Survival", "Survival", "Perception"),
        ("Stealth", "Stealth", "Agility"),
    };

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
            Text = "[bold]Character Sheet[/bold]\n[color=gray]Spend 8 core stat points, choose 3 trained skills, add a background, then finalize to lock the sheet.[/color]",
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
        _finalizeButton.OnPressed += _ => FinalizeSheet();

        header.AddChild(_statusLabel);
        header.AddChild(new Control { HorizontalExpand = true });
        header.AddChild(_remainingPointsLabel);
        header.AddChild(_finalizeButton);
        root.AddChild(header);
        root.AddChild(_finalizeRequirementsLabel);

        var topGrid = new GridContainer { Columns = 2 };
        topGrid.AddChild(new Label { Text = "Level" });
        topGrid.AddChild(new Label { Text = "1" });
        topGrid.AddChild(new Label { Text = "XP" });
        topGrid.AddChild(new Label { Text = "0 / 100" });
        topGrid.AddChild(new Label { Text = "Background" });
        _backgroundEdit = new LineEdit
        {
            MinSize = new Vector2(360, 0),
            PlaceHolder = "Example: Ex-security contractor, field medic, station drifter...",
        };
        _backgroundEdit.OnTextChanged += _ => Refresh();
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
        root.AddChild(new RichTextLabel { Text = $"[bold]Skills[/bold]\n[color=gray]Choose exactly {RequiredTrainedSkills}. Trained skills get +2.[/color]" });

        var skillsGrid = new GridContainer { Columns = 5 };
        skillsGrid.AddChild(new Label { Text = "Skill" });
        skillsGrid.AddChild(new Label { Text = "Core" });
        skillsGrid.AddChild(new Label { Text = "Mod" });
        skillsGrid.AddChild(new Label { Text = "Trained" });
        skillsGrid.AddChild(new Label { Text = "Total" });

        foreach (var skill in _skillDefinitions)
        {
            var row = new SkillRow(skill.Id, skill.Name, skill.Stat);
            _skills[skill.Id] = row;

            skillsGrid.AddChild(new Label { Text = skill.Name });
            skillsGrid.AddChild(new Label { Text = skill.Stat });
            skillsGrid.AddChild(row.ModLabel);
            skillsGrid.AddChild(row.TrainedButton);
            skillsGrid.AddChild(row.TotalLabel);

            row.TrainedButton.OnPressed += _ => ToggleTrained(skill.Id);
        }

        root.AddChild(skillsGrid);

        AddSpacer(root);
        root.AddChild(new RichTextLabel { Text = "[bold]Notes[/bold]" });
        _notesEdit = new TextEdit
        {
            MinSize = new Vector2(520, 140),
            HorizontalExpand = true,
        };
        root.AddChild(_notesEdit);

        Refresh();
    }

    private static void AddSpacer(BoxContainer parent)
    {
        parent.AddChild(new Control { MinSize = new Vector2(0, 14) });
    }

    private void ChangeStat(string id, int delta)
    {
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
    }

    private void ToggleTrained(string id)
    {
        if (_finalized)
            return;

        var row = _skills[id];
        if (!row.Trained && TrainedSkillCount() >= RequiredTrainedSkills)
            return;

        row.Trained = !row.Trained;
        Refresh();
    }

    private void FinalizeSheet()
    {
        if (_finalized || !CanFinalize())
            return;

        _finalized = true;
        Refresh();
    }

    private bool CanFinalize()
    {
        return RemainingPoints() == 0
               && TrainedSkillCount() == RequiredTrainedSkills
               && !string.IsNullOrWhiteSpace(_backgroundEdit.Text);
    }

    private string GetFinalizeRequirementText()
    {
        if (_finalized)
            return "Sheet finalized. Editing is locked.";

        var missing = new List<string>();
        if (RemainingPoints() != 0)
            missing.Add($"spend all stat points ({RemainingPoints()} left)");
        if (TrainedSkillCount() != RequiredTrainedSkills)
            missing.Add($"choose {RequiredTrainedSkills} trained skills ({TrainedSkillCount()}/{RequiredTrainedSkills})");
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

    private int TrainedSkillCount()
    {
        return _skills.Values.Count(skill => skill.Trained);
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
        var remaining = RemainingPoints();
        var trained = TrainedSkillCount();
        _statusLabel.Text = _finalized ? "Status: Finalized" : "Status: Draft";
        _remainingPointsLabel.Text = $"Points: {remaining} | Skills: {trained}/{RequiredTrainedSkills}";
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
            var total = statMod + (row.Trained ? 2 : 0);
            row.ModLabel.Text = Signed(statMod);
            row.TotalLabel.Text = Signed(total);
            row.TrainedButton.Text = row.Trained ? "Yes" : "No";
            row.TrainedButton.Pressed = row.Trained;
            row.TrainedButton.Disabled = _finalized || (!row.Trained && trained >= RequiredTrainedSkills);
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
        public readonly Label ModLabel = new();
        public readonly Button TrainedButton = new() { ToggleMode = true };
        public readonly Label TotalLabel = new();

        public SkillRow(string id, string name, string stat)
        {
            Id = id;
            Name = name;
            Stat = stat;
        }
    }
}
