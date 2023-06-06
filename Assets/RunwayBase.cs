using System;
using System.Collections.Generic;
using UnityEngine;

internal class RunwayDisplayInfo
{
    #region Properties
    const float WHITE_NOTE_WIDTH_FACTOR = 1.0f;
    const float BLACK_NOTE_WIDTH_FACTOR = 0.8f;

    readonly IntRange _range; // Range of notes.
    readonly float _msToReachRunway;
    public readonly float _strikeBarHeightPercent; // Percent of screen from bottom.
    float[] _laneWidth;
    float _whiteNoteWidth; // In unity units.
    int _widthInWhiteNotes;
    #endregion

    #region Getter Setter Methods
    public float UnitsPerMs { get; private set; } // How far the note moves per milisecond.
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
            _whiteNoteWidth = RunwayWidth / _widthInWhiteNotes;
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

        InitLaneWidthArray();
        RunwayWidth = runwayDimensions[0];
        RunwayHeight = runwayDimensions[1];
    }
    #endregion

    #region Methods
    void InitLaneWidthArray()
    {
        // Create array of lanes for each note number.
        _laneWidth = new float[_range.Len];
        _widthInWhiteNotes = 0;

        // Init dict for each note width factor.
        Dictionary<int, float> widthFactor = new()
        {
            { 0, WHITE_NOTE_WIDTH_FACTOR },
            { 1, BLACK_NOTE_WIDTH_FACTOR },
            { 2, WHITE_NOTE_WIDTH_FACTOR },
            { 3, BLACK_NOTE_WIDTH_FACTOR },
            { 4, WHITE_NOTE_WIDTH_FACTOR },
            { 5, WHITE_NOTE_WIDTH_FACTOR },
            { 6, BLACK_NOTE_WIDTH_FACTOR },
            { 7, WHITE_NOTE_WIDTH_FACTOR },
            { 8, BLACK_NOTE_WIDTH_FACTOR },
            { 9, WHITE_NOTE_WIDTH_FACTOR },
            { 10, BLACK_NOTE_WIDTH_FACTOR },
            { 11, WHITE_NOTE_WIDTH_FACTOR }
        };

        // Init multiplication factors for each lane.
        for (int i = 0; i < _laneWidth.Length; i++)
        {
            // Middle c is note number 60. 12 is number of notes in an octave.
            var noteNum = i + _range.Min;
            var normalizedNum = noteNum % 12;

            _widthInWhiteNotes += (int)widthFactor[normalizedNum];
            // Only full size notes will add to the count.

            _laneWidth[i] = widthFactor[normalizedNum];
        }
    }

    public float GetLaneXPos(int laneIndex)
    {
        var noteOffset = _laneWidth[laneIndex] / 2f;
        var whiteNotesToTheLeft = 0;
        
        // Count number of white notes to the left of the current note.
        for (int i = 0; i < laneIndex; i++)
        {
            whiteNotesToTheLeft += (int)_laneWidth[i];
        }

        return whiteNotesToTheLeft * _whiteNoteWidth + noteOffset;
    }

    public float GetLaneWidth(int laneIndex)
    {
        return _laneWidth[laneIndex] * _whiteNoteWidth;
    }
    #endregion
}

public class RunwayBase
{
    #region Properties
    IntRange _noteRange;
    RunwayDisplayInfo _displayInfo;
    NoteLane[] _lanes;
    #endregion

    #region Constructors
    public RunwayBase(List<NoteEvtData> notes, float[] dimensions, float strikeBarHeight,
        float msToReachStrikeBar, Transform lanesParent, GameObject lanePrefab)
    {
        _noteRange = GetNoteRange(notes);
        _displayInfo = new(dimensions, strikeBarHeight, msToReachStrikeBar, _noteRange);
        _lanes = new NoteLane[_noteRange.Len];

        // Calculate time offsets.
        float runwayEnterOffset = msToReachStrikeBar;
        float runwayExitOffset = _displayInfo.RunwayHeight * strikeBarHeight / _displayInfo.UnitsPerMs;

        for (int i = 0; i < _lanes.Length; i++)
        {
            var newLane = UnityEngine.Object.Instantiate(lanePrefab, lanesParent);
            _lanes[i] = newLane.GetComponent<NoteLane>();
            _lanes[i].SetOffsets(runwayEnterOffset, runwayExitOffset);
        }

        UpdateLaneDimensions();
    }
    #endregion

    #region Methods
    IntRange GetNoteRange(List<NoteEvtData> notes)
    {
        short min = short.MaxValue;
        short max = short.MinValue;

        foreach (var note in notes)
        {
            if (note.Number < min) min = note.Number;
            if (note.Number > max) max = note.Number;
        }

        return new IntRange(min, max);
    }

    private void UpdateLaneDimensions()
    {
        for (int i = 0; i< _lanes.Length; ++i)
        {
            _lanes[i].Width = _displayInfo.GetLaneWidth(i);
            _lanes[i].XPos = _displayInfo.GetLaneXPos(i);
        }
    }

    public void UpdateRunway(float playbackTime)
    {
        foreach (var lane in _lanes)
        {
            lane.UpdateLane(playbackTime, _displayInfo.UnitsPerMs);
        }
    }

    public void UpdateRunwayDimensions(float width, float height)
    {
        _displayInfo.RunwayWidth = width;
        _displayInfo.RunwayHeight = height;

        UpdateLaneDimensions();
    }

    public void Clear()
    {
        foreach(var lane in _lanes)
        {
            UnityEngine.Object.Destroy(lane.gameObject);
        }

        _lanes = null;
        _displayInfo = null;
        _noteRange = null;
    } 
    #endregion
}
