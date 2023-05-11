using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UIElements;
using Utils;

struct LaneWrapper
{
    public GameObject Lane;
    public Lane Script;
}

public class NoteWrapper
{
    public GameObject Note { get; private set; }
    public float Length { get; private set; }
    public float OnTime { get; private set; }
    public float OffTime { get; private set; }

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
        Object.Destroy(oldNote.Value.Note);
        NextOldestIndex++;
    }

    public void ManageNextOldestNote(Transform parent, GameObject notePreFab)
    {
        if (NextOldestIndex < 0) return;
        var nextOldNote = Object.Instantiate(notePreFab, parent);
        var wrapper = new NoteWrapper(nextOldNote, _notes[NextOldestIndex]);
        ActiveNotes.AddFirst(wrapper);
        NextOldestIndex--;
    }

    public void UnmanageYoungestNote()
    {
        if (NextYoungestIndex < 0) return;
        var newestNote = ActiveNotes.Last;
        ActiveNotes.RemoveLast();
        Object.Destroy(newestNote.Value.Note);
        NextYoungestIndex--;
    }

    public void ManageNextYoungestNote(Transform parent, GameObject notePreFab)
    {
        if (NextYoungestIndex == _notes.Count) return;
        var nextNewestNote = Object.Instantiate(notePreFab, parent);
        var wrapper = new NoteWrapper(nextNewestNote, _notes[NextYoungestIndex]);
        ActiveNotes.AddLast(wrapper);
        NextYoungestIndex++;
    }
}

public class Lane : MonoBehaviour
{
    NoteListManager _notePlayList;
    float _width = 0f;
    float _height = 0f;
    float _unitsPerMs = 0f;
    float _timeToReachStrike = 0f;
    float _strikeHeight = 0f;
    float TopY
    {
        get
        {
            return _height / 2;
        }
    }
    float BottomY
    {
        get
        {
            return -_height / 2;
        }
    }
    public GameObject StrikeKey;
    public GameObject NotePrefab;

    /// <summary>
    /// Initalize lane.
    /// </summary>
    /// <param name="Dimensions">Width and height of lane.</param>
    /// <param name="Strikeheight">Height of the strike area.</param>
    /// <param name="xPos">X position of lane.</param>
    /// <param name="timeToReachStrike">How long it takes a note to reach the strike.</param>
    public void Init(float[] Dimensions, float Strikeheight, float xPos, float timeToReachStrike)
    {
        _timeToReachStrike = timeToReachStrike;
        _strikeHeight = Strikeheight;

        UpdateDimensions(Dimensions, xPos);
    }

    public void UpdateDimensions(float[] Dimensions, float xPos)
    {
        // Update width and height.
        _width = Dimensions[0];
        _height = Dimensions[1];

        // Update units per ms.
        _unitsPerMs = (_height - _strikeHeight) / _timeToReachStrike;

        // Update x position.
        transform.localPosition = new Vector3(xPos, 0, 0);

        // Update strike range.
        StrikeKey.transform.GetChild(0).localScale = new Vector3(_width, GameData.Forgiveness * _unitsPerMs, 1);
        StrikeKey.transform.localPosition = new Vector3(0, BottomY + GameData.Forgiveness * _unitsPerMs / 2, 1);
    }
    
    public void AddNote(NoteEvtData newNote)
    {
        _notePlayList ??= new (); // Null coalescing operator.
        _notePlayList.AddNewNote(newNote);
    }

    public void AddNotesList(List<NoteEvtData> notes)
    {
        _notePlayList.OverwriteNoteList(notes);
    }

    bool NoteVisible(float playbackTime, float noteOnTime, float noteOffTime)
    {
        float runwayEnterTime = playbackTime - _timeToReachStrike;
        float runwayExitTime = playbackTime + _unitsPerMs * _timeToReachStrike;

        // Check if either end is within the lane bounds.
        return noteOnTime > runwayEnterTime && noteOffTime < runwayExitTime;
    }

    bool NoteVisible(float playbackTime, NoteWrapper noteWrapper)
    {
        if (noteWrapper == null) return false;
        return NoteVisible(playbackTime, noteWrapper.OnTime, noteWrapper.OffTime);
    }

    bool NoteVisible(float playbackTime, NoteEvtData noteEvtData)
    {
        if (noteEvtData == null) return false;
        return NoteVisible(playbackTime, noteEvtData.onTime, noteEvtData.offTime);
    }
    
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

    void UpdateNotePositions(float playbackTime)
    {
        // Update positions for all managed notes.
        foreach (var wrapper in _notePlayList.ActiveNotes)
        {
            // Get new scale.
            var newScale = new Vector3
            {
                x = _width,
                y = wrapper.Length * _unitsPerMs,
                z = 1
            };

            // Get new position.
            var newPosition = new Vector3
            {
                x = 0,
                y = _height / 2 - (_unitsPerMs * (playbackTime - wrapper.OnTime)) + newScale.y / 2,
                z = 0
            };

            // Update scale and position.
            wrapper.Note.transform.localPosition = newPosition;
            wrapper.Note.transform.localScale = newScale;
        }
    }

    /// <summary>
    /// Updates the positions of each note and deletes notes no longer visible.
    /// </summary>
    /// <param name="currentPlaybackTimeMs">Current time of playback.</param>
    public void UpdateLane(float currentPlaybackTimeMs)
    {
        UpdateActiveNoteList(currentPlaybackTimeMs);
        UpdateNotePositions(currentPlaybackTimeMs);
        UnmanageNotesNotVisible(currentPlaybackTimeMs);
    }

    private float CalculateAccuracy(float differenceMs, float forgiveness)
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

    float AbsTimeDiff(float time1, float time2)
    {
        var diff = time1 - time2;
        if (diff < 0f) diff *= -1;
        return diff;
    }

    bool InRange(float number, float rangeMin, float rangeMax)
    {
        if (rangeMin <= number && number <= rangeMax) return true;
        return false;
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
        float eventTimeToCompareWith = 2f * GameData.Forgiveness;
        float evtTime = -10f;

        print($"Current time: {time}");

        // Find next playable note time.
        foreach (var note in _notePlayList.ActiveNotes)
        {
            var noteTime = NoteOnEvent ? note.OnTime : note.OffTime;

            if (InRange(time, noteTime - forgiveness, noteTime + forgiveness))
            {
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
            $"Note time position: {eventTimeToCompareWith}ms\n" +
            $"Played note time distance: {msDist}ms");

        return accuracy;
    }
}
