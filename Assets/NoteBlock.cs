using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class Leeway
{
    public float[] NoteOnLeeway;
    public float[] NoteOffLeeway;
}

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
    Leeway Forgiveness;

    /// <summary>
    /// Returns the range of y values that would count as collision for the given forgiveness index. Use for note on events.
    /// </summary>
    /// <param name="index">Index of the forgiveness range to use. 0 = Perfect, 1 = Good, 2 = Okay</param>
    /// <returns>Range of y values [min, max].</returns>
    public float[] GetNoteOnCollisionRange(short index)
    {
        if (Note == null) {  return null; }
        return new float[] { BottomY - Forgiveness.NoteOnLeeway[index], BottomY + Forgiveness.NoteOnLeeway[index] };

    }

    /// <summary>
    /// Returns the range of y values that would count as collision for the given forgiveness index. Use for note off events.
    /// </summary>
    /// <param name="index">Index of the forgiveness range to use. 0 = Perfect, 1 = Good, 2 = Okay.</param>
    /// <returns>Range of y values [min, max].</returns>
    public float[] GetNoteOffCollisionRange(short index)
    {
        if (Note == null) { return null; }
        return new float[] {TopY - Forgiveness.NoteOffLeeway[index], TopY + Forgiveness.NoteOffLeeway[index] };
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
    public void UpdateNote (Vector3 pos, Vector3 scale)
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
    /// <param name="Forgiveness">Leeway note collisions have.</param>
    public NoteBlock(float positionInTime, float noteLength, short noteNumber, Leeway Forgiveness = null)
    {
        if (Forgiveness == null)
        {
            Forgiveness = new Leeway();
            Forgiveness.NoteOnLeeway = new float[3] { 0, (float)0.2, (float)0.5 };
            Forgiveness.NoteOffLeeway = new float[3] { 0, (float)0.2, (float)0.5 };
        }

        TimePosition = positionInTime;
        Length = noteLength;
        NoteNumber = noteNumber;
        Note = null;
    }
}