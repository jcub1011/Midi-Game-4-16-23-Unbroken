using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class NoteBlock
{
    GameObject Note;
    CollisionRanges Forgiveness;
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

    /// <summary>
    /// Returns the range of y values that would count as collision for the given forgiveness index. Use for note on events.
    /// </summary>
    /// <param name="index">Index of the forgiveness range to use. 0 = Perfect, 1 = Good, 2 = Okay</param>
    /// <returns>Range of y values [min, max].</returns>
    public FloatRange GetNoteOnCollisionRange(short index)
    {
        if (Note == null) {  return null; }
        float range;

        switch (index)
        {
            case 0:
                range = Forgiveness.OnPerfectRange;
                break;

            case 1:
                range = Forgiveness.OnGoodRange;
                break;

            case 2:
                range = Forgiveness.OnGoodRange;
                break;

            default:
                return null;
        }

        range /= 2f;

        return new FloatRange( BottomY - range, BottomY + range );
    }

    /// <summary>
    /// Returns the range of y values that would count as collision for the given forgiveness index. Use for note off events.
    /// </summary>
    /// <param name="index">Index of the forgiveness range to use. 0 = Perfect, 1 = Good, 2 = Okay.</param>
    /// <returns>Range of y values [min, max].</returns>
    public FloatRange GetNoteOffCollisionRange(short index)
    {
        if (Note == null) { return null; }
        float range;

        switch (index)
        {
            case 0:
                range = Forgiveness.OffPerfectRange;
                break;

            case 1:
                range = Forgiveness.OffGoodRange;
                break;

            case 2:
                range = Forgiveness.OffGoodRange;
                break;

            default:
                return null;
        }

        range /= 2f;

        return new FloatRange(BottomY - range, BottomY + range );
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
    public NoteBlock(float positionInTime, float noteLength, short noteNumber, CollisionRanges Forgiveness = null)
    {
        if (Forgiveness == null)
        {
            Forgiveness = new CollisionRanges( new float[3] { 0, (float)0.2, (float)0.5 } );
        }

        TimePosition = positionInTime;
        Length = noteLength;
        NoteNumber = noteNumber;
        Note = null;
    }
}