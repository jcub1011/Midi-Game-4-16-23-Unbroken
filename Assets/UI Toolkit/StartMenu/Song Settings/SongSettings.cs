using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MainStartMenu;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using UnityEngine.UIElements;
using System.IO;
using System.Linq;
using Unity.VisualScripting;

public class SongSettings : MonoBehaviour, IDocHandler, IFileInput
{
    #region IDs
    const string SETTINGS_SCROLLER_ID = "SettingsScroller";
    const string TITLE_ID = "SongNameSettings";
    const string PLAYBACK_DROPDOWN_ID = "PlaybackSpeed";
    const string TRACK_SELECT_CONTAINER_ID = "TrackSelectContainer";
    const string PLAY_BUTTON_ID = "PlayButton";
    const string PREVIEW_BUTTON_ID = "PreviewButton";
    const string BACK_BUTTON_ID = "BackButton";
    #endregion

    #region Properties
    TrackChunk[] _tracksToPlay;
    List<TrackChunk> _tracks;
    TempoMap _tempoMap;
    VisualElement _root;
    #endregion

    #region Interface
    public void OnShow()
    {
        Debug.Log("Displaying song settings.");
        _root = DocHandler.GetRoot(Documents.SongSetts);

        // Register buttons.
        var temp = _root.Q(BACK_BUTTON_ID) as Button;
        temp.clicked += DocHandler.ReturnToPrev;

        temp = _root.Q(PREVIEW_BUTTON_ID) as Button;
        temp.clicked += OnPreviewButtonClick;

        temp = _root.Q(PLAY_BUTTON_ID) as Button;
        temp.clicked += OnPlayButtonClick;
    }

    public void OnShow(string filePath)
    {
        Debug.Log($"[{filePath}] is being opened.");
        LoadSongSettings(filePath);
    }

    public void OnHide()
    {
        Debug.Log("Hiding song settings.");
    }

    public void OnDocAdd()
    {
        Debug.Log("Song settings panel added.");
    }

    public void OnDocRemove()
    {
        Debug.Log("Song settings panel removed.");
    }
    #endregion

    #region Methods
    void OnPreviewButtonClick()
    {
        Debug.Log("Preview button clicked.");


    }

    void OnPlayButtonClick()
    {
        Debug.Log("Play button clicked.");
    }

    void ClearSettings()
    {
        if (_root.Q(TITLE_ID) is Label title) title.text = null;
        if (_root.Q(PLAYBACK_DROPDOWN_ID) is DropdownField dropdown)
        {
            dropdown.choices = null;
        }
        if (_root.Q(TRACK_SELECT_CONTAINER_ID) is VisualElement container)
        {
            container.Clear();
        }
    }

    void InitTitle(string midiPath)
    {
        var title = _root.Q(TITLE_ID) as Label;
        title.text = $"{Path.GetFileNameWithoutExtension(midiPath)} Settings";
    }

    void AdjustTrackPlayList(ChangeEvent<bool> evt, int index)
    {
        Debug.Log($"Toggle {index} changed to {evt.newValue}.");
        // Add or remove the track.
        _tracksToPlay[index] = evt.newValue ? _tracks[index] : null;
    }

    List<string> GetTrackNames(MidiFile midiFile)
    {
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

        return trackNames;
    }

    void InitTrackSelectContainer(MidiFile midiFile)
    {
        // Init track select container.
        var trackSelectContainer = _root.Q(TRACK_SELECT_CONTAINER_ID);
        var trackNames = GetTrackNames(midiFile);

        // First name is name of sequence so skip that.
        for (int i = 1; i < _tracks.Count; i++)
        {
            var newToggle = new Toggle();
            newToggle.RegisterCallback<ChangeEvent<bool>, int>(AdjustTrackPlayList, i);
            newToggle.text = $"{i}: {trackNames[i]}";
            trackSelectContainer.Add(newToggle);
        }
    }

    void InitSpeedDropdown()
    {
        // Init playback speed dropdown.
        List<string> playbackSpeeds = new();

        for (float i = 0f; i <= 10f; i += 0.25f)
        {
            playbackSpeeds.Add(i.ToString("0.##") + "x");
        }

        (_root.Q(PLAYBACK_DROPDOWN_ID) as DropdownField).choices = playbackSpeeds;
    }

    void LoadSongSettings(string midiPath)
    {
        MidiFile midiFile = MidiFile.Read(midiPath);
        _tempoMap = midiFile.GetTempoMap();

        ClearSettings();
        InitTitle(midiPath);
        InitSpeedDropdown();
        InitTrackSelectContainer(midiFile);
    }
    #endregion
}
