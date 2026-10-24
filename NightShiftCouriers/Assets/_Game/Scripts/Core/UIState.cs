using UnityEngine;

// Shared flags for the on-screen menus.
public static class UIState
{
    private static bool menuOpen;

    // True while a menu (shop, pause...) is open: the player stops moving and the mouse is free.
    public static bool MenuOpen
    {
        get => menuOpen;
        set
        {
            if (menuOpen && !value)
            {
                ClosedFrame = Time.frameCount;
            }
            menuOpen = value;
        }
    }

    // The frame a menu last closed. The pause menu uses it so the Esc that closed the shop
    // does not also open the pause menu.
    public static int ClosedFrame { get; private set; } = -10;

    // F1: hide every HUD (for screenshots and trailers).
    public static bool HideHud;

    // F9: the trailer camera is flying, so the player stands still.
    public static bool FreeCam;

    // Gameplay input is ignored while this is true.
    public static bool InputBlocked => MenuOpen || FreeCam;

    public static void CloseMenu()
    {
        MenuOpen = false;
    }
}
