using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

/// <summary>
/// SS14 DND campaign character setup tab container.
/// Removes the Antags tab while preserving the existing HumanoidProfileEditor setup code path.
/// </summary>
public sealed class CampaignTabContainer : TabContainer
{
    private const int AntagTabIndex = 2;
    private bool _removedAntagTab;

    public new void SetTabTitle(int tab, string title)
    {
        RemoveAntagTabIfNeeded();

        // HumanoidProfileEditor.SetupTabs() still tries to title the old Antags tab.
        // Skip it, then shift later titles left by one.
        if (tab == AntagTabIndex)
            return;

        if (tab > AntagTabIndex)
            tab--;

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
