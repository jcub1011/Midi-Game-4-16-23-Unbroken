using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
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
        var midiFolder = Application.dataPath + "/MidiFiles";
        transform.GetComponentInChildren<SongSelector>().DisplaySongList(midiFolder);
        Visible = false;
    }

    void OnSettingsClicked()
    {
        print("Settings clicked.");
    }
}

public delegate void ButtonClicked();

public class MainMenu
{
    #region Properties
    public ButtonClicked OnStartClicked;
    public ButtonClicked OnSettingsClicked;
    public VisualElement Root
    {
        get
        {
            return Root;
        }
        set
        {
            Root = value;
        }
    }
    #endregion

    public bool Visible
    {
        get
        {
            return Root.style.display == DisplayStyle.Flex;
        }

        set
        {
            Root.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public void InitButtons()
    {
        // Get element references.
        var startButton = Root.Q("StartButton") as Button;
        var settingsButton = Root.Q("SettingsButton") as Button;

        // Register click events.
        startButton.clicked += StartButtonClicked;
        settingsButton.clicked += SettingsButtonClicked;
    }

    void StartButtonClicked()
    {
        Debug.Log("Start button clicked.");
        OnStartClicked.Invoke();
    }

    void SettingsButtonClicked()
    {
        Debug.Log("Settings button clicked.");
        OnSettingsClicked.Invoke();
    }
}