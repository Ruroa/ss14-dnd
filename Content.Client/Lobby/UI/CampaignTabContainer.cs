using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

/// <summary>
/// SS14 DND campaign character setup tab container.
/// Removes the Antags tab while preserving the existing HumanoidProfileEditor setup code path.
/// The old Antags tab title slot is reused for the DND14 character sheet tab.
/// </summary>
public sealed class CampaignTabContainer : TabContainer
{
    private const int AntagTabIndex = 2;
    private bool _removedAntagTab;

    public new void SetTabTitle(int tab, string title)
    {
        RemoveAntagTabIfNeeded();

        // HumanoidProfileEditor.SetupTabs() still tries to title the old Antags tab at index 2.
        // After removing that hidden placeholder, index 2 is our new DND14 sheet.
        if (tab == AntagTabIndex)
            title = "Character Sheet";

        if (tab < 0 || tab >= ChildCount)
            return;

        base.SetTabTitle(tab, title);
    }

    private void RemoveAntagTabIfNeeded()
    {
        if (_removedAntagTab || ChildCount <= AntagTabIndex)
            return;

        RemoveChild(GetChild(AntagTabIndex));
        _removedAntagTab = true;
    }
}
