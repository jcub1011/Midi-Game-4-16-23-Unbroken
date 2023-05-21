using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class StartMenuScript : MonoBehaviour
{
    public VisualTreeAsset MainMenuDoc;
    public VisualTreeAsset SongSelectDoc;
    private MainMenu _mainMenu;
    private SongSelectorMenu _songSelectorMenu;

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        // Get panels.
        _mainMenu = new MainMenu(MainMenuDoc);
        _songSelectorMenu = new SongSelectorMenu(SongSelectDoc);

        // Init starting menu.
        _mainMenu.Visible = true;
        _songSelectorMenu.Visible = false;
        root.Add(_mainMenu.Root);
        root.Add(_songSelectorMenu.Root);

        // Add button callbacks.
        _mainMenu.OnStartClicked += SwitchToSongSelectMenu;
    }

    void SwitchToSongSelectMenu()
    {
        string midiFileLocation = Directory.GetCurrentDirectory() + "/Assets/MidiFiles";
        _mainMenu.Visible = false;
        _songSelectorMenu.Visible = true;
        _songSelectorMenu.RefreshSongsList(midiFileLocation);
    }
}
