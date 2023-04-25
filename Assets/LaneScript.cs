using System.Collections.Generic;
using UnityEngine;

public class LaneScript : MonoBehaviour
{
    Queue<NoteBlock> Notes = new Queue<NoteBlock>();
    float Width = 0f;
    float Height = 0f;
    float UnitsPerMs = 0f;
    float MsForgiveness = 0f;
    float TimeToReachStrike = 0f;
    float StrikeHeight = 0f;
    float BottomY { 
        get
        {
            return -Height / 2;
        }
    }
    FloatRange StrikeRange
    {
        get
        {
            return new FloatRange(BottomY + StrikeHeight, BottomY + StrikeHeight + UnitsPerMs * MsForgiveness);
        }
    }
    public GameObject StrikeKey;

    /// <summary>
    /// Initalize lane.
    /// </summary>
    /// <param name="Dimensions">Width and height of lane.</param>
    /// <param name="xPos">X position of lane.</param>
    /// <param name="timeToReachStrike">How long it takes a note to reach the strike.</param>
    public void Init(float[] Dimensions, float xPos, float timeToReachStrike)
    {
        TimeToReachStrike = timeToReachStrike;

        UpdateDimensions(Dimensions, xPos);
    }

    public void UpdateDimensions(float[] Dimensions, float xPos)
    {
        // Update width and height.
        Width = Dimensions[0];
        Height = Dimensions[1];

        // Update units per ms.
        UnitsPerMs = (Height - StrikeHeight) / TimeToReachStrike;

        // Update x position.
        transform.localPosition = new Vector3(xPos, 0, 1);

        // Update strike range.
        StrikeKey.transform.GetChild(0).localScale = new Vector3(Width, MsForgiveness * UnitsPerMs, 1);
        StrikeKey.transform.localPosition = new Vector3(0, BottomY + MsForgiveness * UnitsPerMs / 2, 1);
    }

    public void AddNote(NoteBlock newNote)
    {
        Notes.Enqueue(newNote);
    }

    public void UpdateNotePositions(float CurrentPlaybackTimeMs)
    {
        // Update positions for all managed notes.
        foreach (var note in Notes)
        {
            // Get new scale.
            var newScale = new Vector3();
            newScale.x = Width;
            newScale.y = note.Length * UnitsPerMs;
            newScale.z = 1;

            // Get new position.
            var newPosition = new Vector3();
            newPosition.x = 0;
            newPosition.y = Height / 2 - (UnitsPerMs * (CurrentPlaybackTimeMs - note.TimePosition)) + note.NoteHeight / 2;
            newPosition.z = 2;

            note.UpdateNote(newPosition, newScale);
        }

        // Delete notes that are no longer visible.
        while (Notes.Count > 0)
        {
            if (Notes.Peek().TopY < BottomY) // Below the floor.
            {
                Destroy(Notes.Dequeue().GetNote());
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Gets the collision index of the note on event. 0 = Perfect, 1 = Good, 2 = Okay.
    /// Returns -1 if no collision.
    /// </summary>
    /// <returns>Collision index.</returns>
    public short NoteOnCollision()
    {
        if (Notes.Count == 0)
        {
            print($"Attempted to check note collision but lane is empty.");
            return -1;
        }

        for (short i = 0; i < 3; i++)
        {
            if (StrikeRange.RangesCollide(Notes.Peek().GetNoteOnCollisionRange(i)))
            {
                print($"Note On Collision Level '{i}' @ note number '{Notes.Peek().NoteNumber}'.");
                return i;
            }
        }

        print($"No collision @ note number: {Notes.Peek().NoteNumber}");
        return -1;
    }

    /// <summary>
    /// Gets the collision index of the note off event. 0 = Perfect, 1 = Good, 2 = Okay.
    /// Returns -1 if no collision.
    /// </summary>
    /// <returns>Collision index.</returns>
    public short NoteOffCollision()
    {
        if (Notes.Count == 0)
        {
            print($"Attempted to check note collision but lane is empty.");
            return -1;
        }

        for (short i = 0; i < 3; i++)
        {
            if (StrikeRange.RangesCollide(Notes.Peek().GetNoteOffCollisionRange(i))) 
            {
                print($"Note Off Collision Level '{i}' @ note number '{Notes.Peek().NoteNumber}'.");
                return i; 
            }
        }

        print($"No collision @ note number: {Notes.Peek().NoteNumber}");
        return -1;
    }
}
