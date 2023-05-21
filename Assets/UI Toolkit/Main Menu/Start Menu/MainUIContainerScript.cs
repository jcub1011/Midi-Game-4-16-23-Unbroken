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
        // Get panels.
        _mainMenu = new MainMenu { Root = MainMenuDoc.CloneTree()};
        //_songSelectMenu = root.Q(name:"SongSelector");

        // Init starting menu.
        _mainMenu.Root.style.display = DisplayStyle.Flex;
        //_songSelectMenu.style.display = DisplayStyle.None;
    }
}
