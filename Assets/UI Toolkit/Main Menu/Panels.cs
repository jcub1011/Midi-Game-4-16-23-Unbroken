using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Tools;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

// Delegates
public delegate void ButtonClicked();
public delegate void SongButtonClicked(string midiPath);

abstract public class GameUIPanel
{
    #region Properties
    public VisualElement Root { get; protected set; }
    public ButtonClicked OnBackButtonPress;
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
        Visible = false;
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
    private const string BACK_BUTTON_ID = "BackButton";

    public SongButtonClicked OnSongClicked;

    void BackButtonPressed()
    {
        Debug.Log("Back button pressed.");
        OnBackButtonPress?.Invoke();
    }

    public SongSelectorMenu(VisualTreeAsset doc) : base(doc)
    {
        (Root.Q(BACK_BUTTON_ID) as Button).clicked += BackButtonPressed;
    }

    public void RefreshSongsList(string songsFolderPath)
    {
        var rootContainer = Root.Q(SONG_SELECTOR_UI_CONTAINER);
        Debug.Log("Getting songs list.");

        List<string> songList = Directory.EnumerateFiles(songsFolderPath).ToList();
        Debug.Log($"Files in midi directory: {songList.Count}");

        // Remove existing list container.
        var oldContainer = rootContainer.Q(SONG_LIST_CONTAINER_ID);
        if (oldContainer != null) rootContainer.Remove(oldContainer);

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
                OnSongClicked?.Invoke(song);
            };

            // Add to container.
            songListContainer.Add(newButton);
        }

        rootContainer.Add(songListContainer);
    }
}

public class SongAdjustMenu : GameUIPanel
{
    #region Constants
    const string SETTINGS_SCROLLER_ID = "SettingsScroller";
    const string TITLE_ID = "SongNameSettings";
    const string PLAYBACK_DROPDOWN_ID = "PlaybackSpeed";
    const string TRACK_SELECT_CONTAINER_ID = "TrackSelectContainer";
    const string PLAY_BUTTON_ID = "PlayButton";
    const string PREVIEW_BUTTON_ID = "PreviewButton";
    const string BACK_BUTTON_ID = "BackButton";
    #endregion

    #region Properties
    PreviewUI _preview;
    TrackChunk[] _tracksToPlay;
    List<TrackChunk> _tracks;
    TempoMap _tempoMap;
    #endregion

    #region Constructors
    public SongAdjustMenu(VisualTreeAsset settingsDoc, VisualTreeAsset previewDoc) : base(settingsDoc) 
    {
        // Init back button.
        var temp = Root.Q(BACK_BUTTON_ID) as Button;
        temp.clicked += () => OnBackButtonPress.Invoke();

        // Init preview button.
        temp = Root.Q(PREVIEW_BUTTON_ID) as Button;
        temp.clicked += ShowPreview;

        // Init play button.
        temp = Root.Q(PLAY_BUTTON_ID) as Button;
        temp.clicked += () => { Debug.Log("Play button clicked."); };

        // Init preview UI.
        _preview = new PreviewUI(previewDoc);
        Root.Add(_preview.Root);
        _preview.OnBackButtonPress += HidePreview;
    }
    #endregion

    #region Public Methods
    public void LoadSongSettings(string midiPath)
    {
        MidiFile midiFile = MidiFile.Read(midiPath);
        _tempoMap = midiFile.GetTempoMap();
        ClearSettings();
        // Init title.
        var title = Root.Q(TITLE_ID) as Label;

        // Init playback speed dropdown.
        List<string> playbackSpeeds = new();

        for (float i = 0f; i <= 10f; i += 0.25f)
        {
            playbackSpeeds.Add(i.ToString("0.##") + "x");
        }

        (Root.Q(PLAYBACK_DROPDOWN_ID) as DropdownField).choices = playbackSpeeds;

        // Init track select container.
        var trackSelectContainer = Root.Q(TRACK_SELECT_CONTAINER_ID);

        var trackNames = new List<string>();
        _tracks = midiFile.GetTrackChunks().ToList();
        _tracksToPlay = new TrackChunk[_tracks.Count];
        
        foreach (var track in _tracks)
        {
            var names = track.Events.OfType<SequenceTrackNameEvent>();
            Debug.Log($"Name count: {names.Count()}");
            if (names.Count() > 0) trackNames.Add(names.First().Text);
            else trackNames.Add("Untitled Track");
        }

        // First name is name of sequence so skip that.
        for (int i = 1; i < _tracks.Count; i++)
        {
            var newToggle = new Toggle();
            newToggle.RegisterCallback<ChangeEvent<bool>, int>(AdjustTrackPlayList, i);
            newToggle.text = $"{i}: {trackNames[i]}";
            trackSelectContainer.Add(newToggle);
        }

        // Set settings title.
        if (trackNames.Count() > 0)
        {
            var defaultNames = new List<string>()
            {
                "Untitled Track",
                "",
                " "
            };

            if (defaultNames.Contains(trackNames[0]))
            {
                title.text = $"{Path.GetFileNameWithoutExtension(midiPath)} Settings";
            }
            else title.text = $"{trackNames[0]} Settings";
        }
        else title.text = "Song Settings";
    }

