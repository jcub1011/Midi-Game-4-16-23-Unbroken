using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.UIElements;

// Delegates
public delegate void ButtonClicked();

abstract public class GameUIPanel
{
    #region Properties
    public VisualElement Root { get; protected set; }
    #endregion

    #region Getters and Setters
    public bool Visible
    {
        get
        {
            if (Root == null) return false;
            return Root.style.display == DisplayStyle.Flex;
        }

        set
        {
            if (Root == null) return;
            Root.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
    #endregion

    #region Constructors
    protected GameUIPanel(VisualTreeAsset doc)
    {
        Root = doc.Instantiate();
    }
    #endregion
}

public class MainMenu : GameUIPanel
{
    #region Properties
    public ButtonClicked OnStartClicked;
    public ButtonClicked OnSettingsClicked;
    #endregion

    void StartButtonClicked()
    {
        Debug.Log("Start button clicked.");
        OnStartClicked?.Invoke();
    }

    void SettingsButtonClicked()
    {
        Debug.Log("Settings button clicked.");
        OnSettingsClicked?.Invoke();
    }

    public MainMenu(VisualTreeAsset doc) : base(doc)
    {
        const string START_BUTTON_ID = "StartButton";
        const string SETTINGS_BUTTON_ID = "SettingsButton";

        // Get element references.
        var startButton = Root.Q(START_BUTTON_ID) as Button;
        var settingsButton = Root.Q(SETTINGS_BUTTON_ID) as Button;

        // Register click events.
        startButton.clicked += StartButtonClicked;
        settingsButton.clicked += SettingsButtonClicked;

        // Make it take the entire sceen.
        Root.style.flexGrow = 1;
    }
}

public class SongSelectorMenu : GameUIPanel
{
    // Constants
    private const string SONG_SELECTOR_CLASS_NAME = "Song-Select-Button";
    private const string SONG_LIST_CONTAINER_ID = "Song-List-Container";
    private const string SONG_SELECTOR_UI_CONTAINER = "SongSelectorContainer";

    public SongSelectorMenu(VisualTreeAsset doc) : base(doc) { }

    public void RefreshSongsList(string songsFolderPath)
    {
        var rootContainer = Root.Q(SONG_SELECTOR_UI_CONTAINER);
        Debug.Log("Getting songs list.");

        List<string> songList = Directory.EnumerateFiles(songsFolderPath).ToList();
        Debug.Log($"Files in midi directory: {songList.Count}");

        // Remove existing list container.
        var oldContainer = Root.Q(SONG_LIST_CONTAINER_ID);
        if (oldContainer != null) Root.Remove(oldContainer);

        // Create new list container.
        var songListContainer = new ScrollView
        {
            name = SONG_LIST_CONTAINER_ID
        };

        // Style
        songListContainer.style.flexGrow = 1;
        songListContainer.style.marginBottom = 10;
        songListContainer.style.marginLeft = 10;
        songListContainer.style.marginRight = 10;

        // Add songs to list container.
        foreach (var song in songList)
        {
            // Skip if not a midi file.
            if (Path.GetExtension(song).ToLower() != ".mid") continue;

            var songName = Path.GetFileNameWithoutExtension(song);
            var newButton = new Button();
            Debug.Log($"Found song: {songName}");


            // Modify button values.
            newButton.text = songName;
            newButton.AddToClassList(SONG_SELECTOR_CLASS_NAME);

            // Register event.
            newButton.clicked += () =>
            {
                Debug.Log($"Song '{songName}' selected.");
                SceneParameters.SetSongPath(song);
            };

            // Add to container.
            songListContainer.Add(newButton);
        }

        rootContainer.Add(songListContainer);
    }
}