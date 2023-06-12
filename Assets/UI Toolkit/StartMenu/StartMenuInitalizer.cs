using MainStartMenu;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class StartMenuInitalizer : MonoBehaviour
{
    #region Documents
    public GameObject TheMainMenu;
    public GameObject TheSongSelector;
    public GameObject TheSongSettings;
    public GameObject ThePreview;
    #endregion

    void Start()
    {
        // Get Documents
        DocHandler.Add(Documents.Main,
            TheMainMenu.GetComponent<UIDocument>(),
            TheMainMenu.GetComponent<MenuMain>());
        DocHandler.Add(Documents.SongSelect,
            TheSongSelector.GetComponent<UIDocument>(),
            TheSongSelector.GetComponent<SongSelector>());
        DocHandler.Add(Documents.SongSetts,
            TheSongSettings.GetComponent<UIDocument>(),
            TheSongSettings.GetComponent<SongSettings>());
        DocHandler.Add(Documents.Preview,
            ThePreview.GetComponent<UIDocument>(),
            ThePreview.GetComponent<Preview>());

        // Show Main.
        DocHandler.DisplayDoc(Documents.Main);
    }
}
