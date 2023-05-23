using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class StartMenuScript : MonoBehaviour
{
    public VisualTreeAsset MainMenuDoc;
    public VisualTreeAsset SongSelectDoc;
    private PanelManager _panelManager;

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _panelManager = new();

        // Get panels.
        _panelManager.AddPanel("MainMenu", new MainMenu(MainMenuDoc));
        _panelManager.AddPanel("SongSelectMenu", new SongSelectorMenu(SongSelectDoc));

        // Init starting menu.
        foreach (var panelRoot in _panelManager.GetPanelRoots()) root.Add(panelRoot);
        _panelManager.MakeActive("MainMenu");

        // Add button callbacks.
        var main = _panelManager.GetPanel("MainMenu") as MainMenu;
        main.OnStartClicked += SwitchToSongSelectMenu;
        var selector = _panelManager.GetPanel("SongSelectMenu") as SongSelectorMenu;
        selector.OnBackButtonPress += HandleBackButton;
    }

    void HandleBackButton()
    {
        _panelManager.PreviousPanel();
    }

    void SwitchToSongSelectMenu()
    {
        string midiFileLocation = Directory.GetCurrentDirectory() + "/Assets/MidiFiles";
        _panelManager.MakeActive("SongSelectMenu");
        var temp = _panelManager.GetPanel("SongSelectMenu") as SongSelectorMenu;
        temp.RefreshSongsList(midiFileLocation);
    }
}
