using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Collections.Generic;
using UnityEngine;

public class Runway : MonoBehaviour
{
    IntRange _noteRange;
    float _distPerMs; // How far the note moves per milisecond.
    float _msToReachRunway;
    float _noteWidth; // In unity units.
    float _height; // In unity units.
    float _width; // In unity units.
    float _strikeBarHeight; // In unity units.
    List<Note> _notes = new(); // Notes managed by this runway.
    long _earliestNoteIndex = -1; // Note with greatest playback position.
    long _latestNoteIndex = -1; // Note with lowest playback position.
    LaneWrapper[] _lanes;
    public GameObject StrikeBar;
    public GameObject LanePrefab;

    /// <summary>
    /// Initalizes a runway.
    /// </summary>
    /// <param name="notes">Notes to be managed by this runway.</param>
    /// <param name="dimensions">Height and width of runway in unity units. [width, height]</param>
    /// <param name="strikeBarHeight">Height of strikebar from the bottom of the runway in unity units.</param>
    /// <param name="msToReachStrikeBar">How long it takes to reach the strikebar.</param>
    /// <param name="speedMulti">Multiplier of speed. 1 is normal speed.</param>
    public void Init(List<Note> notes, float[] dimensions, float strikeBarHeight, float msToReachStrikeBar, float speedMulti = 1)
    {
        // Init private members.
        _noteRange = getNoteRange(notes);
        print($"Initalizing runway. Note Range: {_noteRange.Min} - {_noteRange.Max}");
        _strikeBarHeight = strikeBarHeight;

        // Get notespeed.
        var DistToStrikebar = dimensions[1] - _strikeBarHeight;
        _msToReachRunway = msToReachStrikeBar;
        _distPerMs = DistToStrikebar / _msToReachRunway; // units/ms
        print($"Note speed: {_distPerMs} (units/milisecond)");

        UpdateNoteDisplayInfo(dimensions);

        // Create lanes.
        _lanes = new LaneWrapper[_noteRange.Len];

        for (int i = 0; i < _lanes.Length; i++)
        {
            // Create lane.
            var newLane = new LaneWrapper();
            newLane.Lane = Instantiate(LanePrefab, transform);
            newLane.Script = newLane.Lane.transform.GetComponent<Lane>();

            // Lane position;
            var posX = GetNoteXPos((short)(_noteRange.Min + i));

            newLane.Script.Init(new float[2] { _noteWidth, _height }, strikeBarHeight, posX, msToReachStrikeBar);

            _lanes[i] = newLane;
        }
    }

    IntRange getNoteRange(List<Note> notes)
    {
        short min = short.MaxValue;
        short max = short.MinValue;

        foreach (var note in notes)
        {
            if (note.NoteNumber < min) min = note.NoteNumber;
            if (note.NoteNumber > max) max = note.NoteNumber;
        }

        return new IntRange(min, max);
    }

    /// <summary>
    /// Gets an x position using a note number.
    /// </summary>
    /// <param name="noteNum">The note number.</param>
    /// <returns>X position.</returns>
    float GetNoteXPos(short noteNum)
    {
        return (float)(_noteWidth * (noteNum - _noteRange.Min) + _noteWidth / 2.0 - _width / 2.0); // Half width offset because anchor is in the middle.
    }

    /// <summary>
    /// Updates info necessary for notes to display properly on the runway.
    /// </summary>
    /// <param name="newDimensions">The new dimensions of the runway.</param>
    public void UpdateNoteDisplayInfo(float[] newDimensions)
    {
        // Update runway dimensions.
        _width = newDimensions[0];
        _height = newDimensions[1];
        _distPerMs = (_height - _strikeBarHeight) / _msToReachRunway; // units/ms
        print($"Note speed: {_distPerMs} (units/milisecond)");

        // Update note width.
        _noteWidth = _width / _noteRange.Len;

        // Update inner strike bars.
        StrikeBar.transform.localScale = new Vector3(_width, GameData.Forgiveness * _distPerMs, 1);
        // Position outside strike bar.
        var barY = -_height / 2 + _strikeBarHeight;
        StrikeBar.transform.localPosition = new Vector3(0, barY, 1);
    }

    /// <summary>
    /// Adds a note block to its respective lane.
    /// </summary>
    /// <param name="note">The note a lane.</param>
    public void AddNoteToLane(Note note)
    {
        // Check if valid note.
        if (!_noteRange.InRange(note.NoteNumber))
        {
            print($"Note {note.NoteNumber} outside range [{_noteRange.Min}, {_noteRange.Max}].");
            return;
        }

        // Add to lane.
        _lanes[note.NoteNumber - _noteRange.Min].Script.AddNote(note);
    }

    public void UpdateRunway(float playbackTime)
    {
        if (_notes == null || _notes.Count < 1) return;


    }

    /// <summary>
    /// Updates each lane.
    /// </summary>
    /// <param name="PlaybackTime">The time since the beginning of playback. (in miliseconds)</param>
    public void UpdateLanes(float PlaybackTime)
    {
        if (Lanes == null)
        {
            return;
        }

        while (DisplayQueue.Count > 0)
        {
            AddNoteToLane(DisplayQueue.Dequeue());
        }

        for (var i = 0; i < Lanes.Length; i++)
        {
            // Update each lane.
            Lanes[i].Script.UpdateDimensions(new float[2] { NoteWidth, Height }, GetNoteXPos((short)(_noteRange.Min + i)));
            Lanes[i].Script.UpdateNotePositions(PlaybackTime);
        }
    }

    public float GetNoteInputAccuracy(float PlaybackTime, NoteOnEvent note)
    {
        return Lanes[note.NoteNumber - _noteRange.Min].Script.NoteEventAccuracy(PlaybackTime, true);
    }

    public float GetNoteInputAccuracy(float PlaybackTime, NoteOffEvent note)
    {
        return Lanes[note.NoteNumber - _noteRange.Min].Script.NoteEventAccuracy(PlaybackTime, false);
    }

    private void Dispose()
    {
        Lanes = null;
        DisplayQueue = new();
    }

    private void OnApplicationQuit()
    {
        Dispose();
    }

    private void OnDisable()
    {
        Dispose();
    }
}
