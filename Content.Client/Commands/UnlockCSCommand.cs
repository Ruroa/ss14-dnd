using Content.Client.Lobby.UI;
using Robust.Shared.Console;

namespace Content.Client.Commands;

public sealed class UnlockCSCommand : IConsoleCommand
{
    public string Command => "unlockcs";
    public string Description => "Unlocks all cached DND14 character sheets on this client.";
    public string Help => "Usage: UnlockCS [player]\nThe player argument is accepted for DM workflow convenience, but currently all cached local character sheets are unlocked.";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        Dnd14CharacterSheetTab.UnlockAllCachedSheets();
        shell.WriteLine("Unlocked all cached DND14 character sheets on this client.");
    }
}
