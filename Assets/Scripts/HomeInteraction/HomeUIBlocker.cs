using UnityEngine;

/// <summary>
/// Tracks open Home-scene overlay panels. While any panel is open the game is
/// paused and world click interactions are ignored so clicks do not pass through UI.
/// </summary>
public static class HomeUIBlocker
{
    private static int openCount;

    private static PlayerTopDownController playerController;
    private static PlayerCombatController combatController;
    private static bool playerWasEnabled;
    private static bool combatWasEnabled;

    public static bool IsBlocked => openCount > 0;

    public static void Acquire()
    {
        if (openCount == 0)
        {
            Time.timeScale = 0f;
            DisablePlayerInput();
        }

        openCount++;
    }

    public static void Release()
    {
        if (openCount <= 0)
        {
            return;
        }

        openCount--;
        if (openCount == 0)
        {
            RestorePlayerInput();
            Time.timeScale = 1f;
        }
    }

    public static void ForceReset()
    {
        openCount = 0;
        RestorePlayerInput();
    }

    private static void DisablePlayerInput()
    {
        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<PlayerTopDownController>();
        }

        if (playerController != null)
        {
            playerWasEnabled = playerController.enabled;
            playerController.enabled = false;
        }

        if (combatController == null)
        {
            combatController = Object.FindFirstObjectByType<PlayerCombatController>();
        }

        if (combatController != null)
        {
            combatWasEnabled = combatController.enabled;
            combatController.enabled = false;
        }
    }

    private static void RestorePlayerInput()
    {
        if (playerController != null)
        {
            playerController.enabled = playerWasEnabled;
            playerController = null;
        }

        if (combatController != null)
        {
            combatController.enabled = combatWasEnabled;
            combatController = null;
        }
    }
}
