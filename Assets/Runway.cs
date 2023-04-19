using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Runway : MonoBehaviour
{
    short[] NoteRange;
    float NoteSpeedCoeff;
    LinkedList<GameObject>[] Lanes;
    float Height; // In unity units.
    float Width; // In unity units.
    float StrikeBarHeight; // In unity units.
    
    /// <summary>
    /// Creates a new runway.
    /// </summary>
    /// <param name="Range">Range of note numbers to display.</param>
    /// <param name="NoteSpeed">Coefficent of speed for notes.</param>
    public Runway(short[] Range, float NoteSpeed, float[] Dimensions)
    {
        print($"Initalizing runway. Note Range: {Range[0]} - {Range[1]}");
        NoteRange = Range;
        NoteSpeedCoeff = NoteSpeed;
        Height = Dimensions[1];
        Width = Dimensions[0];
        StrikeBarHeight = -Height + (float)1;

        // Init lanes for managed notes.
        Lanes = new LinkedList<GameObject>[NoteRange[1] - NoteRange[0] + 1]; // Length = Number of notes to represent.
        
        for (int i = 0; i < Lanes.Length; i++) 
        {
            Lanes[i] = new LinkedList<GameObject>();
        }
    }

    void AddNoteToLane(short LaneToAddTo, long NoteLength)
    {

    }

    // Update is called once per frame
    void Update()
    {

        // Move notes down.
        foreach (var Lane in Lanes)
        {
            // For every note in each lane.
            for (var Note = Lane.First; Note != null; Note = Note.Next)
            {
                // Shift note down.
                Note.Value.transform.position -= new Vector3(0, (float)(NoteSpeedCoeff * Time.deltaTime / 1000), 0);

                // Check if visible and delete if not.
                
            }
        }
    }
}
