using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public static class SceneParameters
{
    private static string SongPath;

    public static void SetSongPath(string path)
    {
        Debug.Log("Setting song path.");
        SongPath = path;
    }
}
