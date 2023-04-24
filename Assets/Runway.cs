using Melanchall.DryWetMidi.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct NoteInfo
{
    public short NoteNumber;
    public float NoteLength;
    public float TimePosition;
}

struct NoteBlock
{
    public GameObject Note;
    public float TimePosition;
    public float PositionOffset;
}

public class Runway : MonoBehaviour
{
    short[] NoteRange;
    float NoteSpeedCoeff; // How far the note moves per milisecond.
    float PlaybackSpeed = 1; // Multiplier to apply on top of NoteSpeedCoeff.
    LinkedList<NoteBlock>[] Lanes;
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
    /// <param name="StrikebarHeight">The height of the strikebar from the bottom of the runway.</param>
    public void Init(short[] Range, float NoteSpeed, float[] Dimensions, float StrikebarHeight)
    {
        print($"Initalizing runway. Note Range: {Range[0]} - {Range[1]}");
        print($"Note speed: {NoteSpeed} (units/milisecond)");
        NoteRange = Range;
        NoteSpeedCoeff = NoteSpeed;
        Height = Dimensions[1];
        Width = Dimensions[0];
        NoteWidth = Width / (float)(Range[1] - Range[0] + 1);
        StrikeBarHeight = StrikebarHeight; // Height above the floor.
        print($"Note Width: {NoteWidth}");

        // Init lanes for managed notes.
        Lanes = new LinkedList<NoteBlock>[NoteRange[1] - NoteRange[0] + 1]; // Length = Range of notes to represent.
        
        for (int i = 0; i < Lanes.Length; i++) 
        {
            Lanes[i] = new LinkedList<NoteBlock>();
        }

        // Create Strike bar
        StrikeBar.transform.localScale = new Vector3(Width, (float)0.5, 0);
        var barY = -Height / 2 + StrikeBarHeight - StrikeBar.transform.localScale.y;
        StrikeBar.transform.localPosition = new Vector3(0, barY, 1);
        StrikeBar.transform.GetComponent<SpriteRenderer>().enabled = true;
    }

    /// <summary>
    /// Initalizes a runway.
    /// </summary>
    /// <param name="Range">Range of notes where first is min and last is max. [min, max]</param>
    /// <param name="Dimensions">Height and width of runway in unity units. [width, height]</param>
    /// <param name="StrikebarHeight">Height of strikebar from the bottom of the runway in unity units.</param>
    /// <param name="QuarterNotesLeadup">Number of quarter notes that can be seen before first note touches the strikebar.</param>
    /// <param name="Tempo">The tempo map of the song.</param>
    /// <param name="SpeedMulti">Multiplier of speed. 1 is normal speed.</param>
    public void Init(short[] Range, float[] Dimensions, float StrikebarHeight, short QuarterNotesLeadup, TempoMap Tempo, float SpeedMulti = 1)
    {
        print($"Initalizing runway. Note Range: {Range[0]} - {Range[1]}");
        // Init private members.
        NoteRange = Range;
        Width = Dimensions[0];
        Height = Dimensions[1];
        PlaybackSpeed = 1;
        StrikeBarHeight = StrikebarHeight;

        // Get notespeed.
        var DistToStrikebar = Height * 2 - StrikeBarHeight;
        var TimeSpanQNotes = new MusicalTimeSpan(QuarterNotesLeadup);
        var MsToReachStrikeBar = (float)TimeConverter.ConvertTo<MetricTimeSpan>(TimeSpanQNotes, Tempo).TotalMilliseconds;
        NoteSpeedCoeff = DistToStrikebar / MsToReachStrikeBar * SpeedMulti; // units/ms
        print($"Note speed: {NoteSpeedCoeff} (units/milisecond)");

        // Get note width.
        var noteRange = Range[1] - Range[0] + 1;
        NoteWidth = Width / noteRange;

        // Init strike bar.
        StrikeBar.transform.localScale = new Vector3(Width, (float)0.5, 0);
        var barY = - Height / 2 + StrikeBarHeight - StrikeBar.transform.localScale.y;
        StrikeBar.transform.localPosition = new Vector3(0, barY, 1);
        StrikeBar.transform.GetComponent<SpriteRenderer>().enabled = true;
    }

