using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Runway : MonoBehaviour
{
    short[] NoteRange;
    float NoteSpeedCoeff;
    LinkedList<GameObject>[] Lanes;
    
    /// <summary>
    /// Creates a new runway.
    /// </summary>
    /// <param name="Range">Range of note numbers to display.</param>
    /// <param name="NoteSpeed">Coefficent of speed for notes.</param>
    public Runway(short[] Range, float NoteSpeed)
    {
        print($"Initalizing runway. Note Range: {Range[0]} - {Range[1]}");
        NoteRange = Range;
        NoteSpeedCoeff = NoteSpeed;

        // Init lanes for managed notes.
        Lanes = new LinkedList<GameObject>[NoteRange[1] - NoteRange[0] + 1]; // Length = Number of notes to represent.
        
        for (int i = 0; i < Lanes.Length; i++) 
        {
            Lanes[i] = new LinkedList<GameObject>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
