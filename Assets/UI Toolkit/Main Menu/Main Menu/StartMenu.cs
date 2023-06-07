using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class StartMenu : MonoBehaviour
{
    #region IDs

    #endregion

    void Start()
    {
        // Get Documents
        MainMenuDocHandler.Add("Main", transform.GetComponent<UIDocument>());
        MainMenuDocHandler.Add("Song Selector", transform.GetChild(0).GetComponent<UIDocument>());
        MainMenuDocHandler.Add("Song Settings", transform.GetChild(1).GetComponent<UIDocument>());
        MainMenuDocHandler.Add("Preview", transform.GetChild(2).GetComponent<UIDocument>());

        // Show Main.
        MainMenuDocHandler.Show("Main");

        // Register Buttons.

    }
}