    /// <summary>
    /// Inserts the given note to the proper lane. NoteLength is in miliseconds.
    /// </summary>
    /// <param name="NoteNumber"></param>
    /// <param name="NoteLength"></param>
    /// <param name="TimePosition">The time in miliseconds since the beginning of playback.</param>
    private void InsertNoteToRunway(short NoteNumber, float NoteLength, float TimePosition)
    {
        // Check if valid note.
        if (NoteNumber < NoteRange[0] || NoteNumber > NoteRange[1])
        {
            print($"Note {NoteNumber} outside range [{NoteRange[0]}, {NoteRange[1]}].");
            return;
        }

        // Create new note.
        var NewNote = Instantiate(NotePrefab, this.transform);
        float NoteX = (float)(NoteWidth * (NoteNumber - NoteRange[0]) + NoteWidth / 2.0 - Width / 2.0); // Half width offset because anchor is in the middle.
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

        // print($"Inserting note '{NoteNumber}' to lane {NoteNumber - NoteRange[0]}.");
        var NewBlock = new NoteBlock();
        NewBlock.Note = NewNote;
        NewBlock.TimePosition = TimePosition;
        NewBlock.PositionOffset = NewNote.transform.GetChild(0).transform.localScale.y / 2;
        Lanes[NoteNumber - NoteRange[0]].AddFirst(NewBlock); // Add to managed list.
    }

    /// <summary>
    /// Adds a note to the display queue.
    /// </summary>
    /// <param name="NoteNumber">The number of the note accoring to midi standard.</param>
    /// <param name="NoteLength">The length of the note in miliseconds.</param>
    /// <param name="TimePosition">The position of the note in playback. (miliseconds)</param>
    public void AddNoteToQueue(short NoteNumber, float NoteLength, float TimePosition)
    {
        DisplayQueue.Enqueue(new NoteInfo { NoteNumber = NoteNumber, NoteLength = NoteLength, TimePosition = TimePosition });
    }

    float GetNoteSpeed()
    {
        return NoteSpeedCoeff * PlaybackSpeed;
    }

    /// <summary>
    /// Updates the position of all the notes in each lane.
    /// </summary>
    /// <param name="PlaybackTime">The time since the beginning of playback. (in miliseconds)</param>
    public void UpdateNotePosition(float PlaybackTime)
    {
        if (Lanes == null) // If lanes has not been instantiated yet.
        {
            return;
        }
        // Insert new notes.
        while (DisplayQueue.Count > 0)
        {
            var temp = DisplayQueue.Dequeue();
            InsertNoteToRunway(temp.NoteNumber, temp.NoteLength, temp.TimePosition);
        }
        // Move notes down.
        foreach (var Lane in Lanes)
        {
            // print($"Lane length: {Lane.Count}");
            // For every note in each lane.
            for (var Note = Lane.First; Note != null;)
            {
                // Shift note down.
                // Note.Value.Note.transform.position -= new Vector3(0, (float)(GetNoteSpeed() * Time.deltaTime * 1000), 0);
                // Get new position.
                var newPosition = new Vector3();
                newPosition.x = Note.Value.Note.transform.localPosition.x;
                newPosition.y = Height / 2 - (float)(NoteSpeedCoeff * (PlaybackTime - Note.Value.TimePosition)) + Note.Value.PositionOffset;
                newPosition.z = Note.Value.Note.transform.localPosition.z;

                Note.Value.Note.transform.localPosition = newPosition;

                // Check if visible and delete if not.
                if (newPosition.y < - Height / 2 - Note.Value.PositionOffset) // Below the floor.
                {
                    var temp = Note;
                    Note = Note.Next;
                    Lane.Remove(temp);
                    Destroy(temp.Value.Note);
                    continue;
                }

                Note = Note.Next; // Increment iterator.
            }
        }
    }
}
