using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteObject
{
    public GameObject Note { get; private set; }
    public Note Data { get; private set; }

    public NoteObject(GameObject note, Note data)
    {
        Note = note;
        Data = data;
    }
}

public class NoteListUnmanaged
{
    List<Note> _notes;
    public LinkedList<NoteObject> ActiveNotes;
    public int NextYoungestIndex { get; private set; }
    public int NextOldestIndex { get; private set; }
    public int ActiveNoteCount
    {
        get { return ActiveNotes.Count; }
    }
    public int TotalNoteCount
    {
        get { return _notes.Count; }
    }

    public void AddNewNote(Note noteData, Transform parent, GameObject prefab)
    {
        Debug.Log($"Adding note {noteData.NoteName} @ time {noteData.Time}");
        var wrappedData = new NoteObject(UnityEngine.Object.Instantiate(prefab, parent), noteData);
        wrappedData.Note.GetComponent<SpriteRenderer>().enabled = true;
        ActiveNotes.AddLast(wrappedData);
    }

    public NoteListUnmanaged()
    {
        _notes = new();
        ActiveNotes = new();
        NextYoungestIndex = 0;
        NextOldestIndex = -1;
    }
}

public class NoteLane : MonoBehaviour
{
    #region Properies
    GameObject NotePrefab;
    long _laneEnterOffset;
    long _laneExitOffset;
    NoteListUnmanaged _notePlayList;
    float _zPos;
    float _xPos;
    #endregion

    #region Utility Methods
    /// <summary>
    /// Checks if a note is visible on the runway.
    /// </summary>
    /// <param name="playbackTick">Current time of playback in ticks.</param>
    /// <param name="noteOnTick">Time of note on evt in ticks.</param>
    /// <param name="noteOffTick">Time of note off in ticks.</param>
    /// <returns>True if visible.</returns>
    bool NoteVisible(long playbackTick, long noteOnTick, long noteOffTick)
    {
        var enterTick = _laneEnterOffset - playbackTick;
        var exitTick = _laneExitOffset + playbackTick;
        // Check if either end is within the lane bounds.
        return noteOnTick >= enterTick && noteOffTick <= exitTick;
    }

    /// <summary>
    /// Checks if a note is visible on the runway.
    /// </summary>
    /// <param name="playbackTick">Current time of playback in ticks.</param>
    /// <param name="noteObject"></param>
    /// <returns></returns>
    bool NoteVisible(long playbackTick, NoteObject noteObject)
    {
        if (noteObject == null) return false;
        return NoteVisible(playbackTick, noteObject.Data.Time, noteObject.Data.EndTime);
    }

    bool NoteVisible(long playbackTick, Note noteData)
    {
        if (noteData == null) return false;
        return NoteVisible(playbackTick, noteData.Time, noteData.EndTime);
    }

    /// <summary>
    /// Updates the list of active notes.
    /// </summary>
    /// <param name="playbackTick">Current tick of playback.</param>
    void UpdateActiveNoteList(long playbackTick)
    {
    }

    /// <summary>
    /// Gets the accuracy of the note play.
    /// </summary>
    /// <param name="differenceTicks">Difference between the actual note tick and the played note tick.</param>
    /// <param name="forgiveness">Range of forgiveness in ticks.</param>
    /// <returns></returns>
    float CalculateAccuracy(long differenceTicks, long forgiveness)
    {
        if (differenceTicks < 0) differenceTicks *= -1; // Make it positive.

        if (differenceTicks == 0)
        {
            return 100f;
        }

        if (differenceTicks > forgiveness)
        {
            return 0f;
        }

        float accuracy = 100f - (float)differenceTicks / forgiveness * 100f;

        return accuracy;
    }

    /// <summary>
    /// Checks if a number is between two other numbers.
    /// </summary>
    /// <param name="number">Number to check.</param>
    /// <param name="rangeMin">Range min.</param>
    /// <param name="rangeMax">Range max.</param>
    /// <returns></returns>
    bool InRange(float number, float rangeMin, float rangeMax)
    {
        return rangeMin <= number && number <= rangeMax;
    }

    /// <summary>
    /// Deletes the game objects for notes that aren't visible.
    /// </summary>
    /// <param name="playbackTick"></param>
    void UnmanageNotesNotVisible(float playbackTick)
    {
    }

