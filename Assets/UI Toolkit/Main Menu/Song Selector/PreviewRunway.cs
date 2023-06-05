using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewRunway : MonoBehaviour
{
    #region Properties
    RunwayBase _runway;
    public GameObject LanePrefab;
    #endregion

    #region Methods
    public void Initalize(List<NoteEvtData> notes, float strikeBarHeight, float msToReachStrikeBar)
    {
        _runway?.Clear();
        float height = Camera.main.orthographicSize * 2f;
        float width = height * Camera.main.aspect;
        float[] dimensions = new float[2] { width, height };

        _runway = new(notes, dimensions, strikeBarHeight, 
            msToReachStrikeBar, transform, LanePrefab);
    }
    #endregion
}
