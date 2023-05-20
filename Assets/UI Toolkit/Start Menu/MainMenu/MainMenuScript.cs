using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuScript : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _rootContainer;
    private Button _startButton;
    private Button _settingsButton;

    private bool Visible
    {
        set
        {
            _rootContainer.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    void Start()
    {
        // Get element references.
        _root = transform.GetComponent<UIDocument>().rootVisualElement;
        _rootContainer = _root.Q(name: "RootContainerMainMenu");
        _startButton = _root.Q("StartButton") as Button;
        _settingsButton = _root.Q("SettingsButton") as Button;

        // Register click events.
        _startButton.clicked += OnStartClicked;
        _settingsButton.clicked += OnSettingsClicked;
    }

    void OnStartClicked()
    {
        print("Start clicked.");
        Visible = false;
        var midiFolder = Application.dataPath + "/MidiFiles";
        transform.GetComponentInChildren<SongSelector>().DisplaySongList(midiFolder);
    }

    void OnSettingsClicked()
    {
        print("Settings clicked.");
    }
}
