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

public class PanelManager
{
    #region Properties
    private Dictionary<string, GameUIPanel> _panels;
    private string _activePanel;
    private Stack<string> _panelHistory;
    #endregion

    #region Methods
    /// <summary>
    /// Adds a panel to manage.
    /// </summary>
    /// <param name="panelName">Name to assign panel.</param>
    /// <param name="panel">Panel to add.</param>
    public void AddPanel(string panelName, GameUIPanel panel)
    {
        Debug.Log($"Adding panel '{panelName}'.");
        panel.Visible = false;
        _panels.Add(panelName, panel);
    }

    /// <summary>
    /// Unmanages the given panel.
    /// </summary>
    /// <param name="panelName">Name of panel to remove.</param>
    public void RemovePanel(string panelName)
    {
        Debug.Log($"Removing panel '{panelName}'.");
        if (panelName == _activePanel)
        {
            _activePanel = null;
        }

        _panels.Remove(panelName);
    }

    /// <summary>
    /// Gets a list of roots from all the managed panels.
    /// </summary>
    /// <returns>List of visual elements.</returns>
    public List<VisualElement> GetPanelRoots()
    {
        var roots = new List<VisualElement>();

        foreach (var panel in _panels.Values)
        {
            roots.Add(panel.Root);
        }

        return roots;
    }

    /// <summary>
    /// Returns the given panel.
    /// </summary>
    /// <param name="panelName">Name of panel to get.</param>
    /// <returns>The panel.</returns>
    public GameUIPanel GetPanel(string panelName)
    {
        return _panels[panelName];
    }

    /// <summary>
    /// Replaces the currently active panel with the given panel.
    /// </summary>
    /// <param name="panelName">Name of panel to make active.</param>
    public void MakeActive(string panelName)
    {
        Debug.Log($"Making panel active '{panelName}'.");
        if (_activePanel == panelName) return;
        _panels[panelName].Visible = true;

        if (_activePanel != null)
        {
            _panels[_activePanel].Visible = false;
            _panelHistory.Push(_activePanel);
        }

        _activePanel = panelName;
    }

    /// <summary>
    /// Makes the previous panel active again.
    /// </summary>
    /// <returns>If there is a previous panel to return to.</returns>
    public bool PreviousPanel()
    {
        Debug.Log($"Returning to previous panel.");

        // Remove panels that no longer exist.
        while (!_panels.ContainsKey(_panelHistory.Peek()))
        {
            _panelHistory.Pop();
        }

        if (_panelHistory.Count == 0) return false;
        _panels[_panelHistory.Peek()].Visible = true;
        _panels[_activePanel].Visible = false;
        _activePanel = _panelHistory.Pop();
        return true;
    }

    /// <summary>
    /// Clears the panel history.
    /// </summary>
    public void ClearHistory()
    {
        Debug.Log($"Clearing panel history.");
        _panelHistory.Clear();
    }
    #endregion

    #region Constructors
    public PanelManager()
    {
        _panels = new();
        _activePanel = null;
        _panelHistory = new();
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