    void AdjustTrackPlayList(ChangeEvent<bool> evt, int index)
    {
        Debug.Log($"Toggle {index} changed to {evt.newValue}.");
        // Add or remove the track.
        _tracksToPlay[index] = evt.newValue ? _tracks[index] : null;
    }

    void ShowPreview()
    {
        Debug.Log("Preview button clicked.");
        Root.Q(SETTINGS_SCROLLER_ID).style.display = DisplayStyle.None;

        // Get notes to display.
        var tracks = new List<TrackChunk>();
        var notes = new List<NoteEvtData>();
        MidiFile tempMidi;

        foreach (var track in _tracksToPlay)
        {
            if (track == null) continue;
            tracks.Add(track);
        }

        tempMidi = new MidiFile(tracks); // For sorting notes appropriately.

        foreach(var note in tempMidi.GetNotes())
        {
            var temp = new NoteEvtData();
            temp.Number = note.NoteNumber;
            temp.OnTime = (float)note.TimeAs<MetricTimeSpan>(_tempoMap).TotalMilliseconds;
            temp.OffTime = (float)note.EndTimeAs<MetricTimeSpan>(_tempoMap).TotalMilliseconds;
            temp.Length = (float)note.LengthAs<MetricTimeSpan>(_tempoMap).TotalMilliseconds;

            notes.Add(temp);
        }


        _preview.Show(notes);
    }

    void HidePreview()
    {
        Debug.Log("Hide preview pressed.");
        Root.Q(SETTINGS_SCROLLER_ID).style.display = DisplayStyle.Flex;
    }
    #endregion

    #region Private methods
    void ClearSettings()
    {
        if (Root.Q(TITLE_ID) is Label title) title.text = null;
        if (Root.Q(PLAYBACK_DROPDOWN_ID) is DropdownField dropdown)
        {
            dropdown.choices = null;
        }
        if (Root.Q(TRACK_SELECT_CONTAINER_ID) is VisualElement container)
        {
            container.Clear();
        }
    }
    #endregion
}

public class PreviewUI : GameUIPanel
{
    #region Constants
    const string SLIDER = "TimeSlider";
    const string BACK_BUTTON = "BackButton";
    const string PREVIEW_RUNWAY_NAME = "PreviewRunway";
    #endregion

    #region Properties
    PreviewRunway _runway;
    #endregion

    #region Methods
    void UpdatePlaybackTime(ChangeEvent<float> evt)
    {
        Debug.Log($"New Time: {evt.newValue}");
        _runway.UpdateTime(evt.newValue);
    }

    public void Show(List<NoteEvtData> notes, float strikeBarHeight = 0.2f, 
        float msLeadup = 4000f, float time = 0f)
    {
        Debug.Log("Displaying preview UI.");
        Visible = true;
        _runway.Initalize(notes, strikeBarHeight, msLeadup, time);
        Debug.Log($"Playback started @ {time}ms");
    }

    void OnBackButtonClick()
    {
        OnBackButtonPress?.Invoke();
        _runway.Unload();
        Visible = false;
    }
    #endregion

    #region Constructors
    public PreviewUI(VisualTreeAsset doc) : base(doc)
    {
        var slider = Root.Q(SLIDER) as Slider;
        var backButton = Root.Q(BACK_BUTTON) as Button;

        slider.RegisterValueChangedCallback(UpdatePlaybackTime);
        backButton.clicked += OnBackButtonClick;

        // Init Preview Runway.
        _runway = UnityEngine.GameObject.Find(PREVIEW_RUNWAY_NAME).GetComponent<PreviewRunway>();
    }
    #endregion
}