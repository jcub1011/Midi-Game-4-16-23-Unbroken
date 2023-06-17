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

/*
public class NoteListManager
{
    List<NoteEvtData> _notes = new();
    public LinkedList<NoteWrapper> ActiveNotes = new();
    public int NextYoungestIndex { get; private set; } = 0;
    public int NextOldestIndex { get; private set; } = -1;
    public int ActiveNoteCount
    {
        get { return ActiveNotes.Count; }
    }
    public int TotalNoteCount
    {
        get { return _notes.Count; }
    }

    public void ClearNoteList()
    {
        _notes.Clear();
    }

    public void AddNewNote(NoteEvtData noteEvtData)
    {
        Debug.Log($"Adding note {noteEvtData.Number} @ time {noteEvtData.OnTime}");
        _notes.Add(noteEvtData);
    }

    public void OverwriteNoteList(List<NoteEvtData> notes)
    {
        _notes = notes;
    }

    public NoteWrapper PeekCurrentYoungestNote()
    {
        if (ActiveNoteCount == 0) return null;
        return ActiveNotes.Last.Value;
    }

    public NoteEvtData PeekNextYoungestNote()
    {
        if (_notes.Count == NextYoungestIndex) return null;
        return _notes[NextYoungestIndex];
    }

    public NoteWrapper PeekCurrentOldestNote()
    {
        if (ActiveNoteCount == 0) return null;
        return ActiveNotes.First.Value;
    }

    public NoteEvtData PeekNextOldestNote()
    {
        if (NextOldestIndex < 0) return null;
        return _notes[NextOldestIndex];
    }

    public void UnmanageOldestNote()
    {
        if (NextOldestIndex == _notes.Count) return;
        var oldNote = ActiveNotes.First;
        ActiveNotes.RemoveFirst();
        UnityEngine.Object.Destroy(oldNote.Value.Note);
        NextOldestIndex++;
    }

    public void ManageNextOldestNote(Transform parent, GameObject notePreFab)
    {
        if (NextOldestIndex < 0) return;
        var nextOldNote = UnityEngine.Object.Instantiate(notePreFab, parent);
        nextOldNote.GetComponent<SpriteRenderer>().enabled = true;
        var wrapper = new NoteWrapper(nextOldNote, _notes[NextOldestIndex]);
        ActiveNotes.AddFirst(wrapper);
        NextOldestIndex--;
        Debug.Log($"Managing note");
    }

    public void UnmanageYoungestNote()
    {
        if (NextYoungestIndex < 0) return;
        var newestNote = ActiveNotes.Last;
        ActiveNotes.RemoveLast();
        UnityEngine.Object.Destroy(newestNote.Value.Note);
        NextYoungestIndex--;
    }

    public void ManageNextYoungestNote(Transform parent, GameObject notePreFab)
    {
        if (NextYoungestIndex == _notes.Count) return;
        var nextNewestNote = UnityEngine.Object.Instantiate(notePreFab, parent);
        nextNewestNote.GetComponent<SpriteRenderer>().enabled = true;
        var wrapper = new NoteWrapper(nextNewestNote, _notes[NextYoungestIndex]);
        ActiveNotes.AddLast(wrapper);
        NextYoungestIndex++;
    }
}*/

public class NoteLane : MonoBehaviour
{
    #region Properies
    GameObject NotePrefab;
    float _laneEnterOffset;
    float _laneExitOffset;
    NoteListUnmanaged _notePlayList;
    float _zPos;
    float _xPos;
    #endregion

    #region Utility Methods
    /// <summary>
    /// Checks if a note is visible on the runway.
    /// </summary>
    /// <param name="playbackTime">Current time of playback in ms.</param>
    /// <param name="noteOnTime"></param>
    /// <param name="noteOffTime"></param>
    /// <returns>True if visible.</returns>
    bool NoteVisible(float playbackTime, float noteOnTime, float noteOffTime)
    {
        var enterTime = _laneEnterOffset - playbackTime;
        var exitTime = _laneExitOffset + playbackTime;
        // Check if either end is within the lane bounds.
        return noteOnTime >= enterTime && noteOffTime <= exitTime;
    }

    /// <summary>
    /// Checks if a note is visible on the runway.
    /// </summary>
    /// <param name="playbackTime">Current time of playback in ms.</param>
    /// <param name="noteObject"></param>
    /// <returns></returns>
    bool NoteVisible(float playbackTime, NoteObject noteObject)
    {
        if (noteObject == null) return false;
        return NoteVisible(playbackTime, noteObject.Data.Time, noteObject.Data.EndTime);
    }

