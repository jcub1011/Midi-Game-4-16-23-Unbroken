using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Schema;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour
{
    private Button _selectSongButton;
    private Button _openSettingsButton;

    private VisualElement _root;
    private StartMenu _startMenu;
    private VisualElement _settingsBox;
    private SongList _songsBox;

    private string _midiBaseDirectory = $"{Directory.GetCurrentDirectory()}\\Assets\\MidiFiles";

    private void OnEnable()
    {
        // Get root.
        _root = GetComponent<UIDocument>().rootVisualElement;

        // Get panels.
        _startMenu = _root.Q<VisualElement>("StartMenu");
        _startMenu.Show();
        _selectSongButton = _startMenu.visualElement.Q("SelectSong") as Button;
        _openSettingsButton = _startMenu.visualElement.Q("Settings") as Button;

        _songsBox = _root.Q<VisualElement>("SongsBox");
        _songsBox.Hide();

        // Init buttons.
        _selectSongButton.RegisterCallback<ClickEvent>(OpenSongList);
    }

    void OpenSongList(ClickEvent evt)
    {
        _songsBox.RefreshList(_midiBaseDirectory);
        _songsBox.Show();
        _startMenu.Hide();
    }
}

public class StartMenu
{
    public VisualElement visualElement { get; protected set; }

    public StartMenu(VisualElement element)
    {
        visualElement = element;
    }

    public void Hide()
    {
        visualElement.style.display = DisplayStyle.None;
    }

    public void Show()
    {
        visualElement.style.display = DisplayStyle.Flex;
    }

    public static implicit operator StartMenu(VisualElement element)
    {
        var menu = new StartMenu(element);

        return menu;
    }
}

public class SongList : StartMenu
{
    public SongList(VisualElement element) : base(element) { }

    public void RefreshList(string _midiFolder)
    {
        // Remove old list.
        foreach (var child in visualElement.Children())
        {
            if (child.ClassListContains("song-list-button")) visualElement.Remove(child);
        }

        // Create new list.
        foreach (var song in GetSongNames(_midiFolder))
        {
            var button = new Button();
            button.AddToClassList("song-list-button");
            button.text = song;
            visualElement.Add(button);
        }
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

    public static implicit operator SongList(VisualElement element)
    {
        var menu = new SongList(element);

        return menu;
    }
}
