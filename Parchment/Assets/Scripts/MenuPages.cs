using UnityEngine;

// Swaps the palm menu between its main page and its settings page.
// Lives on the panel object (the one PalmMenu shows and hides); the pages are children of it.
// Every time the panel opens it starts on the main page.
public class MenuPages : MonoBehaviour
{
    public GameObject mainPage;       // Page_Main: Parchment / Phantom / Poke / Settings
    public GameObject settingsPage;   // Page_Settings: Back (plus the settings buttons later)
    public PalmMenu menu;             // which hand presses, for the punch-through guard

    public bool SettingsShown => settingsPage != null && settingsPage.activeSelf;

    void OnEnable()
    {
        ShowMain();
    }

    public void ShowSettings()
    {
        Swap(mainPage, settingsPage);
    }

    public void ShowMain()
    {
        Swap(settingsPage, mainPage);
    }

    // Hide one page, show the other, then tell every button on the new page that the press hand
    // already "holds" it, so a fingertip still sitting where it just pressed can't fire again.
    void Swap(GameObject hide, GameObject show)
    {
        if (hide != null) hide.SetActive(false);
        if (show == null) return;
        show.SetActive(true);

        HandPointer presser = menu != null ? menu.pressHand : null;
        if (presser == null) return;
        foreach (MenuButton b in show.GetComponentsInChildren<MenuButton>(true))
            b.HoldUntilClear(presser);
    }
}