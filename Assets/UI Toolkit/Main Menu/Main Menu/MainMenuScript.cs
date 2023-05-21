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

public class GameUIPanel
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

    public MainMenu(VisualTreeAsset doc)
    {
        Root = doc.Instantiate();

        // Get element references.
        var startButton = Root.Q("StartButton") as Button;
        var settingsButton = Root.Q("SettingsButton") as Button;

        // Register click events.
        startButton.clicked += StartButtonClicked;
        settingsButton.clicked += SettingsButtonClicked;

        // Make it take the entire sceen.
        Root.style.flexGrow = 1;
    }
}