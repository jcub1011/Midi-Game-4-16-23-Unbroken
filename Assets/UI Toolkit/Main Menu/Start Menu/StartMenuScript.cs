using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class StartMenuScript : MonoBehaviour
{
    public VisualTreeAsset MainMenuDoc;
    public VisualTreeAsset SongSelectDoc;
    public VisualTreeAsset SongSettingsDoc;
    public VisualTreeAsset PreviewPanelDoc;
    public Camera previewCamera;
    private PanelManager _panelManager;

    // Constants
    const string MAIN_MENU = "Main Menu";
    const string SELECT_MENU = "Song Selector Menu";
    const string SONG_SETTINGS_MENU = "Song Settings Menu";

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _panelManager = new();

        // Get panels.
        _panelManager.AddPanel(MAIN_MENU, new MainMenu(MainMenuDoc));
        _panelManager.AddPanel(SELECT_MENU, new SongSelectorMenu(SongSelectDoc));
        _panelManager.AddPanel(SONG_SETTINGS_MENU, new SongAdjustMenu(SongSettingsDoc, PreviewPanelDoc));

        // Init starting menu.
        foreach (var panelRoot in _panelManager.GetPanelRoots()) root.Add(panelRoot);
        _panelManager.MakeActive(MAIN_MENU);

        // Add button callbacks.
        var main = _panelManager.GetPanel(MAIN_MENU) as MainMenu;
        main.OnStartClicked += SwitchToSongSelectMenu;
        var selector = _panelManager.GetPanel(SELECT_MENU) as SongSelectorMenu;
        selector.OnBackButtonPress += HandleBackButton;
        selector.OnSongClicked += SwitchToSongSettingsMenu;

        var songSettings = _panelManager.GetPanel(SONG_SETTINGS_MENU) as SongAdjustMenu;
        songSettings.OnBackButtonPress += HandleBackButton;

    }

    void HandleBackButton()
    {
        _panelManager.PreviousPanel();
    }

    void SwitchToSongSelectMenu()
    {
        string midiFileLocation = Directory.GetCurrentDirectory() + "/Assets/MidiFiles";
        _panelManager.MakeActive(SELECT_MENU);
        var temp = _panelManager.GetPanel(SELECT_MENU) as SongSelectorMenu;
        temp.RefreshSongsList(midiFileLocation);
    }

    void SwitchToSongSettingsMenu(string midiPath)
    {
        var temp = _panelManager.GetPanel(SONG_SETTINGS_MENU) as SongAdjustMenu;
        temp.LoadSongSettings(midiPath);
        _panelManager.MakeActive(SONG_SETTINGS_MENU);
    }
}
