using Melanchall.DryWetMidi.MusicTheory;
using System.Collections.Generic;
using UnityEngine;

struct LaneWrapper
{
    public GameObject Lane;
    public LaneScript Script;
}

public class LaneScript : MonoBehaviour
{
    Queue<NoteBlock> _notes = new Queue<NoteBlock>();
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
        transform.localPosition = new Vector3(xPos, 0, 1);

        // Update strike range.
        StrikeKey.transform.GetChild(0).localScale = new Vector3(_width, _msForgiveness * _unitsPerMs, 1);
        StrikeKey.transform.localPosition = new Vector3(0, BottomY + _msForgiveness * _unitsPerMs / 2, 1);
    }

    public void AddNote(NoteBlock newNote)
    {
        newNote.GetNote().transform.parent = transform;
        _notes.Enqueue(newNote);
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

        print($"Distance in ms from actual: {differenceMs}ms");

        if (differenceMs == 0)
        {
            print($"Perfect accuracy.");
            return 100f;
        }

        if (differenceMs > _msForgiveness)
        {
            print($"Total miss.");
            return 0f;
        }

        float accuracy = 100f - (differenceMs / _msForgiveness) * 100f;

        return accuracy;
    }

    /// <summary>
    /// Returns percent accuracy.
    /// </summary>
    /// <param name="Time">Current playback time.</param>
    /// <param name="NoteOnEvent">True if note on event, otherwise note off event.</param>
    /// <returns>Accuracy</returns>
    public float NoteEventAccuracy(float Time, bool NoteOnEvent)
    {
        if (_notes.Count == 0)
        {
            print($"Attempted to check note collision but lane is empty.");
            return 0f;
        }

        // Finds the closest unplayed note to the bottom of the strikebar to check accuracy against.
        var noteCache = _notes.ToArray();

        int closestNoteIndex = -1;
        float currentNoteTimeDist;
        float closestNoteTimeDist = _msForgiveness;

        for (int i = 0; i < noteCache.Length; i++)
        {
            currentNoteTimeDist = Time - (NoteOnEvent ? noteCache[i].NoteOnTime : noteCache[i].NoteOffTime);

            // If note has already been played.
            if (NoteOnEvent ? noteCache[i].NoteOnPlayed : noteCache[i].NoteOffPlayed)
            {
                continue;
            }

            // If note on time is within forgiveness range.
            if (-_msForgiveness < currentNoteTimeDist && currentNoteTimeDist < _msForgiveness)
            {
                // If closer to the bottom of the strike bar.
                if (currentNoteTimeDist < closestNoteTimeDist)
                {
                    closestNoteIndex = i;
                    closestNoteTimeDist = currentNoteTimeDist;
                }
            }
        }

        if (closestNoteIndex == -1) // -1 means no note within forgiveness range was found.
        {
            print("Total miss.");
            return 0f;
        }

        if (closestNoteTimeDist < 0f) closestNoteTimeDist *= -1f; // Make positive.

        if (closestNoteTimeDist == 0)
        {
            print($"Perfect accuracy.");
            return 100f;
        }

        float Accuracy = 100f - (closestNoteTimeDist / _msForgiveness) * 100f;

        if (NoteOnEvent)
        {

            print($"Note on event accuracy: {Accuracy}%\nNote time position: {noteCache[closestNoteIndex].NoteOnTime}ms\nPlayed note time distance: {closestNoteTimeDist}ms");
        }
        else print($"Note on event accuracy: {Accuracy}%\nNote time position: {noteCache[closestNoteIndex].NoteOffTime}ms\nPlayed note time distance: {closestNoteTimeDist}ms");

        // Update queue.
        _notes.Clear();
        for (int i = 0; i < noteCache.Length; i++)
        {
            // If this was the played note mark it as such.
            if (i == closestNoteIndex)
            {
                if (NoteOnEvent)
                {
                    noteCache[i].NoteOnPlayed = true;
                } else
                {
                    noteCache[i].NoteOffPlayed = true;
                }
            }

            _notes.Enqueue(noteCache[i]);
        }

        return Accuracy;
    }
}
