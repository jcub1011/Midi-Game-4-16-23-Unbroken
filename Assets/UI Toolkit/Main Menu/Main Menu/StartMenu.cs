using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MainStartMenu;

public class StartMenu : MonoBehaviour, IDocHandler
{
    #region IDs
    const string STRT_BTN = "StartButton";
    const string SETTN_BTN = "SettingsButton";
    #endregion

    #region Interface
    public void OnShow()
    {
        Debug.Log("Displaying main menu.");
    }
    public void OnHide()
    {
        Debug.Log("Hiding main menu.");
    }
    #endregion

    void Start()
    {
        // Get Documents
        DocHandler.Add(Documents.Main, 
            transform.GetComponent<UIDocument>(), 
            transform.GetComponent<StartMenu>());
        DocHandler.Add(Documents.SongSelect, 
            transform.GetChild(0).GetComponent<UIDocument>(),
            transform.GetChild(0).GetComponent<SongSelector>());
        DocHandler.Add(Documents.SongSetts, 
            transform.GetChild(1).GetComponent<UIDocument>(),
            transform.GetChild(1).GetComponent<SongSettings>());
        DocHandler.Add(Documents.Preview, 
            transform.GetChild(2).GetComponent<UIDocument>(),
            transform.GetChild(2).GetComponent<Preview>());

        // Show Main.
        DocHandler.Show(Documents.Main);

        // Register Buttons.
        var mainMenu = DocHandler.GetRoot(Documents.Main);
        var start = mainMenu.Q(STRT_BTN) as Button;
        var settings = mainMenu.Q(SETTN_BTN) as Button;

        start.clicked += StartButtonPressed;
        settings.clicked += SettingsButtonPressed;

        
    }

    #region Methods
    void StartButtonPressed()
    {
        Debug.Log("Start button pressed.");
    }

    void SettingsButtonPressed()
    {
        Debug.Log("Settings button pressed.");
    }
    #endregion
}