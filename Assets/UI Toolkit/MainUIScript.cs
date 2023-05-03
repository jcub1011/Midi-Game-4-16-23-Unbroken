using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
/*
public class MainUIScript : MonoBehaviour
{
    private Button _playButton;
    private Button _settingsButton;
    private string _midiBaseDirectory = $"{Directory.GetCurrentDirectory()}\\Assets\\MidiFiles";
    private VisualElement _root;
    private VisualElement _mainMenu;
    private VisualElement _songSelector;
    private VisualElement _settings;
    public GameObject Midimanager;

    // Reference: https://youtu.be/NQYHIH0BJbs

    private void OnEnable()
    {
        // Get root.
        _root = GetComponent<UIDocument>().rootVisualElement;

        // Get panels.
        _mainMenu = _root.Q("MainMenu");
        _songSelector = _root.Q("SongSelector");
        _settings = _root.Q("Settings");

        // Enable only main menu.
        _mainMenu.style.display = DisplayStyle.Flex;
        _songSelector.style.display = DisplayStyle.None;
        _settings.style.display = DisplayStyle.None;

        // Get buttons.
        _playButton = _mainMenu.Q("PlayButton") as Button;
        _settingsButton = _mainMenu.Q("SettingsButton") as Button;

        // Assign events.
        _playButton.RegisterCallback<ClickEvent>(OpenSelectSongScreen);
    }

    private void ShowSongList(List<string> songList)
    {
        // Display only song selector.
        _songSelector.style.display = DisplayStyle.Flex;
        _mainMenu.style.display = DisplayStyle.None;
        _settings.style.display = DisplayStyle.None;

        print($"Songs to add: {songList.Count}");

        foreach (var song in songList )
        {
            var button = new Button();
            button.text = song;
            button.RegisterCallback<ClickEvent, string>(PlaySong, song);
            _songSelector.Add(button);
            print($"Added {song} to song list.");
        }
    }

    private void OpenSelectSongScreen(ClickEvent evt)
    {
        print("Getting song list.");
        ShowSongList(GetSongNames(_midiBaseDirectory));
    }

    private void PlaySong(ClickEvent evt, string songName)
    {
        Midimanager.GetComponent<MidiHandler>().StartSongPlayback(songName, 6, 400f);
        _songSelector.style.display = DisplayStyle.None;
    }

    private List<string> GetSongNames(string folderDirectory)
    {
        // Valid extension.
        const string VALIDSONGEXTENSION = ".mid";

        List<string> songNames = new();

        // Loops through every file and adds midi files to the song names list.
        foreach (var file in Directory.EnumerateFiles(folderDirectory))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var extension = Path.GetExtension(file);

            // print($"File name: {name}, File extension: {extension}");

            if (extension.ToLower() == VALIDSONGEXTENSION)
            {
                songNames.Add(name);
            }
        }

        // Loops through every folder and gets the midi files contained in them.
        foreach (var folder in Directory.EnumerateDirectories(folderDirectory))
        {
            songNames.AddRange(GetSongNames(folder));
        }

        return songNames;
    }
}
*/