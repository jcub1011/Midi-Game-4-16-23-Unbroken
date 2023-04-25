using UnityEditor;
using UnityEngine;

public class Leeway
{
    public float[] NoteOnLeeway;
    public float[] NoteOffLeeway;
}

public class VisualNote
{
    GameObject Note;
    float TimePosition;
    float Length;
    short NoteNumber;
    Leeway Forgiveness;

    public float GetTimePosition() { return TimePosition; }

    public float GetLength() { return Length; }

    public short GetNoteNumber() { return NoteNumber; }

    /// <summary>
    /// Returns the range of y values that would count as collision for the given forgiveness index. Use for note on events.
    /// </summary>
    /// <param name="index">Index of the forgiveness range to use. 0 = Perfect, 1 = Good, 2 = Okay</param>
    /// <returns>Range of y values [min, max].</returns>
    public float[] GetNoteOnCollisionRange(short index)
    {
        if (Note == null) {  return null; }
        var BottomPosition = Note.transform.localPosition.y - Note.transform.localScale.y / 2;
        return new float[] { BottomPosition - Forgiveness.NoteOnLeeway[index], BottomPosition + Forgiveness.NoteOnLeeway[index] };

    }

    /// <summary>
    /// Returns the range of y values that would count as collision for the given forgiveness index. Use for note off events.
    /// </summary>
    /// <param name="index">Index of the forgiveness range to use. 0 = Perfect, 1 = Good, 2 = Okay.</param>
    /// <returns>Range of y values [min, max].</returns>
    public float[] GetNoteOffCollisionRange(short index)
    {
        if (Note == null) { return null; }
        var TopPosition = Note.transform.localPosition.y + Note.transform.localScale.y / 2;
        return new float[] { TopPosition - Forgiveness.NoteOffLeeway[index], TopPosition + Forgiveness.NoteOffLeeway[index] };
    }

    public void DisplayNote (GameObject note)
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
        Note.transform.localScale = scale;
    }

    VisualNote(float positionInTime, float noteLength, short noteNumber, Leeway Forgiveness = null)
    {
        if (Forgiveness == null)
        {
            Forgiveness.NoteOnLeeway = new float[3] { 0, (float)0.2, (float)0.5 };
            Forgiveness.NoteOffLeeway = new float[3] { 0, (float)0.2, (float)0.5 };
        }

        TimePosition = positionInTime;
        Length = noteLength;
        NoteNumber = noteNumber;
        Note = null;
    }
}