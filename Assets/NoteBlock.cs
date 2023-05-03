using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class NoteBlock
{
    GameObject Note;
    public float TimePosition { get; private set; }
    public float Length { get; private set; }
    public short NoteNumber { get; private set; }
    public float NoteHeight
    {
        get
        {
            if (Note == null) return 0;

            return Note.transform.GetChild(0).localScale.y;
        }
    }
    public float TopY
    {
        get
        {
            if (Note == null) return 0;

            return Note.transform.GetChild(0).localScale.y / 2 + Note.transform.localPosition.y;
        }
    }
    public float BottomY
    {
        get
        {
            if (Note == null) return 0;

            return TopY - Note.transform.GetChild(0).localScale.y;
        }
    }
    public float NoteOnTime
    {
        get
        {
            return TimePosition;
        }
    }
    public float NoteOffTime
    {
        get
        {
            return TimePosition + Length;
        }
    }

    /// <summary>
    /// Assigns Note game object.
    /// </summary>
    /// <param name="note">The game object to assign for note.</param>
    public void SetNote (GameObject note)
    {
        Note = note;
    }

    /// <summary>
    /// Updates the position and scale of note.
    /// </summary>
    /// <param name="pos">Position of note.</param>
    /// <param name="scale">Scale of note.</param>
    public void UpdateNotePos (Vector3 pos, Vector3 scale)
    {
        if (Note == null) return;
        Note.transform.localPosition = pos;
        Note.transform.GetChild(0).localScale = scale;
    }

    /// <summary>
    /// Gets the note game object.
    /// </summary>
    /// <returns>The note game object.</returns>
    public GameObject GetNote()
    {
        return Note;
    }

    /// <summary>
    /// Creates a new visual note with no game object attached.
    /// </summary>
    /// <param name="positionInTime">Position in playback in ms.</param>
    /// <param name="noteLength">Length of note in ms.</param>
    /// <param name="noteNumber">Note number.</param>
    public NoteBlock(float positionInTime, float noteLength, short noteNumber)
    {
        TimePosition = positionInTime;
        Length = noteLength;
        NoteNumber = noteNumber;
        Note = null;
    }

    void Dispose()
    {
        Note = null;
    }
}