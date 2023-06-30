using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MIDIGame.UI.Documents;

namespace MIDIGame.UI
{
    public class MenuMain : MonoBehaviour, IDocHandler
    {
        #region IDs
        const string STRT_BTN = "StartButton";
        const string SETTN_BTN = "SettingsButton";
        #endregion

        #region Interface
        public void OnShow()
        {
            Debug.Log("Displaying main menu.");
        }

        public void OnHide()
        {
            Debug.Log("Hiding main menu.");
        }

        public void OnDocAdd()
        {
            Debug.Log("Main menu panel added.");

            // Register Buttons.
            var mainMenu = DocHandler.GetRoot(Documents.DocNames.Main);
            var start = mainMenu.Q(STRT_BTN) as Button;
            var settings = mainMenu.Q(SETTN_BTN) as Button;

            start.clicked += StartButtonPressed;
            settings.clicked += SettingsButtonPressed;
        }

        public void OnDocRemove()
        {
            Debug.Log("Start menu panel removed.");
        }
        #endregion

        #region Methods
        void StartButtonPressed()
        {
            Debug.Log("Start button pressed.");
            DocHandler.DisplayDoc(Documents.DocNames.SongSelect);
        }

        void SettingsButtonPressed()
        {
            Debug.Log("Settings button pressed.");
        }
        #endregion
    }
}