using System.Collections.Generic;
using UnityEngine;

struct LaneWrapper
{
    public GameObject Lane;
    public LaneScript Script;
}

public class LaneScript : MonoBehaviour
{
    Queue<NoteBlock> Notes = new Queue<NoteBlock>();
    float Width = 0f;
    float Height = 0f;
    float UnitsPerMs = 0f;
    float MsForgiveness;
    float TimeToReachStrike = 0f;
    float StrikeHeight = 0f;
    float BottomY { 
        get
        {
            return -Height / 2;
        }
    }
    public GameObject StrikeKey;

    /// <summary>
    /// Initalize lane.
    /// </summary>
    /// <param name="Dimensions">Width and height of lane.</param>
    /// <param name="Strikeheight">Height of the strike area.</param>
    /// <param name="xPos">X position of lane.</param>
    /// <param name="timeToReachStrike">How long it takes a note to reach the strike.</param>
    /// <param name="Forgiveness">Range of forgiveness. (in ms)</param>
    public void Init(float[] Dimensions, float Strikeheight, float xPos, float timeToReachStrike, float Forgiveness)
    {
        TimeToReachStrike = timeToReachStrike;
        MsForgiveness = Forgiveness;
        StrikeHeight = Strikeheight;

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
        newNote.GetNote().transform.parent = transform;
        Notes.Enqueue(newNote);
    }

    /// <summary>
    /// Updates the positions of each note and deletes notes no longer visible.
    /// </summary>
    /// <param name="CurrentPlaybackTimeMs">Current time of playback.</param>
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
            newPosition.z = 0;

            note.UpdateNotePos(newPosition, newScale);
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
    /// Returns percent accuracy.
    /// </summary>
    /// <param name="Time">Current playback time.</param>
    /// <param name="NoteOnEvent">True if note on event, otherwise note off event.</param>
    /// <returns>Accuracy</returns>
    public float NoteEventAccuracy(float Time, bool NoteOnEvent)
    {
        if (Notes.Count == 0)
        {
            print($"Attempted to check note collision but lane is empty.");
            return 0f;
        }

        var MsDist = Time - (NoteOnEvent ? Notes.Peek().NoteOnTime : Notes.Peek().NoteOffTime); // Distance from actual time.
        if (MsDist < 0f) MsDist *= -1f; // Make it positive.

        print($"Distance in ms from actual: {MsDist}ms");

        if (MsDist == 0)
        {
            print($"Perfect accuracy.");
            return 100f;
        }

        if (MsDist > MsForgiveness)
        {
            print($"Total miss.");
            return 0f;
        }

        float Accuracy = 100f - (MsDist / MsForgiveness) * 100f;

        if (NoteOnEvent) print($"Note on accuracy: {Accuracy}%");
        else print($"Note off accuracy: {Accuracy}%");

        return Accuracy;
    }
}
