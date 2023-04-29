using Melanchall.DryWetMidi.MusicTheory;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using Utils;

struct LaneWrapper
{
    public GameObject Lane;
    public LaneScript Script;
}

public class LaneScript : MonoBehaviour
{
    Queue<NoteBlock> _notes = new Queue<NoteBlock>();
    PriorityQueue<float, float> _noteOnCollisionQueue = new PriorityQueue<float, float>();
    PriorityQueue<float, float> _noteOffCollisionQueue = new PriorityQueue<float, float>();
    float _width = 0f;
    float _height = 0f;
    float _unitsPerMs = 0f;
    float _msForgiveness;
    float _timeToReachStrike = 0f;
    float _strikeHeight = 0f;
    float BottomY {
        get
        {
            return -_height / 2;
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
        _timeToReachStrike = timeToReachStrike;
        _msForgiveness = Forgiveness;
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
        StrikeKey.transform.GetChild(0).localScale = new Vector3(_width, _msForgiveness * _unitsPerMs, 1);
        StrikeKey.transform.localPosition = new Vector3(0, BottomY + _msForgiveness * _unitsPerMs / 2, 1);
    }

    public void AddNote(NoteBlock newNote)
    {
        newNote.GetNote().transform.parent = transform;
        var priority = newNote.NoteOnTime;
        _notes.Enqueue(newNote);
        _noteOnCollisionQueue.Enqueue(newNote.NoteOnTime, priority);
        _noteOffCollisionQueue.Enqueue(newNote.NoteOffTime, priority);
    }

    /// <summary>
    /// Updates the positions of each note and deletes notes no longer visible.
    /// </summary>
    /// <param name="CurrentPlaybackTimeMs">Current time of playback.</param>
    public void UpdateNotePositions(float CurrentPlaybackTimeMs)
    {
        // Update positions for all managed notes.
        foreach (var note in _notes)
        {
            // Get new scale.
            var newScale = new Vector3();
            newScale.x = _width;
            newScale.y = note.Length * _unitsPerMs;
            newScale.z = 1;

            // Get new position.
            var newPosition = new Vector3();
            newPosition.x = 0;
            newPosition.y = _height / 2 - (_unitsPerMs * (CurrentPlaybackTimeMs - note.TimePosition)) + note.NoteHeight / 2;
            newPosition.z = 0;

            note.UpdateNotePos(newPosition, newScale);
        }

        // Delete notes that are no longer visible.
        while (_notes.Count > 0)
        {
            if (_notes.Peek().TopY < BottomY) // Below the floor.
            {
                Destroy(_notes.Dequeue().GetNote());
            }
            else
            {
                break;
            }
        }
    }

    private float CalculateAccuracy(float differenceMs)
    {
        if (differenceMs < 0f) differenceMs *= -1f; // Make it positive.

        if (differenceMs == 0)
        {
            return 100f;
        }

        if (differenceMs > _msForgiveness)
        {
            return 0f;
        }

        float accuracy = 100f - (differenceMs / _msForgiveness) * 100f;

        return accuracy;
    }

    float AbsTimeDiff(float time1, float time2)
    {
        var diff = time1 - time2;
        if (diff < 0f) diff *= -1;
        return diff;
    }

    /// <summary>
    /// Returns percent accuracy.
    /// </summary>
    /// <param name="Time">Current playback time.</param>
    /// <param name="NoteOnEvent">True if note on event, otherwise note off event.</param>
    /// <returns>Accuracy</returns>
    public float NoteEventAccuracy(float Time, bool NoteOnEvent)
    {
        float eventTimeToCompareWith = 2f * _msForgiveness;

        print($"Current time: {Time}");

        if (NoteOnEvent)
        {
            print($"Checking note on accuracy. Unplayed notes {_noteOnCollisionQueue.Count}");
            while (_noteOnCollisionQueue.Count > 0)
            {
                var eventTimeDiff = Time - _noteOnCollisionQueue.Peek();
                print($"Next playable on note time: {_noteOnCollisionQueue.Peek()}");

                // If below strike bar.
                if (eventTimeDiff > _msForgiveness)
                {
                    eventTimeToCompareWith = _noteOnCollisionQueue.Dequeue();
                    print("Skipping note.");
                    continue;
                }

                // If above the strike bar.
                if (eventTimeDiff < -_msForgiveness)
                {
                    eventTimeToCompareWith = _noteOnCollisionQueue.Peek();
                    break;
                }

                // When within strike bar.
                eventTimeToCompareWith = _noteOnCollisionQueue.Dequeue();
                break;
            }
        }
        else
        {
            print($"Checking note off accuracy. Unplayed notes {_noteOffCollisionQueue.Count}");
            while (_noteOffCollisionQueue.Count > 0)
            {
                var eventTimeDiff = Time - _noteOffCollisionQueue.Peek();
                print($"Next playable off note time: {_noteOffCollisionQueue.Peek()}");

                // If below strike bar.
                if (eventTimeDiff > _msForgiveness)
                {
                    eventTimeToCompareWith = _noteOffCollisionQueue.Dequeue();
                    print("Skipping note.");
                    continue;
                }

                // If above the strike bar.
                if (eventTimeDiff < -_msForgiveness)
                {
                    eventTimeToCompareWith = _noteOffCollisionQueue.Peek();
                    break;
                }

                // When within strike bar.
                eventTimeToCompareWith = _noteOffCollisionQueue.Dequeue();
                break;
            }
        }

        var msDist = eventTimeToCompareWith - Time;
        var accuracy = CalculateAccuracy(msDist);
        print($"Note event accuracy: {accuracy}%\nNote time position: {eventTimeToCompareWith}ms\nPlayed note time distance: {msDist}ms");

        return accuracy;
    }
}
