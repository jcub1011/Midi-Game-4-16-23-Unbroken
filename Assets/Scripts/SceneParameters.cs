using UnityEngine;

namespace MIDIGame.Params
{
    public static class SceneParameters
    {
        private static string SongPath;

        public static void SetSongPath(string path)
        {
            Debug.Log("Setting song path.");
            SongPath = path;
        }

        public static string GetSongPath()
        {
            Debug.Log("Retrieving song path.");
            return SongPath;
        }
    }
}