    /// <summary>
    /// Checks if a note is visible on the runway.
    /// </summary>
    /// <param name="playbackTime">Current time of playback in ms.</param>
    /// <param name="noteEvtData"></param>
    /// <returns></returns>
    bool NoteVisible(float playbackTime, NoteEvtData noteEvtData)
    {
        if (noteEvtData == null) return false;
        return NoteVisible(playbackTime, noteEvtData.OnTime, noteEvtData.OffTime);
    }

    /// <summary>
    /// Updates the list of active notes.
    /// </summary>
    /// <param name="playbackTime"></param>
    void UpdateActiveNoteList(float playbackTime)
    {
        /*
        // Add notes to the top.
        while (NoteVisible(playbackTime, _notePlayList.PeekNextYoungestNote()))
        {
            _notePlayList.ManageNextYoungestNote(transform, NotePrefab);
        }

        // Add notes to the bottom.
        while (NoteVisible(playbackTime, _notePlayList.PeekNextOldestNote()))
        {
            _notePlayList.ManageNextOldestNote(transform, NotePrefab);
        }*/
    }

    /// <summary>
    /// Gets the accuracy of the note play.
    /// </summary>
    /// <param name="differenceMs">Difference between the actual note time and the played note time.</param>
    /// <param name="forgiveness">Range of forgiveness in ms.</param>
    /// <returns></returns>
    float CalculateAccuracy(float differenceMs, float forgiveness)
    {
        if (differenceMs < 0f) differenceMs *= -1f; // Make it positive.

        if (differenceMs == 0)
        {
            return 100f;
        }

        if (differenceMs > forgiveness)
        {
            return 0f;
        }

        float accuracy = 100f - (differenceMs / forgiveness) * 100f;

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
    /// <param name="playbackTime"></param>
    void UnmanageNotesNotVisible(float playbackTime)
    {
        /*
        // Delete notes that are below floor.
        while (_notePlayList.ActiveNoteCount > 0)
        {
            // Get top y pos of note.
            var oldestNote = _notePlayList.PeekCurrentOldestNote();

            if (!NoteVisible(playbackTime, oldestNote))
            {
                _notePlayList.UnmanageOldestNote();
            }
            else
            {
                break;
            }
        }

        // Delete notes that are above ceiling.
        while (_notePlayList.ActiveNoteCount > 0)
        {
            // Get bottom y pos of note.
            var lastNote = _notePlayList.PeekCurrentYoungestNote();

            if (!NoteVisible(playbackTime, lastNote))
            {
                _notePlayList.UnmanageYoungestNote();
            }
            else
            {
                break;
            }
        }*/
    }

    /// <summary>
    /// Updates the note positions for all managed notes.
    /// </summary>
    /// <param name="playbackTime"></param>
    void UpdateNotePositions(float playbackTime, float unitsPerMs)
    {
        var halfRunwayHeight = (_laneEnterOffset + _laneExitOffset) * unitsPerMs / 2f;
        // Update positions for all managed notes.
        foreach (var wrapper in _notePlayList.ActiveNotes)
        {
            // Get new scale.
            var newScale = new Vector3
            {
                x = 1,
                y = wrapper.Length * unitsPerMs,
                z = 1
            };

            // Get new position.
            var newPosition = new Vector3
            {
                x = 0,
                y = halfRunwayHeight - (unitsPerMs * (playbackTime - wrapper.OnTime)) + newScale.y / 2,
                z = 0
            };

            // Update scale and position.
            wrapper.Note.transform.localPosition = newPosition;
            wrapper.Note.transform.localScale = newScale;
        }
    }
    #endregion

    #region Public Methods
    public void SetNotePrefab(GameObject notePrefab)
    {
        NotePrefab = notePrefab;
    }

    public void UpdateLane(float playbackTime, float unitsPerMs)
    {
        if (_notePlayList == null)
        {
            Debug.Log($"Lane has no notes.");
            return;
        }

        UpdateActiveNoteList(playbackTime);
        UpdateNotePositions(playbackTime, unitsPerMs);
        UnmanageNotesNotVisible(playbackTime);
    }

    /// <summary>
    /// Adds a note to the note play list.
    /// </summary>
    /// <param name="newNote"></param>
    public void AddNote(NoteEvtData newNote)
    {
        _notePlayList ??= new(); // Null coalescing operator.
        _notePlayList.AddNewNote(newNote, transform, NotePrefab);
    }

    /// <summary>
    /// Replaces the note play list with a new note play list.
    /// </summary>
    /// <param name="notes"></param>
    public void AddNotesList(List<NoteEvtData> notes)
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
