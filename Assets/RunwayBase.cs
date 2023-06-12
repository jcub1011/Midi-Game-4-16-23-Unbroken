using System;
using System.Collections.Generic;
using UnityEngine;

internal class RunwayDisplayInfo
{
    #region Properties
    const float WHITE_NOTE_WIDTH_FACTOR = 1.0f;
    const float BLACK_NOTE_WIDTH_FACTOR = 0.8f;

    public static readonly IntRange FourtyNineKeyKeyboard = new(36, 84);
    public static readonly IntRange SixtyOneKeyKeyboard = new(36, 96);
    public static readonly IntRange SeventySixKeyKeyboard = new(28, 103);
    public static readonly IntRange EightyEightKeyKeyboard = new(21, 108);
    public static readonly IntRange MaxKeyKeyboard = new(0, 127);

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
    /// <param name="strikeBarHeight">Height of the strike bar as a percent of runway from the bottom
    /// where 1 = 100%.</param>
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
    bool IsWhiteNote(int noteNum)
    {
        // Note pattern repeats every 12 notes.
        return (noteNum % 12) switch
        {
            0 => true,
            1 => false,
            2 => true,
            3 => false,
            4 => true,
            5 => true,
            6 => false,
            7 => true,
            8 => false,
            9 => true,
            10 => false,
            11 => true,
            _ => false,
        };
    }

    public bool IsWhiteNoteLane(int laneIndex)
    {
        return IsWhiteNote(laneIndex + _range.Min);
    }

    void InitLaneWidthArray()
    {
        // Create array of lanes for each note number.
        _laneWidth = new float[_range.Len];
        _widthInWhiteNotes = 0;

        // Init multiplication factors for each lane.
        for (int i = 0; i < _laneWidth.Length; i++)
        {
            // Middle c is note number 60. 12 is number of notes in an octave.
            var noteNum = i + _range.Min;
            var normalizedNum = noteNum % 12;

            _widthInWhiteNotes += IsWhiteNote(normalizedNum) ? 1 : 0;
            // Only full size notes will add to the count.

            _laneWidth[i] = IsWhiteNote(normalizedNum) ? WHITE_NOTE_WIDTH_FACTOR : BLACK_NOTE_WIDTH_FACTOR;
        }
    }

    public float GetLaneXPos(int laneIndex)
    {
        var noteOffset = _laneWidth[laneIndex] / 2f - RunwayWidth / 2f;
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
        float msToReachStrikeBar, Transform lanesParent, GameObject lanePrefab,
        GameObject whiteNotePrefab, GameObject blackNotePrefab)
    {
        _noteRange = GetNoteRange(notes);
        _displayInfo = new(dimensions, strikeBarHeight, msToReachStrikeBar, _noteRange);
        _lanes = new NoteLane[_noteRange.Len];

        Debug.Log($"Range of notes {_noteRange.Len}.");

        // Calculate time offsets.
        float runwayEnterOffset = msToReachStrikeBar;
        float runwayExitOffset = _displayInfo.RunwayHeight * strikeBarHeight / _displayInfo.UnitsPerMs;

        // Initalize lanes.
        for (int i = 0; i < _lanes.Length; i++)
        {
            var newLane = UnityEngine.Object.Instantiate(lanePrefab, lanesParent);
            _lanes[i] = newLane.GetComponent<NoteLane>();
            _lanes[i].SetOffsets(runwayEnterOffset, runwayExitOffset);
            _lanes[i].SetNotePrefab(_displayInfo.IsWhiteNoteLane(i) ? whiteNotePrefab : blackNotePrefab);
            _lanes[i].SetPosition(_displayInfo.GetLaneXPos(i), _displayInfo.IsWhiteNoteLane(i) ? 1 : 0);
        }

        // Distribute notes.
        foreach (var note in notes)
        {
            _lanes[note.Number - _noteRange.Min].AddNote(note);
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

        Debug.Log($"Note range: {min} - {max}");

        var noteRange = new IntRange(min, max);
        IntRange rangeToReturn;

        if (RunwayDisplayInfo.FourtyNineKeyKeyboard.InRange(noteRange))
        {
            rangeToReturn = RunwayDisplayInfo.FourtyNineKeyKeyboard;
            UnityEngine.GameObject.Find("49KeyKeyboard").GetComponent<SpriteRenderer>().enabled = true;
        }
        else if (RunwayDisplayInfo.SixtyOneKeyKeyboard.InRange(noteRange))
        {
            rangeToReturn = RunwayDisplayInfo.SixtyOneKeyKeyboard;
            UnityEngine.GameObject.Find("61KeyKeyboard").GetComponent<SpriteRenderer>().enabled = true;
        }
        else if (RunwayDisplayInfo.SeventySixKeyKeyboard.InRange(noteRange))
        {
            rangeToReturn = RunwayDisplayInfo.SeventySixKeyKeyboard;
            UnityEngine.GameObject.Find("76KeyKeyboard").GetComponent<SpriteRenderer>().enabled = true;
        }
        else if (RunwayDisplayInfo.EightyEightKeyKeyboard.InRange(noteRange))
        {
            rangeToReturn = RunwayDisplayInfo.EightyEightKeyKeyboard;
            UnityEngine.GameObject.Find("88KeyKeyboard").GetComponent<SpriteRenderer>().enabled = true;
        }
        else
        {
            rangeToReturn = RunwayDisplayInfo.MaxKeyKeyboard;
            UnityEngine.GameObject.Find("128KeyKeyboard").GetComponent<SpriteRenderer>().enabled = true;
        }



        return rangeToReturn;
    }

    private void UpdateLaneDimensions()
    {
        for (int i = 0; i< _lanes.Length; ++i)
        {
            _lanes[i].Width = _displayInfo.GetLaneWidth(i);
            _lanes[i].SetPosition(_displayInfo.GetLaneXPos(i));
        }
    }

    public void UpdateRunway(float playbackTime)
    {
        if (_lanes == null) return;
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
        if (_lanes != null)
        {
            foreach(var lane in _lanes)
            {
                UnityEngine.Object.Destroy(lane.gameObject);
            }
        }

        _lanes = null;
        _displayInfo = null;
        _noteRange = null;
    } 
    #endregion
}