    /// <summary>
    /// Updates the note positions for all managed notes.
    /// </summary>
    /// <param name="playbackTick">Time in ticks.</param>
    void UpdateNotePositions(long playbackTick, float unitsPerTick)
    {
        var halfRunwayHeight = (_laneEnterOffset + _laneExitOffset) * unitsPerTick / 2f;

        // Update positions for all managed notes.
        foreach (var noteObject in _notePlayList.ActiveNotes)
        {
            // Get new scale.
            var newScale = new Vector3
            {
                x = 1,
                y = noteObject.Data.Length * unitsPerTick,
                z = 1
            };

            // Get new position.
            var newPosition = new Vector3
            {
                x = 0,
                y = halfRunwayHeight - (unitsPerTick * (playbackTick - noteObject.Data.Time)) + newScale.y / 2,
                z = 0
            };

            // Update scale and position.
            noteObject.Note.transform.localPosition = newPosition;
            noteObject.Note.transform.localScale = newScale;
        }
    }
    #endregion

    #region Public Methods
    public void SetNotePrefab(GameObject notePrefab)
    {
        NotePrefab = notePrefab;
    }

    public void UpdateLane(long playbackTick, float unitsPerMs)
    {
        if (_notePlayList == null)
        {
            Debug.Log($"Lane has no notes.");
            return;
        }

        UpdateActiveNoteList(playbackTick);
        UpdateNotePositions(playbackTick, unitsPerMs);
        UnmanageNotesNotVisible(playbackTick);
    }

    /// <summary>
    /// Adds a note to the note play list.
    /// </summary>
    /// <param name="newNote"></param>
    public void AddNote(Note newNote)
    {
        _notePlayList ??= new(); // Null coalescing operator.
        _notePlayList.AddNewNote(newNote, transform, NotePrefab);
    }

    /// <summary>
    /// Replaces the note play list with a new note play list.
    /// </summary>
    /// <param name="notes"></param>
    public void AddNotesList(List<Note> notes)
    {
        _notePlayList ??= new();

        //_notePlayList.OverwriteNoteList(notes);
        foreach (var note in notes)
        {
            _notePlayList.AddNewNote(note, transform, NotePrefab);
        }
    }

    /// <summary>
    /// Returns percent accuracy.
    /// </summary>
    /// <param name="time">Current playback time.</param>
    /// <param name="forgiveness">Forgivness range in ms.</param>
    /// <param name="NoteOnEvent">True if note on event, otherwise note off event.</param>
    /// <returns>Accuracy</returns>
    public float NoteEventAccuracy(float time, float forgiveness, bool NoteOnEvent)
    {
        //float eventTimeToCompareWith = 2f * GameData.Forgiveness;
        float evtTime = -10f;

        print($"Current time: {time}");

        // Find next playable note time.
        foreach (var note in _notePlayList.ActiveNotes)
        {
            var noteTime = NoteOnEvent ? note.OnTime : note.OffTime;

            if (InRange(time, noteTime - forgiveness, noteTime + forgiveness) && !note.Played)
            {
                note.Played = true;
                evtTime = noteTime;
                break;
            }

        }

        // Only when there is no note within forgiveness range.
        if (evtTime < 0f)
        {
            print("Total miss.");
            return 0f;
        }

        var msDist = evtTime - time;
        var accuracy = CalculateAccuracy(msDist, forgiveness);
        print($"Note event accuracy: {accuracy}%\n" +
            "Note time position: {eventTimeToCompareWith}ms\n" +
            $"Played note time distance: {msDist}ms");

        return accuracy;
    }

    public void SetOffsets(float laneEnterOffset, float laneExitOffset)
    {
        _laneEnterOffset = laneEnterOffset;
        _laneExitOffset = laneExitOffset;
    }

    public void SetPosition(float xPos, float zPos)
    {
        _xPos = xPos;
        _zPos = zPos;
        transform.localPosition = new Vector3(_xPos, 0, _zPos);
    }

    public void SetPosition(float xPos)
    {
        SetPosition(xPos, _zPos);
    }
    #endregion

    #region Getter Setter Methods
    private float _width;
    public float Width
    {
        get { return _width; }
        set
        {
            _width = value;
            transform.localScale = new Vector3(_width, 1f, 1f);
        }
    }
    #endregion
}
