using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class RunwayDisplayInfo
{
    #region Properties
    readonly IntRange _range; // Range of notes.
    readonly float _msToReachRunway;
    public readonly float _strikeBarHeightPercent; // Percent of screen from bottom.
    #endregion

    #region Getter Setter Methods
    public float UnitsPerMs { get; private set; } // How far the note moves per milisecond.
    public float NoteWidth { get; private set; } // In unity units.
    private float _runwayHeight;
    public float RunwayHeight
    {
        get { return _runwayHeight; }
        set
        {
            _runwayHeight = value;
            var distToStrikeBar = RunwayHeight - RunwayHeight * _strikeBarHeightPercent;
            UnitsPerMs = distToStrikeBar / _msToReachRunway;
        }
    }
    private float _runwayWidth;
    public float RunwayWidth
    {
        get { return _runwayWidth; }
        set
        {
            _runwayWidth = value;
            NoteWidth = RunwayWidth / _range.Len;
        }
    }
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a runway display info manager class.
    /// </summary>
    /// <param name="runwayDimensions">Inital dimensions of the runway.</param>
    /// <param name="strikeBarHeight">Height of the strike bar as a percent where 1 = 100%.</param>
    /// <param name="msLeadup">Time before the first note hits the strike bar.</param>
    /// <param name="range">Range of notes to display.</param>
    public RunwayDisplayInfo(float[] runwayDimensions, float strikeBarHeight, float msLeadup, IntRange range)
    {
        // Readonly properties.
        if (strikeBarHeight < 0f || strikeBarHeight > 1f)
        {
            throw new ArgumentOutOfRangeException("Height should be a decimal between 0 and 1 inclusively.");
        }
        _strikeBarHeightPercent = strikeBarHeight;
        _msToReachRunway = msLeadup;
        _range = range;

        RunwayWidth = runwayDimensions[0];
        RunwayHeight = runwayDimensions[1];
    }
    #endregion

    #region Methods
    public float GetNoteXPos(int noteNum)
    {
        return (_range.Min - noteNum) * NoteWidth;
    }
    #endregion
}

public class RunwayBase
{
    #region Properties
    IntRange _noteRange;
    RunwayDisplayInfo _displayInfo;
    NoteLane[] _lanes;
    GameObject _lanePrefab;
    #endregion

    #region Constructors
    public RunwayBase(List<NoteEvtData> notes, float[] dimensions, float strikeBarHeight,
        float msToReachStrikeBar, Transform lanesParent)
    {
        _noteRange = GetNoteRange(notes);
        _displayInfo = new(dimensions, strikeBarHeight, msToReachStrikeBar, _noteRange);
        _lanes = new NoteLane[_noteRange.Len];

        // Calculate time offsets.
        float runwayEnterOffset = msToReachStrikeBar;
        float runwayExitOffset = _displayInfo.RunwayHeight * strikeBarHeight / _displayInfo.UnitsPerMs;

        for (int i = 0; i < _lanes.Length; i++)
        {
            var newLane = UnityEngine.Object.Instantiate(_lanePrefab, lanesParent);
            _lanes[i] = newLane.GetComponent<NoteLane>();
            _lanes[i].Width = _displayInfo.NoteWidth;
            _lanes[i].XPos = _displayInfo.GetNoteXPos(i);
            _lanes[i].SetOffsets(runwayEnterOffset, runwayExitOffset);
        }
    }
    #endregion

    #region Methods
    IntRange GetNoteRange(List<NoteEvtData> notes)
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

    protected void DistributeNotes(List<NoteEvtData> notes)
    {
        foreach (var note in notes)
        {
            var index = note.number - _noteRange.Min;
            _lanes[index].AddNote(note);
        }
    }

    protected void UpdateRunway(float playbackTime)
    {
        foreach (var lane in _lanes)
        {
            lane.UpdateLane(playbackTime, _displayInfo.UnitsPerMs);
        }
    }

    protected void UpdateRunwayDimensions(float width, float height)
    {
        _displayInfo.RunwayWidth = width;
        _displayInfo.RunwayHeight = height;
    }
    #endregion
}
