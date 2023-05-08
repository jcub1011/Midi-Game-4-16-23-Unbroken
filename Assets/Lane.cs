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

public class Lane : MonoBehaviour
{
    LinkedList<NoteWrapper> _activeNotes = new LinkedList<NoteWrapper>();
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
        // Create new note block.
        NoteWrapper noteBlock = new(Instantiate(NotePrefab, transform), newNote);

        // Add note to managed list.
        _activeNotes.AddLast(noteBlock);
    }

    /// <summary>
    /// Updates the positions of each note and deletes notes no longer visible.
    /// </summary>
    /// <param name="CurrentPlaybackTimeMs">Current time of playback.</param>
    public void UpdateNotePositions(float CurrentPlaybackTimeMs)
    {
        // Update positions for all managed notes.
        foreach (var noteWrapper in _activeNotes)
        {
            // Get new scale.
            var newScale = new Vector3();
            newScale.x = _width;
            newScale.y = noteWrapper.Length * _unitsPerMs;
            newScale.z = 1;

            // Get new position.
            var newPosition = new Vector3();
            newPosition.x = 0;
            newPosition.y = _height / 2 - (_unitsPerMs * (CurrentPlaybackTimeMs - noteWrapper.OnTime)) + newScale.y / 2;
            newPosition.z = 0;

            // Update scale and position.
            noteWrapper.Note.transform.localPosition = newPosition;
            noteWrapper.Note.transform.localScale = newScale;
        }

        // Delete notes that are below floor.
        while (_activeNotes.Count > 0)
        {
            // Get top y pos of note.
            var firstNote = _activeNotes.First.Value.Note;
            var noteTopY = firstNote.transform.localPosition.y + firstNote.transform.localScale.y / 2f;

            if (noteTopY < BottomY) // Below the floor.
            {
                _activeNotes.RemoveFirst();
                Destroy(firstNote);
            }
            else
            {
                break;
            }
        }

        // Delete notes that are above ceiling.
        while (_activeNotes.Count > 0)
        {
            // Get bottom y pos of note.
            var lastNote = _activeNotes.Last.Value.Note;
            var noteBottomY = lastNote.transform.localPosition.y - lastNote.transform.localScale.y / 2f;

            if (noteBottomY > TopY) // Above the ceiling.
            {
                _activeNotes.RemoveLast();
                Destroy(lastNote);
            }
            else
            {
                break;
            }
        }
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
        foreach (var note in _activeNotes)
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
        print($"Note event accuracy: {accuracy}%\nNote time position: {eventTimeToCompareWith}ms\nPlayed note time distance: {msDist}ms");

        return accuracy;
    }
}
