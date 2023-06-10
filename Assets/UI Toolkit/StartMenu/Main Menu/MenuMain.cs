using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MainStartMenu;

public class MenuMain : MonoBehaviour, IDocHandler
{
    #region IDs
    const string STRT_BTN = "StartButton";
    const string SETTN_BTN = "SettingsButton";
    #endregion

    #region Interface
    public void OnShow()
    {
        Debug.Log("Displaying main menu.");

        // Register Buttons.
        var mainMenu = DocHandler.GetRoot(Documents.Main);
        var start = mainMenu.Q(STRT_BTN) as Button;
        var settings = mainMenu.Q(SETTN_BTN) as Button;

        start.clicked += StartButtonPressed;
        settings.clicked += SettingsButtonPressed;
    }

    public void OnHide()
    {
        Debug.Log("Hiding main menu.");
    }

    public void OnDocAdd()
    {
        Debug.Log("Main menu panel added.");
    }

    public void OnDocRemove()
    {
        Debug.Log("Start menu panel removed.");
    }
    #endregion

    #region Methods
    void StartButtonPressed()
    {
        Debug.Log("Start button pressed.");
        DocHandler.DisplayDoc(Documents.SongSelect);
    }

    void SettingsButtonPressed()
    {
        Debug.Log("Settings button pressed.");
    }
    #endregion
}