using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Runway : MonoBehaviour
{
    short[] NoteRange;
    float NoteSpeedCoeff; // How far the note moves per milisecond.
    float PlaybackSpeed = 1; // Multiplier to apply on top of NoteSpeedCoeff.
    LinkedList<GameObject>[] Lanes;
    float NoteWidth; // In unity units.
    float Height; // In unity units.
    float Width; // In unity units.
    float StrikeBarHeight; // In unity units.
    public GameObject NotePrefab;
    
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
        StrikeBarHeight = 1; // Height above the floor.

        // Init lanes for managed notes.
        Lanes = new LinkedList<GameObject>[NoteRange[1] - NoteRange[0] + 1]; // Length = Number of notes to represent.
        
        for (int i = 0; i < Lanes.Length; i++) 
        {
            Lanes[i] = new LinkedList<GameObject>();
        }
    }

    void AddNoteToLane(short NoteNumber, long NoteLength)
    {
        if (NoteNumber < NoteRange[0] || NoteNumber > NoteRange[1])
        {
            print($"Note {NoteNumber} outside range [{NoteRange[0]}, {NoteRange[1]}].");
            return;
        }
        var NewNote = Instantiate(NotePrefab);
        float NoteX = (float)(NoteWidth * NoteNumber + NoteWidth / 2.0); // Half width offset because anchor is in the middle.
        float NoteY = (float)(NoteLength * GetNoteSpeed() / 2.0); // Half height offset because anchor is in the middle.
        NewNote.transform.position = new Vector3(NoteX, NoteY);
    }

    float GetNoteSpeed()
    {
        return NoteSpeedCoeff * PlaybackSpeed;
    }

    // Update is called once per frame
    void Update()
    {

        // Move notes down.
        foreach (var Lane in Lanes)
        {
            // For every note in each lane.
            for (var Note = Lane.First; Note != null;)
            {
                // Shift note down.
                Note.Value.transform.position -= new Vector3(0, (float)(GetNoteSpeed() * Time.deltaTime / 1000), 0);

                // Check if visible and delete if not.
                if (Note.Value.transform.position.y < -Height) // Below the floor.
                {
                    var temp = Note;
                    Note = Note.Next;
                    Lane.Remove(temp);
                    continue;
                }

                Note = Note.Next; // Increment iterator.
            }
        }
    }
}
