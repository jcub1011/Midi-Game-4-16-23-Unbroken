using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteWrapper
{
    public GameObject Note { get; private set; }
    public float Length { get; private set; }
    public float OnTime { get; private set; }
    public float OffTime { get; private set; }
    public bool Played = false;

    public NoteWrapper(GameObject note, float length, float noteOnTime, float noteOffTime)
    {
        Note = note;
        Length = length;
        OnTime = noteOnTime;
        OffTime = noteOffTime;
    }

    public NoteWrapper(GameObject note, NoteEvtData data)
    {
        Note = note;
        Length = data.len;
        OnTime = data.onTime;
        OffTime = data.offTime;
    }
}

public class DisposableNoteListManager : IDisposable
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

    #region Interface
    ~DisposableNoteListManager()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {

        if (disposing)
        {
            // Clean managed resources.
            _notes = null;
        }

        // Clean unmanaged resources.
        foreach (var note in ActiveNotes)
        {
            UnityEngine.Object.Destroy(note.Note);
        }
        ActiveNotes.Clear();
    }
    #endregion

    public void AddNewNote(NoteEvtData noteEvtData)
    {
        _notes.Add(noteEvtData);
    }

    public void OverwriteNoteList(List<NoteEvtData> notes)
    {
        Dispose(true);
        _notes = notes;
        NextYoungestIndex = 0;
        NextOldestIndex = -1;
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
        var wrapper = new NoteWrapper(nextOldNote, _notes[NextOldestIndex]);
        ActiveNotes.AddFirst(wrapper);
        NextOldestIndex--;
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
        var wrapper = new NoteWrapper(nextNewestNote, _notes[NextYoungestIndex]);
        ActiveNotes.AddLast(wrapper);
        NextYoungestIndex++;
    }
}

public class NoteLane : MonoBehaviour
{
    #region Properies
    public GameObject StrikeKey;
    public GameObject NotePrefab;
    float _laneEnterOffset;
    float _laneExitOffset;
    NoteListManager _notePlayList;
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
    /// <param name="noteWrapper"></param>
    /// <returns></returns>
    bool NoteVisible(float playbackTime, NoteWrapper noteWrapper)
    {
        if (noteWrapper == null) return false;
        return NoteVisible(playbackTime, noteWrapper.OnTime, noteWrapper.OffTime);
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
        return NoteVisible(playbackTime, noteEvtData.onTime, noteEvtData.offTime);
    }

    /// <summary>
    /// Updates the list of active notes.
    /// </summary>
    /// <param name="playbackTime"></param>
    void UpdateActiveNoteList(float playbackTime)
    {
        // Add notes to the top.
        while (NoteVisible(playbackTime, _notePlayList.PeekNextYoungestNote()))
        {
            _notePlayList.ManageNextYoungestNote(transform, NotePrefab);
        }

        // Add notes to the bottom.
        while (NoteVisible(playbackTime, _notePlayList.PeekNextOldestNote()))
        {
            _notePlayList.ManageNextOldestNote(transform, NotePrefab);
        }
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
        }
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
                x = _width,
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
    public void UpdateLane(float playbackTime, float unitsPerMs)
    {
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
        _notePlayList.AddNewNote(newNote);
    }

    /// <summary>
    /// Replaces the note play list with a new note play list.
    /// </summary>
    /// <param name="notes"></param>
    public void AddNotesList(List<NoteEvtData> notes)
    {
        _notePlayList ??= new();
        _notePlayList.OverwriteNoteList(notes);
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
    private float _xPos;
    public float XPos
    {
        get { return _xPos; }
        set
        {
            _xPos = value;
            transform.localPosition = new Vector3(_xPos, 0f, 0f);
        }
    }
    #endregion
}
