using Melanchall.DryWetMidi.Interaction;
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
    public short Number;
    public float Length;
}

public class Runway : MonoBehaviour
{
    short[] NoteRange;
    float NoteSpeedCoeff; // How far the note moves per milisecond.
    float MsToTouchRunway;
    LinkedList<NoteBlock>[] Lanes; // Treat like queue.
    Queue<VisualNote>[] Lane;
    float NoteWidth; // In unity units.
    float Height; // In unity units.
    float Width; // In unity units.
    float StrikeBarHeight; // In unity units.
    Queue<NoteInfo> DisplayQueue = new(); // Notes waiting to be displayed on next frame.
    public GameObject NotePrefab;
    public GameObject StrikeBar;
    private float[] ColliderOffsets = new float[3] { (float)0.05, (float)0.15, (float)0.5 };

    /// <summary>
    /// Initalizes a runway.
    /// </summary>
    /// <param name="Range">Range of notes where first is min and last is max. [min, max]</param>
    /// <param name="Dimensions">Height and width of runway in unity units. [width, height]</param>
    /// <param name="StrikebarHeight">Height of strikebar from the bottom of the runway in unity units.</param>
    /// <param name="QuarterNotesLeadup">Number of quarter notes that can be seen before first note touches the strikebar.</param>
    /// <param name="Tempo">The tempo map of the song.</param>
    /// <param name="SpeedMulti">Multiplier of speed. 1 is normal speed.</param>
    public void Init(short[] Range, float[] Dimensions, float StrikebarHeight, float MsToHitRunway, float SpeedMulti = 1)
    {
        print($"Initalizing runway. Note Range: {Range[0]} - {Range[1]}");
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
        var noteRange = Range[1] - Range[0] + 1;
        NoteWidth = Width / noteRange;

        // Init strike bar.
        StrikeBar.transform.localScale = new Vector3(Width, (float)0.5, 0);
        var barY = - Height / 2 + StrikeBarHeight - StrikeBar.transform.localScale.y;
        StrikeBar.transform.localPosition = new Vector3(0, barY, 1);
        StrikeBar.transform.GetComponent<SpriteRenderer>().enabled = true;

        // Create lanes.
        Lanes = new LinkedList<NoteBlock>[noteRange];

        for (int i = 0; i < Lanes.Length; i++)
        {
            Lanes[i] = new LinkedList<NoteBlock>();
        }
    }

    float GetNoteXPos(short NoteNum)
    {
        return (float)(NoteWidth * (NoteNum - NoteRange[0]) + NoteWidth / 2.0 - Width / 2.0); // Half width offset because anchor is in the middle.
    }

    /*
    void PositionColliderSet( Transform ColliderWrapper, bool PositionBottom )
    {
        for (int i = 0; i < ColliderWrapper.childCount; i++)
        {
            // Update height.
            var newScale = ColliderWrapper.GetChild(i).localScale;
            newScale.y = ColliderOffsets[i];
            newScale.x = NoteWidth;
            ColliderWrapper.GetChild(i).localScale = newScale;

            // Update position.
            var newPos = new Vector3(0,0,0);
            newPos.y = ColliderWrapper.parent.GetChild(0).localScale.y / 2;

            // Flip to bottom.
            if (PositionBottom)
            {
                newPos.y *= -1;
            }

            ColliderWrapper.GetChild(i).localPosition = newPos;
        }
    }*/

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
        float NoteX = GetNoteXPos(NoteNumber);
        float NoteY = (float)(NoteLength * NoteSpeedCoeff / 2.0 + Height / 2.0); // Half height offset because anchor is in the middle.
        NewNote.transform.position = new Vector3(NoteX, NoteY, 2);

        // Child 0 is skin.
        var NoteDimensions = new Vector3(NoteWidth, NoteLength * NoteSpeedCoeff); // Dimensions of note.
        NewNote.transform.GetChild(0).localScale = NoteDimensions;
        NewNote.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;

