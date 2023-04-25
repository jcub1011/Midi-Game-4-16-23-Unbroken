using Melanchall.DryWetMidi.Interaction;
using System.Collections.Generic;
using UnityEngine;

public class Runway : MonoBehaviour
{
    IntRange NoteRange;
    float NoteSpeedCoeff; // How far the note moves per milisecond.
    float MsToTouchRunway;
    Queue<NoteBlock>[] Lanes;
    float NoteWidth; // In unity units.
    float Height; // In unity units.
    float Width; // In unity units.
    float StrikeBarHeight; // In unity units.
    Queue<NoteBlock> DisplayQueue = new(); // Notes waiting to be displayed on next frame.
    public GameObject NotePrefab;
    public GameObject StrikeBar;
    private float[] ColliderOffsets = new float[3] { (float)0.05, (float)0.15, (float)0.5 };

    /// <summary>
    /// Initalizes a runway.
    /// </summary>
    /// <param name="Range">Range of notes where first is min and last is max. [min, max]</param>
    /// <param name="Dimensions">Height and width of runway in unity units. [width, height]</param>
    /// <param name="StrikebarHeight">Height of strikebar from the bottom of the runway in unity units.</param>
    /// <param name="MsToHitRunway">How long it takes to reach the strikebar.</param>
    /// <param name="SpeedMulti">Multiplier of speed. 1 is normal speed.</param>
    public void Init(IntRange Range, float[] Dimensions, float StrikebarHeight, float MsToHitRunway, float SpeedMulti = 1)
    {
        print($"Initalizing runway. Note Range: {Range.Min} - {Range.Max}");
        // Init private members.
        NoteRange = Range;
        Width = Dimensions[0];
        Height = Dimensions[1];
        StrikeBarHeight = StrikebarHeight;

        // Get notespeed.
        var DistToStrikebar = Height - StrikeBarHeight;
        MsToTouchRunway = MsToHitRunway;
        NoteSpeedCoeff = DistToStrikebar / MsToTouchRunway; // units/ms
        print($"Note speed: {NoteSpeedCoeff} (units/milisecond)");

        // Get note width.
        NoteWidth = Width / Range.Len;

        // Init strike bar.
        StrikeBar.transform.localScale = new Vector3(Width, (float)0.5, 0);
        var barY = - Height / 2 + StrikeBarHeight - StrikeBar.transform.localScale.y;
        StrikeBar.transform.localPosition = new Vector3(0, barY, 1);
        StrikeBar.transform.GetComponent<SpriteRenderer>().enabled = true;

        // Init strike bar keys.
        for (int i = 0; i < Range.Len; i++)
        {
            
        }

        // Create lanes.
        Lanes = new Queue<NoteBlock>[Range.Len];

        for (int i = 0; i < Lanes.Length; i++)
        {
            Lanes[i] = new Queue<NoteBlock>();
        }
    }

    /// <summary>
    /// Gets an x position using a note number.
    /// </summary>
    /// <param name="NoteNum">The note number.</param>
    /// <returns>X position.</returns>
    float GetNoteXPos(short NoteNum)
    {
        return (float)(NoteWidth * (NoteNum - NoteRange.Min) + NoteWidth / 2.0 - Width / 2.0); // Half width offset because anchor is in the middle.
    }

    /// <summary>
    /// Updates info necessary for notes to display properly on the runway.
    /// </summary>
    /// <param name="NewDimensions">The new dimensions of the runway.</param>
    public void UpdateNoteDisplayInfo(float[] NewDimensions)
    {
        // Update runway dimensions.
        Width = NewDimensions[0];
        Height = NewDimensions[1];
        NoteSpeedCoeff = (Height - StrikeBarHeight) / MsToTouchRunway; // units/ms
        print($"Note speed: {NoteSpeedCoeff} (units/milisecond)");

        // Update note width.
        NoteWidth = Width / NoteRange.Len;

        // Update strike bar.
        StrikeBar.transform.localScale = new Vector3(Width, (float)0.5, 0);
        var barY = -Height / 2 + StrikeBarHeight - StrikeBar.transform.localScale.y;
        StrikeBar.transform.localPosition = new Vector3(0, barY, 1);
    }

    /// <summary>
    /// Adds a note to the display queue.
    /// </summary>
    /// <param name="NoteNumber">The number of the note accoring to midi standard.</param>
    /// <param name="NoteLength">The length of the note in miliseconds.</param>
    /// <param name="TimePosition">The position of the note in playback. (miliseconds)</param>
    public void AddNoteToQueue(short NoteNumber, float NoteLength, float TimePosition)
    {
        var newNote = new NoteBlock(TimePosition, NoteLength, NoteNumber);
        DisplayQueue.Enqueue(newNote);
    }

    /// <summary>
    /// Adds a note block to its respective lane.
    /// </summary>
    /// <param name="noteBlock">The note block to add to a lane.</param>
    public void AddNoteToLane(NoteBlock noteBlock)
    {
        // Check if valid note.
        if (!NoteRange.InRange(noteBlock.NoteNumber))
        {
            print($"Note {noteBlock.NoteNumber} outside range [{NoteRange.Min}, {NoteRange.Max}].");
            return;
        }

        // Create new note.
        var NewNote = Instantiate(NotePrefab, this.transform);
        float NoteX = GetNoteXPos(noteBlock.NoteNumber);
        float NoteY = (float)(noteBlock.Length * NoteSpeedCoeff / 2.0 + Height / 2.0); // Half height offset because anchor is in the middle.
        NewNote.transform.position = new Vector3(NoteX, NoteY, 2);

        // Child 0 is skin.
        var NoteDimensions = new Vector3(NoteWidth, noteBlock.Length * NoteSpeedCoeff); // Dimensions of note.
        NewNote.transform.GetChild(0).localScale = NoteDimensions;
        NewNote.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;

        // Add to lane.
        noteBlock.SetNote(NewNote);
        Lanes[noteBlock.NoteNumber - NoteRange.Min].Enqueue(noteBlock);
    }

    /// <summary>
    /// Updates the position of all the notes in each lane.
    /// </summary>
    /// <param name="PlaybackTime">The time since the beginning of playback. (in miliseconds)</param>
    public void UpdateNotesPositions(float PlaybackTime)
    {
        if (Lanes == null)
        {
            return;
        }

        while (DisplayQueue.Count > 0)
        {
            AddNoteToLane(DisplayQueue.Dequeue());
        }

        foreach (var lane in Lanes)
        {
            foreach (var note in lane)
            {
                // Get new scale.
                var newScale = new Vector3();
                newScale.x = NoteWidth;
                newScale.y = note.Length * NoteSpeedCoeff;
                newScale.z = 1;

                // Get new position.
                var newPosition = new Vector3();
                newPosition.x = GetNoteXPos(note.NoteNumber);
                newPosition.y = Height / 2 - (float)(NoteSpeedCoeff * (PlaybackTime - note.TimePosition)) + note.NoteHeight / 2;
                newPosition.z = 2;

                note.UpdateNote(newPosition, newScale);
            }

            // Delete notes no longer visible.
            while (lane.Count > 0)
            {
                if (lane.Peek().TopY < - Height / 2) // Below the floor.
                {
                    Destroy(lane.Dequeue().GetNote());
                } else
                {
                    break;
                }
            }
        }
    }
}
