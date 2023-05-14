using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaybackManager : MonoBehaviour
{
    private BasePlaybackEngine playback;
    // Start is called before the first frame update
    void Start()
    {
        playback = new BasePlaybackEngine(GameData.SongToPlay);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
