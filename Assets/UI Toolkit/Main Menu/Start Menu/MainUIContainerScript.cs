using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainUIContainerScript : MonoBehaviour
{
    public VisualTreeAsset MainMenuDoc;
    public VisualTreeAsset SongSelectDoc;
    private MainMenu _mainMenu;
    //private VisualElement _songSelectMenu;

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        // Get panels.
        _mainMenu = new MainMenu(MainMenuDoc);
        //_songSelectMenu = root.Q(name:"SongSelector");

        // Init starting menu.
        _mainMenu.Visible = true;
        root.Add(_mainMenu.Root);
        //_songSelectMenu.style.display = DisplayStyle.None;
    }
}
