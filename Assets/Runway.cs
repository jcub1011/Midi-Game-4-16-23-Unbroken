using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct NoteInfo
{
    public short NoteNumber;
    public float NoteLength;
}

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
    Queue<NoteInfo> DisplayQueue = new(); // Notes waiting to be displayed on next frame.
    public GameObject NotePrefab;
    public GameObject StrikeBar;
    
    /// <summary>
    /// Inits a new runway.
    /// </summary>
    /// <param name="Range">Range of note numbers to display.</param>
    /// <param name="NoteSpeed">Distance notes should travel every milisecond in unity units.</param>
    /// <param name="Dimensions">Width and height of runway in unity units.</param>
    public void Init(short[] Range, float NoteSpeed, float[] Dimensions)
    {
        print($"Initalizing runway. Note Range: {Range[0]} - {Range[1]}");
        print($"Note speed: {NoteSpeed} (units/milisecond)");
        NoteRange = Range;
        NoteSpeedCoeff = NoteSpeed;
        Height = Dimensions[1];
        Width = Dimensions[0];
        NoteWidth = Width / (float)(Range[1] - Range[0] + 1);
        StrikeBarHeight = 4; // Height above the floor.
        print($"Note Width: {NoteWidth}");

        // Init lanes for managed notes.
        Lanes = new LinkedList<GameObject>[NoteRange[1] - NoteRange[0] + 1]; // Length = Range of notes to represent.
        
        for (int i = 0; i < Lanes.Length; i++) 
        {
            Lanes[i] = new LinkedList<GameObject>();
        }

        // Create Strike bar
        StrikeBar.transform.localPosition = new Vector3(0, - Height / 2 + StrikeBarHeight, 1);
        StrikeBar.transform.localScale = new Vector3(Width, (float)0.5, 0);
        StrikeBar.transform.GetComponent<SpriteRenderer>().enabled = true;
    }

    /// <summary>
    /// Inserts the given note to the proper lane. NoteLength is in miliseconds.
    /// </summary>
    /// <param name="NoteNumber"></param>
    /// <param name="NoteLength"></param>
    private void InsertNoteToRunway(short NoteNumber, float NoteLength)
    {
        // Check if valid note.
        if (NoteNumber < NoteRange[0] || NoteNumber > NoteRange[1])
        {
            print($"Note {NoteNumber} outside range [{NoteRange[0]}, {NoteRange[1]}].");
            return;
        }
        // Create new note.
        var NewNote = Instantiate(NotePrefab, this.transform);
        float NoteX = (float)(NoteWidth * (NoteNumber - NoteRange[0]) + NoteWidth / 2.0); // Half width offset because anchor is in the middle.
        float NoteY = (float)(NoteLength * GetNoteSpeed() / 2.0 + Height / 2.0); // Half height offset because anchor is in the middle.
        NewNote.transform.position = new Vector3(NoteX, NoteY, 2);
        /// Child 0 is skin.
        /// Child 1 is perfect collider.
        /// Child 2 is good collider.
        /// Child 3 is ok collider.
        var NoteDimensions = new Vector3(NoteWidth, NoteLength * GetNoteSpeed()); // Dimensions of note.
        NewNote.transform.GetChild(0).localScale = NoteDimensions;
        NewNote.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
        NewNote.transform.GetChild(1).localScale = NoteDimensions + new Vector3(0, (float)0.01, 0); // Additional perfect collider offset.
        NewNote.transform.GetChild(2).localScale = NoteDimensions + new Vector3(0, (float)0.1, 0); // Additional good collider offset.
        NewNote.transform.GetChild(3).localScale = NoteDimensions + new Vector3(0, (float)0.2, 0); // Additional ok collider offset.

        print($"Inserting note '{NoteNumber}' to lane {NoteNumber - NoteRange[0]}.");
        Lanes[NoteNumber - NoteRange[0]].AddFirst(NewNote); // Add to managed list.
    }

    public void AddNoteToQueue(short NoteNumber, float NoteLength)
    {
        DisplayQueue.Enqueue(new NoteInfo { NoteNumber = NoteNumber, NoteLength = NoteLength });
    }

    float GetNoteSpeed()
    {
        return NoteSpeedCoeff * PlaybackSpeed;
    }

    void Update()
    {
        if (Lanes == null) // If lanes has not been instantiated yet.
        {
            return;
        }
        // Insert new notes.
        while (DisplayQueue.Count > 0)
        {
            var temp = DisplayQueue.Dequeue();
            InsertNoteToRunway(temp.NoteNumber, temp.NoteLength);
        }
        // Move notes down.
        foreach (var Lane in Lanes)
        {
            print($"Lane length: {Lane.Count}");
            // For every note in each lane.
            for (var Note = Lane.First; Note != null;)
            {
                // Shift note down.
                Note.Value.transform.position -= new Vector3(0, (float)(GetNoteSpeed() * Time.deltaTime * 1000), 0);

                // Check if visible and delete if not.
                if (Note.Value.transform.position.y < -Height) // Below the floor.
                {
                    var temp = Note;
                    Note = Note.Next;
                    Lane.Remove(temp);
                    Destroy(temp.Value);
                    continue;
                }

                Note = Note.Next; // Increment iterator.
            }
        }
    }
}