        /*
        // Adjust colliders.
        PositionColliderSet(NewNote.transform.GetChild(1), true);
        PositionColliderSet(NewNote.transform.GetChild(2), false);*/

        // print($"Inserting note '{NoteNumber}' to lane {NoteNumber - NoteRange[0]}.");
        var NewBlock = new NoteBlock();
        NewBlock.Note = NewNote;
        NewBlock.Number = NoteNumber;
        NewBlock.TimePosition = TimePosition;
        NewBlock.Length = NoteLength;
        NewBlock.PositionOffset = NewNote.transform.GetChild(0).transform.localScale.y / 2;
        Lanes[NoteNumber - NoteRange[0]].AddFirst(NewBlock); // Add to managed list.
    }

    public void UpdateNoteDisplayInfo(float[] NewDimensions)
    {
        // Update runway dimensions.
        Width = NewDimensions[0];
        Height = NewDimensions[1];
        NoteSpeedCoeff = (Height - StrikeBarHeight) / MsToTouchRunway; // units/ms
        print($"Note speed: {NoteSpeedCoeff} (units/milisecond)");

        // Update note width.
        var noteRange = NoteRange[1] - NoteRange[0] + 1;
        NoteWidth = Width / noteRange;

        // Update strike bar.
        StrikeBar.transform.localScale = new Vector3(Width, (float)0.5, 0);
        var barY = -Height / 2 + StrikeBarHeight - StrikeBar.transform.localScale.y;
        StrikeBar.transform.localPosition = new Vector3(0, barY, 1);
    }

    /// <summary>
    /// Turns on the collider for the specified note.
    /// </summary>
    /// <param name="NoteNumber">The note to activate the collider for.</param>
    public void ActivateNoteCollider(short NoteNumber)
    {

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

    public void AddNoteToLane(VisualNote visualNote)
    {
        // Check if valid note.
        if (visualNote.GetNoteNumber() < NoteRange[0] || visualNote.GetNoteNumber() > NoteRange[1])
        {
            print($"Note {visualNote.GetNoteNumber()} outside range [{NoteRange[0]}, {NoteRange[1]}].");
            return;
        }

        // Create new note.
        var NewNote = Instantiate(NotePrefab, this.transform);
        float NoteX = GetNoteXPos(visualNote.GetNoteNumber());
        float NoteY = (float)(visualNote.GetLength() * NoteSpeedCoeff / 2.0 + Height / 2.0); // Half height offset because anchor is in the middle.
        NewNote.transform.position = new Vector3(NoteX, NoteY, 2);

        // Child 0 is skin.
        var NoteDimensions = new Vector3(NoteWidth, visualNote.GetLength() * NoteSpeedCoeff); // Dimensions of note.
        NewNote.transform.GetChild(0).localScale = NoteDimensions;
        NewNote.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;

        // Add to lane.
        visualNote.DisplayNote(NewNote);
        Lane[visualNote.GetNoteNumber() - NoteRange[0]].Enqueue(visualNote);
    }

    /// <summary>
    /// Updates the position of all the notes in each lane.
    /// </summary>
    /// <param name="PlaybackTime">The time since the beginning of playback. (in miliseconds)</param>
    public void UpdateNotesPositions(float PlaybackTime)
    {
        if (Lane == null)
        {
            return;
        }

        while (DisplayQueue.Count > 0)
        {
            AddNoteToLane(DisplayQueue.Dequeue());
        }

        /*
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
                // Update position.
                var newPosition = new Vector3();
                newPosition.x = GetNoteXPos(Note.Value.Number);
                newPosition.y = Height / 2 - (float)(NoteSpeedCoeff * (PlaybackTime - Note.Value.TimePosition)) + Note.Value.PositionOffset;
                newPosition.z = Note.Value.Note.transform.localPosition.z;
                Note.Value.Note.transform.localPosition = newPosition;

                // Update scale.
                var newScale = new Vector3(NoteWidth, NoteSpeedCoeff * Note.Value.Length);
                Note.Value.Note.transform.GetChild(0).localScale = newScale;

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
    }*/
}
