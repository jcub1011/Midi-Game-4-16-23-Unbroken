using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class SongSelector : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _rootContainer;
    private const string SONG_SELECTOR_CLASS_NAME = "Song-Select-Button";

    #region Getter and Setter Methods
    private bool Visible
    {
        set
        {
            _rootContainer.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
    #endregion

    void Start()
    {
        _root = transform.GetComponent<UIDocument>().rootVisualElement;
        _rootContainer = _root.Q("SongSelectorContainer");
        HideList();
    }

    #region Methods
    void LoadSongsList(string songsFolderPath)
    {
        const string SONG_LIST_CONTAINER = "Song-List-Container";

        print("Getting songs list.");
        // Get list of songs.
        List<string> songList = Directory.EnumerateFiles(songsFolderPath).ToList();
        print($"Files in midi directory: {songList.Count}");

        // Clear existing buttons.
        var oldContainer = _rootContainer.Q(SONG_LIST_CONTAINER);
        if (oldContainer != null) _root.Remove(oldContainer);


        // Make new container for buttons.
        var songListContainer = new ScrollView
        {
            name = SONG_LIST_CONTAINER
        };
        // Style
        songListContainer.style.flexGrow = 1;
        songListContainer.style.marginBottom = 10;
        songListContainer.style.marginLeft = 10;
        songListContainer.style.marginRight = 10;

        // Add new buttons.
        foreach (var song in songList)
        {
            // Skip if not a midi file.
            if (Path.GetExtension(song).ToLower() != ".mid") continue;

            var songName = Path.GetFileNameWithoutExtension(song);
            var newButton = new Button();
            print($"Found song: {songName}");


            // Modify button values.
            newButton.text = songName;
            newButton.AddToClassList(SONG_SELECTOR_CLASS_NAME);

            // Register event.
            newButton.clicked += () =>
            {
                print($"Song '{songName}' selected.");
            };

            // Add to container.
            songListContainer.Add(newButton);
        }

        _rootContainer.Add(songListContainer);
    }

    public void DisplaySongList()
    {
        print("Displaying song list.");
        Visible = true;

    }

    public void DisplaySongList(string songsFolderPath)
    {
        DisplaySongList();
        LoadSongsList(songsFolderPath);
    }

    public void HideList()
    {
        print("Hiding song list.");
        Visible = false;
    }
    #endregion
}
