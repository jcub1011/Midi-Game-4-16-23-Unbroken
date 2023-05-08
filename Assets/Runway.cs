using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
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
    List<NoteEvtData> _notes = new(); // Notes managed by this runway.
    IntRange _displayRange = new(0, -1);
    LinkedList<int> _displayIndicies = new(); // Stores indexes of notes that are currently being displayed.
    int _upcommingMaxNoteIndex = 0;
    int _previousMinNoteIndex = 0;
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
    public void Init(List<NoteEvtData> notes, float[] dimensions, float strikeBarHeight, float msToReachStrikeBar, float speedMulti = 1)
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

    IntRange getNoteRange(List<NoteEvtData> notes)
    {
        short min = short.MaxValue;
        short max = short.MinValue;

        foreach (var note in notes)
        {
            if (note.number < min) min = note.number;
            if (note.number > max) max = note.number;
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
    public void AddNoteToLane(NoteEvtData note)
    {
        // Check if valid note.
        if (!_noteRange.InRange(note.number))
        {
            print($"Note {note.number} outside range [{_noteRange.Min}, {_noteRange.Max}].");
            return;
        }

        // Add to lane.
        _lanes[note.number - _noteRange.Min].Script.AddNote(note);
    }

    public void UpdateRunway(float playbackTime)
    {
        if (_notes == null || _notes.Count < 1) return;
        UpdateDisplayRange(playbackTime);

    }

    private void IncrementDisplayIndexMax()
    {
        if (_upcommingMaxNoteIndex == _notes.Count) return;

        _displayIndicies.AddLast(_upcommingMaxNoteIndex);
        _lanes[_notes[_upcommingMaxNoteIndex].number - _noteRange.Min].Script.AddNote(_notes[_upcommingMaxNoteIndex]);

        _upcommingMaxNoteIndex++;
    }

    private void DecrementDisplayIndexMax()
    {
        if (_displayIndicies.Count == 0) return;

        _displayIndicies.RemoveLast();
        _upcommingMaxNoteIndex--;
    }

    private void IncrementDisplayIndexMin()
    {
        if (_previousMinNoteIndex == _notes.Count) return;
        if (_displayIndicies.Count == 0) return;

        _displayIndicies.RemoveFirst();
    }

    private void DecrementDisplayIndexMin()
    {
        if (_displayIndicies.Count == 0) return;
        if (_previousMinNoteIndex < 0) return;

        _displayIndicies.AddFirst(_previousMinNoteIndex);
        _lanes[_notes[_previousMinNoteIndex].number - _noteRange.Min].Script.AddNote(_notes[_previousMinNoteIndex]);

        _previousMinNoteIndex--;
    }

    private void UpdateDisplayIndicies(float playbackTime)
    {
        if (_notes == null || _notes.Count == 0) return;
        float _runwayUpperBound = playbackTime + _msToReachRunway;
        float _runwayLowerBound = playbackTime - _strikeBarHeight * _distPerMs;

        // Base case.
        if (_displayIndicies.Count == 0)
        {
            if (_notes[_upcommingMaxNoteIndex].onTime < _runwayUpperBound)
            {
                _displayIndicies.AddFirst(_upcommingMaxNoteIndex);
            }
            else
            {
                return;
            }
        }


    }

    private void UpdateDisplayRange(float playbackTime)
    {
        if (_notes == null || _notes.Count == 0) return;
        float _runwayUpperBound = playbackTime + _msToReachRunway;
        float _runwayLowerBound = playbackTime - _strikeBarHeight * _distPerMs;
        var prevMax = _displayRange.Max;
        var prevMin = _displayRange.Min;
        var newMax = prevMax;
        var newMin = prevMin;

        if (prevMax == -1)
        {
            prevMax = 0;
        }

        // If last item in display range is in runway.
        if (_runwayUpperBound > _notes[prevMax].onTime)
        {
            // Add notes that aren't being displayed to their respective lanes.
            int iterator = prevMax + 1;
            while (_notes[iterator].onTime < _runwayUpperBound)
            {
                // Add note to range.
                _lanes[_notes[iterator].number - _noteRange.Min].Script.AddNote(_notes[iterator]);
                newMax++;
                iterator++;
                if (iterator >= _notes.Count) break;
            }
        }
        else
        {
            int iterator = prevMax - 1;
            while (_notes[iterator].onTime > _runwayUpperBound)
            {
                // Remove notes from range.
                newMax--;
                iterator--;
                if (iterator < 0) break;
            }
        }

        // If first item in display range is in runway.
        if (_runwayLowerBound < _notes[prevMin].offTime)
        {
            // Add notes that aren't being displayed to their respective lanes.
            int iterator = prevMin - 1;
            while (_notes[iterator].offTime > _runwayLowerBound)
            {
                _lanes[_notes[iterator].number - _noteRange.Min].Script.AddNote(_notes[iterator]);
                newMin--;
                iterator--;
                if (iterator < 0) break;
            }
        } 
        else
        {
            int iterator = prevMin + 1;
            while (_notes[iterator].offTime < _runwayLowerBound)
            {
                // remove notes from range.
                newMin++;
                iterator++;
                if (iterator >= _notes.Count) break;
            }
        }

        _displayRange = new IntRange(newMin, newMax);
    }

    /// <summary>
    /// Updates each lane.
    /// </summary>
    /// <param name="PlaybackTime">The time since the beginning of playback. (in miliseconds)</param>
    public void UpdateLanes(float PlaybackTime)
    {
        if (_lanes == null)
        {
            return;
        }

        // Update indicies of playback.


        for (var i = 0; i < _lanes.Length; i++)
        {
            // Update each lane.
            _lanes[i].Script.UpdateDimensions(new float[2] { _noteWidth, _height }, GetNoteXPos((short)(_noteRange.Min + i)));
            _lanes[i].Script.UpdateNotePositions(PlaybackTime);
        }
    }

    public float GetNoteInputAccuracy(float PlaybackTime, float forgiveness, NoteOnEvent note)
    {
        return _lanes[note.NoteNumber - _noteRange.Min].Script.NoteEventAccuracy(PlaybackTime, forgiveness, true);
    }

    public float GetNoteInputAccuracy(float PlaybackTime, float forgiveness, NoteOffEvent note)
    {
        return _lanes[note.NoteNumber - _noteRange.Min].Script.NoteEventAccuracy(PlaybackTime, forgiveness, false);
    }

    private void Dispose()
    {
        _lanes = null;
        _notes = null;
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
