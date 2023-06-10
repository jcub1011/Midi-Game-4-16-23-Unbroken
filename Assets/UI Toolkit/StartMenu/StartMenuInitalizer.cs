using MainStartMenu;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class StartMenuInitalizer : MonoBehaviour
{
    void Start()
    {
        // Get Documents
        DocHandler.Add(Documents.Main,
            transform.GetChild(0).GetComponent<UIDocument>(),
            transform.GetChild(0).GetComponent<MenuMain>());
        DocHandler.Add(Documents.SongSelect,
            transform.GetChild(1).GetComponent<UIDocument>(),
            transform.GetChild(1).GetComponent<SongSelector>());
        DocHandler.Add(Documents.SongSetts,
            transform.GetChild(2).GetComponent<UIDocument>(),
            transform.GetChild(2).GetComponent<SongSettings>());
        DocHandler.Add(Documents.Preview,
            transform.GetChild(3).GetComponent<UIDocument>(),
            transform.GetChild(3).GetComponent<Preview>());

        // Show Main.
        DocHandler.DisplayDoc(Documents.Main);
    }
}
