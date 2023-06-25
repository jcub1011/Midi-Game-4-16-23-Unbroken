using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Melanchall.DryWetMidi.Interaction;

public class PreviewRunway : MonoBehaviour
{
    #region Properties
    RunwayBase _runway;
    public GameObject LanePrefab;
    public GameObject WhiteNotePrefab;
    public GameObject BlackNotePrefab;
    #endregion

    #region Methods
    public void Initalize(List<Note> notes, float strikeBarHeight, long tickToReachStrikeBar,
        long startTick)
    {
        if (notes.Count == 0) throw new System.Exception("There are no notes to display.");
        Debug.Log($"Preview has {notes.Count} notes.");
        UpdateTime(startTick);

        _runway?.Clear();
        float height = Camera.main.orthographicSize * 2f;
        float width = height * Camera.main.aspect;
        float[] dimensions = new float[2] { width, height };

        _runway = new(notes, dimensions, strikeBarHeight, 
            tickToReachStrikeBar, transform, LanePrefab, WhiteNotePrefab, BlackNotePrefab);
    }

    public void Unload()
    {
        _runway?.Clear();
    }

    public void UpdateTime(long playbackTick)
    {
        _runway?.UpdateRunway(playbackTick);
    }
    #endregion
}
