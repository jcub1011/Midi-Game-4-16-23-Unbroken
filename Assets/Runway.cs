using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Collections.Generic;
using UnityEngine;

public class Runway : MonoBehaviour
{
    IntRange NoteRange;
    float UnitsPerMs; // How far the note moves per milisecond.
    float MsToTouchRunway;
    float NoteWidth; // In unity units.
    float Height; // In unity units.
    float Width; // In unity units.
    float StrikeBarHeight; // In unity units.
    float Forgiveness; // In ms.
    float[] _forgivenessDivision;
    LaneWrapper[] Lanes;
    Queue<NoteBlock> DisplayQueue = new(); // Notes waiting to be displayed on next frame.
    public GameObject NotePrefab;
    public GameObject StrikeBar;
    public GameObject LanePrefab;

    /// <summary>
    /// Initalizes a runway.
    /// </summary>
    /// <param name="Range">Range of notes where first is min and last is max. [min, max]</param>
    /// <param name="Dimensions">Height and width of runway in unity units. [width, height]</param>
    /// <param name="StrikebarHeight">Height of strikebar from the bottom of the runway in unity units.</param>
    /// <param name="MsToHitStrikebar">How long it takes to reach the strikebar.</param>
    /// <param name="ForgiveRadius">Ms of forgiveness around the strike area.</param>
    /// <param name="SpeedMulti">Multiplier of speed. 1 is normal speed.</param>
    public void Init(IntRange Range, float[] Dimensions, float StrikebarHeight, float MsToHitStrikebar, float ForgiveRadius, float SpeedMulti = 1, float[] ForgivenessDivision = null)
    {
        // Init forgiveness range.
        if (ForgivenessDivision == null) _forgivenessDivision = new float[] { 0.1f, 0.5f };
        else _forgivenessDivision = ForgivenessDivision;

        print($"Initalizing runway. Note Range: {Range.Min} - {Range.Max}");
        // Init private members.
        NoteRange = Range;
        StrikeBarHeight = StrikebarHeight;

        // Get notespeed.
        var DistToStrikebar = Height - StrikeBarHeight;
        MsToTouchRunway = MsToHitStrikebar;
        UnitsPerMs = DistToStrikebar / MsToTouchRunway; // units/ms
        print($"Note speed: {UnitsPerMs} (units/milisecond)");

        // Init strike bar.
        foreach (var renderer in StrikeBar.transform.GetComponentsInChildren<SpriteRenderer>())
        {
            renderer.enabled = true;
        }

        Forgiveness = ForgiveRadius;
        UpdateNoteDisplayInfo(Dimensions);

        // Create lanes.
        Lanes = new LaneWrapper[Range.Len];

        for (int i = 0; i < Lanes.Length; i++)
        {
            // Create lane.
            var newLane = new LaneWrapper();
            newLane.Lane = Instantiate(LanePrefab, transform);
            newLane.Script = newLane.Lane.transform.GetComponent<LaneScript>();

            // Lane position;
            var posX = GetNoteXPos((short)(Range.Min + i));

            newLane.Script.Init(new float[2] { NoteWidth, Height }, StrikebarHeight, posX, MsToHitStrikebar, ForgiveRadius);

            Lanes[i] = newLane;
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
    /// <param name="Forgiveness">Forgiveness of player input.</param>
    public void UpdateNoteDisplayInfo(float[] NewDimensions)
    {
        // Update runway dimensions.
        Width = NewDimensions[0];
        Height = NewDimensions[1];
        UnitsPerMs = (Height - StrikeBarHeight) / MsToTouchRunway; // units/ms
        print($"Note speed: {UnitsPerMs} (units/milisecond)");

        // Update note width.
        NoteWidth = Width / NoteRange.Len;

        // Update strike bar.
        StrikeBar.transform.localScale = new Vector3(Width, 1, 0);
        // Update inner strike bars.
        StrikeBar.transform.GetChild(0).localScale = new Vector3(Width, _forgivenessDivision[0] * Forgiveness * UnitsPerMs, 1); // Perfect
        StrikeBar.transform.GetChild(0).localPosition = new Vector3(0, 0, 2); // Perfect
        StrikeBar.transform.GetChild(1).localScale = new Vector3(Width, _forgivenessDivision[1] * Forgiveness * UnitsPerMs, 1); // Good
        StrikeBar.transform.GetChild(1).localPosition = new Vector3(0, 0, 3); // Good
        StrikeBar.transform.GetChild(2).localScale = new Vector3(Width, Forgiveness * UnitsPerMs, 1); // Okay
        StrikeBar.transform.GetChild(2).localPosition = new Vector3(0, 0, 4); // Okay
        // Position outside strike bar.
        var barY = -Height / 2 + StrikeBarHeight;
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
        var NewNote = Instantiate(NotePrefab);
        // NewNote.transform.localPosition = new Vector3(0, Height + NewNote.transform.localScale.y * 2, 0);

        // Child 0 is skin.
        NewNote.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;

        // Add to lane.
        noteBlock.SetNote(NewNote);
        Lanes[noteBlock.NoteNumber - NoteRange.Min].Script.AddNote(noteBlock);
    }

    /// <summary>
    /// Updates each lane.
    /// </summary>
    /// <param name="PlaybackTime">The time since the beginning of playback. (in miliseconds)</param>
    public void UpdateLanes(float PlaybackTime)
    {
        if (Lanes == null)
        {
            return;
        }

        while (DisplayQueue.Count > 0)
        {
            AddNoteToLane(DisplayQueue.Dequeue());
        }

        for (var i = 0; i < Lanes.Length; i++)
        {
            // Update each lane.
            Lanes[i].Script.UpdateDimensions(new float[2] { NoteWidth, Height }, GetNoteXPos((short)(NoteRange.Min + i)));
            Lanes[i].Script.UpdateNotePositions(PlaybackTime);
        }
    }

    public float GetNoteInputAccuracy(float PlaybackTime, NoteOnEvent note)
    {
        return Lanes[note.NoteNumber - NoteRange.Min].Script.NoteEventAccuracy(PlaybackTime, true);
    }

    public float GetNoteInputAccuracy(float PlaybackTime, NoteOffEvent note)
    {
        return Lanes[note.NoteNumber - NoteRange.Min].Script.NoteEventAccuracy(PlaybackTime, false);
    }
}
