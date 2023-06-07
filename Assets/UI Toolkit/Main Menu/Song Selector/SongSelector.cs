using MainStartMenu;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class SongSelector : MonoBehaviour, IDocHandler
{
    #region Constants
    private const string SONG_SELECTOR_CLASS_NAME = "Song-Select-Button";
    private const string SONG_LIST_CONTAINER_ID = "Song-List-Container";
    private const string SONG_SELECTOR_UI_CONTAINER = "SongSelectorContainer";
    private const string BACK_BUTTON_ID = "BackButton";
    #endregion

    #region Interface
    public void OnShow()
    {
        Debug.Log("Displaying song selector.");
    }
    public void OnHide()
    {
        Debug.Log("Hiding song selector.");
    }
    #endregion

    void Start()
    {
        (DocHandler.GetRoot(Documents.SongSelect).Q(BACK_BUTTON_ID) 
            as Button).clicked += BackButtonPressed;
    }

    #region Methods
    void BackButtonPressed()
    {
        Debug.Log("Back button pressed.");
        DocHandler.ReturnToPrev();
    }

    void DisplaySongSettings(string song)
    {
        Debug.Log($"Displaying settings for {song}");
    }

    void RefreshSongsList(string songsFolderPath)
    {
        var rootContainer = DocHandler.GetRoot(Documents.SongSelect).Q(SONG_SELECTOR_UI_CONTAINER);
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
                DisplaySongSettings(song);
            };

            // Add to container.
            songListContainer.Add(newButton);
        }

        rootContainer.Add(songListContainer);
    }
    #endregion
}
