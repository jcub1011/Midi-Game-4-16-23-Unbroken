using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaybackManager : MonoBehaviour
{
    private CustomPlaybackEngine playback;
    // Start is called before the first frame update
    void Start()
    {
        playback = new CustomPlaybackEngine(GameData.SongToPlay);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
