using UnityEngine;
using UnityEngine.UIElements;
using MIDIGame.UI.Documents;

namespace MIDIGame.UI
{
    public class StartMenuInitalizer : MonoBehaviour
    {
        #region Documents
        public GameObject TheMainMenu;
        public GameObject TheSongSelector;
        public GameObject TheSongSettings;
        public GameObject ThePreview;
        #endregion

        void Start()
        {
            // Get Documents
            DocHandler.Add(DocNames.Main,
                TheMainMenu.GetComponent<UIDocument>(),
                TheMainMenu.GetComponent<MenuMain>());
            DocHandler.Add(DocNames.SongSelect,
                TheSongSelector.GetComponent<UIDocument>(),
                TheSongSelector.GetComponent<SongSelector>());
            DocHandler.Add(DocNames.SongSetts,
                TheSongSettings.GetComponent<UIDocument>(),
                TheSongSettings.GetComponent<SongSettings>());
            DocHandler.Add(DocNames.Preview,
                ThePreview.GetComponent<UIDocument>(),
                ThePreview.GetComponent<Preview>());

            // Show Main.
            DocHandler.DisplayDoc(DocNames.Main);
        }
    }
}
