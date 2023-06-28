using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;

internal class RunwayDisplayInfo
{
    #region Properties
    const float WHITE_NOTE_WIDTH_FACTOR = 0.95f;
    const float BLACK_NOTE_WIDTH_FACTOR = 0.75f;

    public static readonly IntRange FourtyNineKeyKeyboard = new(36, 84);
    public static readonly IntRange SixtyOneKeyKeyboard = new(36, 96);
    public static readonly IntRange SeventySixKeyKeyboard = new(28, 103);
    public static readonly IntRange EightyEightKeyKeyboard = new(21, 108);
    public static readonly IntRange MaxKeyKeyboard = new(0, 127);

    readonly IntRange _range; // Range of notes.
    readonly long _ticksToReachStrikeBar;
    public readonly float _strikeBarHeightPercent; // Percent of screen from bottom.
    float[] _laneWidth;
    float _whiteNoteWidth; // In unity units.
    int _numWhiteNoteLanes;
    #endregion

    #region Getter Setter Methods
    public float UnitsPerTick { get; private set; } // How far the note moves per tick.
    private float _runwayHeight;
    public float RunwayHeight
    {
        get { return _runwayHeight; }
        set
        {
            _runwayHeight = value;
            var distToStrikeBar = RunwayHeight - RunwayHeight * _strikeBarHeightPercent;
            UnitsPerTick = distToStrikeBar / _ticksToReachStrikeBar;
        }
    }
    private float _runwayWidth;
    public float RunwayWidth
    {
        get { return _runwayWidth; }
        set
        {
            _runwayWidth = value;
            _whiteNoteWidth = RunwayWidth / _numWhiteNoteLanes;
        }
    }

    public long TicksVisibleAboveStrike
    {
        get { return _ticksToReachStrikeBar; }
    }

    public long TicksVisibleBelowStrike
    {
        get
        {
            var strikeHeight = _strikeBarHeightPercent * RunwayHeight;
            return (long)(strikeHeight * UnitsPerTick);
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
    /// <param name="ticksLeadup">Ticks before the first note hits the strike bar.</param>
    /// <param name="range">Range of notes to display.</param>
    public RunwayDisplayInfo(float[] runwayDimensions, float strikeBarHeight, long ticksLeadup, IntRange range)
    {
        // Readonly properties.
        if (strikeBarHeight < 0f || strikeBarHeight > 1f)
        {
            throw new ArgumentOutOfRangeException("Height should be a decimal between 0 and 1 inclusively.");
        }
        _strikeBarHeightPercent = strikeBarHeight;
        _ticksToReachStrikeBar = ticksLeadup;
        _range = range;

        InitLaneWidthArray();
        RunwayWidth = runwayDimensions[0];
        RunwayHeight = runwayDimensions[1];
    }
    #endregion

    #region Methods
    bool IsWhiteNote(int noteNum)
    {
        // Middle c is note number 60. 12 is number of notes in an octave.
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
            _ => false
        };
    }

    public bool IsWhiteNoteLane(int laneIndex)
    {
        return IsWhiteNote(laneIndex + _range.Min);
    }

    float GetLaneWidthFactor(int laneIndex)
    {
        return IsWhiteNoteLane(laneIndex) ? WHITE_NOTE_WIDTH_FACTOR : BLACK_NOTE_WIDTH_FACTOR;
    }

    void InitLaneWidthArray()
    {
        // Create array of lanes for each note number.
        _laneWidth = new float[_range.Len];
        _numWhiteNoteLanes = 0;

        // Init multiplication factors for each lane.
        for (int i = 0; i < _laneWidth.Length; i++)
        {
            var noteNum = i + _range.Min;

            _numWhiteNoteLanes += IsWhiteNote(noteNum) ? 1 : 0;
            // Only full size notes will add to the count.

            _laneWidth[i] = IsWhiteNote(noteNum) ? WHITE_NOTE_WIDTH_FACTOR : BLACK_NOTE_WIDTH_FACTOR;
        }

        Debug.Log($"Width in white notes: {_numWhiteNoteLanes}");
    }

    public float GetNumWhiteLanesToLeft(int laneIndex)
    {
        var count = 0;

        for (int i = 0; i < laneIndex; i++)
        {
            if (IsWhiteNoteLane(i)) count++;
        }

        return count;
    }

    public float GetLaneXPos(int laneIndex)
    {
        float whiteNoteAdditionalOffset = (IsWhiteNoteLane(laneIndex) ? _whiteNoteWidth / 2f : 0f);
        float originOffset = -RunwayWidth / 2f;
        return GetNumWhiteLanesToLeft(laneIndex) * _whiteNoteWidth + originOffset + whiteNoteAdditionalOffset;
    }

    public float GetLaneWidth(int laneIndex)
    {
        return GetLaneWidthFactor(laneIndex) * _whiteNoteWidth;
    }
    #endregion
}

internal delegate void IndexChangeEvent(Note note);
internal delegate void ClearRunwayEvent();

internal class IndexManager
{
    #region Properties
    int _lowerIndex;
    int _upperIndex;

    public int LowerIndex
    {
        get
        {
            if (InvalidRange()) return -1;
            return _lowerIndex;
        }
    }

    public int NextLowerIndex
    {
        get
        {
            return _lowerIndex - 1;
        }
    }

    public int UpperIndex
    {
        get
        {
            if (InvalidRange()) return -1;
            return _upperIndex; 
        }
    }

    public int NextUpperIndex
    {
        get
        {
            return _upperIndex + 1;
        }
    }
    #endregion

    #region Methods
    public void IncrementUpperIndex()
    {
        _upperIndex++;
    }
    public void DecrementUpperIndex()
    {
        if (_upperIndex >= 0) _upperIndex--;
    }
    public void IncrementLowerIndex()
    {
        _lowerIndex++;
    }
    public void DecrementLowerIndex()
    {
        if (_lowerIndex >= 0) _lowerIndex--;
    }
    bool InvalidRange()
    {
        if (_lowerIndex < 0 || _upperIndex < 0) return false;
        return _lowerIndex > _upperIndex;
    }
    #endregion

    #region Constructors
    /// <summary>
    /// Indicies cannot be lower than -1.
    /// </summary>
    /// <param name="lowerIndex"></param>
    /// <param name="upperIndex"></param>
    public IndexManager(int lowerIndex, int upperIndex)
    {
        _lowerIndex = lowerIndex > -1 ? lowerIndex : -1;
        _upperIndex = upperIndex > -1 ? upperIndex : -1;
    }
    #endregion
}

internal class NoteManager
{
    #region Properties
    IndexManager _indexManager;

    readonly List<Note> _notes;

    public IndexChangeEvent AddedNextLatestNote;
    public IndexChangeEvent RemovedLatestNote;
    public IndexChangeEvent AddedNextEarliestNote;
    public IndexChangeEvent RemovedEarliestNote;
    public ClearRunwayEvent OnRunwayClear;
    #endregion

    #region Constructors
    public NoteManager(List<Note> notes)
    {
        _indexManager = new(0, -1);

        _notes = notes;
    }
    #endregion

    #region Methods
    /// <summary>
    /// Updates what notes are visible.
    /// </summary>
    /// <param name="playbackTick">Current tick of playback.</param>
    /// <param name="ticksBeforeStrike">Ticks notes can be seen before hitting strike bar.</param>
    /// <param name="ticksAfterStrike">Ticks notes can be seen after hitting stike bar.</param>
    public void UpdateVisibleNotes(long playbackTick, long ticksBeforeStrike, long ticksAfterStrike)
    {
        AddVisibleEarliestNotes(playbackTick, ticksAfterStrike);
        RemoveHiddenLatestNotes(playbackTick, ticksBeforeStrike);
        AddVisibleLatestNotes(playbackTick, ticksBeforeStrike);
        RemoveHiddenEarliestNotes(playbackTick, ticksAfterStrike);
    }

    void AddVisibleLatestNotes(long playbackTick, long ticksBeforeStrike)
    {
        while(PeekNextLatest() != null)
        {
            // If note visible.
            if (PeekNextLatest().Time - playbackTick <= ticksBeforeStrike) AddNextLatest();
            else break;
        }
    }

    void RemoveHiddenLatestNotes(long playbackTick, long ticksBeforeStrike)
    {
        while (PeekCurrentLatest() != null)
        {
            // Stop when notes are visible.
            if (PeekCurrentLatest().Time - playbackTick <= ticksBeforeStrike) break;
            else RemoveCurrentLatest();
        }
    }

    void AddVisibleEarliestNotes(long playbackTick, long ticksAfterStrike)
    {
        while (PeekNextEarliest() != null)
        {
            // If note visible.
            if (playbackTick - PeekNextEarliest().Time > ticksAfterStrike) AddNextEarliest();
            else break;
        }
    }

    void RemoveHiddenEarliestNotes(long playbackTick, long ticksAfterStrike)
    {
        while (PeekCurrentEarliest() != null)
        {
            // Stop when notes are visible.
            if (playbackTick - PeekCurrentEarliest().Time > ticksAfterStrike) break;
            else RemoveCurrentEarliest();
        }
    }
    #endregion

    #region Display Indicies Manager
    /// <summary>
    /// Gets the next latest note but doesn't add it to displayed notes.
    /// </summary>
    /// <returns>Null when none.</returns>
    private Note PeekNextLatest()
    {
        return ValidIndex(_indexManager.NextUpperIndex) ? _notes[_indexManager.NextUpperIndex] : null;
    }

    /// <summary>
    /// Adds next latest to displayed notes.
    /// </summary>
    private void AddNextLatest()
    {
        var note = PeekNextLatest();
        if (note != null)
        {
            _indexManager.IncrementUpperIndex();
            AddedNextLatestNote.Invoke(note);
        }
        else Debug.Log("Attempted to add non existent latest note.");
    }

    /// <summary>
    /// Gets the current latest note but doens't remove it from displayed notes.
    /// </summary>
    /// <returns>Null when none.</returns>
    private Note PeekCurrentLatest()
    {
        return ValidIndex(_indexManager.UpperIndex) ? _notes[_indexManager.UpperIndex] : null;
    }

    /// <summary>
    /// Removes current latest from displayed notes.
    /// </summary>
    private void RemoveCurrentLatest()
    {
        var note = PeekCurrentLatest();
        if (note != null)
        {
            _indexManager.DecrementUpperIndex();
            RemovedLatestNote.Invoke(note);
        }
        else Debug.Log("Attempted to remove non existent latest note.");
    }

    /// <summary>
    /// Gets the next earliest note but doesn't add it to displayed notes.
    /// </summary>
    /// <returns>Null when none.</returns>
    private Note PeekNextEarliest()
    {
        return ValidIndex(_indexManager.NextLowerIndex) ? _notes[_indexManager.NextLowerIndex] : null;
    }

    /// <summary>
    /// Gets the next earliest note and adds it to displayed notes.
    /// </summary>
    private void AddNextEarliest()
    {
        var note = PeekNextEarliest();
        if (note != null)
        {
            _indexManager.DecrementLowerIndex();
            AddedNextEarliestNote.Invoke(note);
        }
        else Debug.Log("Attempted to add non existent earliest note.");
    }

    /// <summary>
    /// Gets the current earliest note without removing it from displayed notes.
    /// </summary>
    /// <returns>Null when none.</returns>
    private Note PeekCurrentEarliest()
    {
        return ValidIndex(_indexManager.LowerIndex) ? _notes[_indexManager.LowerIndex] : null;
    }

    /// <summary>
    /// Gets the current earliest note and removes it from displayed notes.
    /// </summary>
    private void RemoveCurrentEarliest()
    {
        var note = PeekCurrentEarliest();
        if (note != null)
        {
            _indexManager.IncrementLowerIndex();
            RemovedEarliestNote.Invoke(note);
        }
        else Debug.Log("Attempted to remove non existent earliest note.");
    }

    private int FindIndexClosestToTick(long tick)
    {
        return FindIndexClosestToTick(tick, 0, _notes.Count - 1);
    }

    private int FindIndexClosestToTick(long tick, int lowerBound, int upperBound)
    {
        // Binary search.
        if (lowerBound == upperBound) return lowerBound;

        int middle = (lowerBound + upperBound) / 2;
        long noteTick = _notes[middle].Time;

        if (noteTick == tick)
        {
            return middle;
        }
        else if (noteTick < tick)
        {
            return FindIndexClosestToTick(tick, lowerBound, middle);
        }
        else
        {
            return FindIndexClosestToTick(tick, middle, upperBound);
        }
    }

    private bool ValidIndex(int index)
    {
        if (index < 0) return false;
        if (index >= _notes.Count) return false;
        return true;
    }

    private void SeekTo(long tick)
    {
        OnRunwayClear?.Invoke();

        int indexNoteClosestToTick = FindIndexClosestToTick(tick);

    }
    #endregion
}

public class RunwayBase
{
    #region Properties
    IntRange _noteRange;
    RunwayDisplayInfo _displayInfo;
    NoteLane[] _lanes;
    NoteManager _noteManager;
    GameObject _wNotePrefab;
    GameObject _bNotePrefab;
    Transform _laneParent;
    GameObject _lanePrefab;
    #endregion

    #region Constructors
    public RunwayBase(List<Note> notes, float[] dimensions, float strikeBarHeight,
        long ticksToReachStrikeBar, Transform lanesParent, GameObject lanePrefab,
        GameObject whiteNotePrefab, GameObject blackNotePrefab)
    {
        InitNoteManger(notes);
        InitLanes(notes, dimensions, strikeBarHeight, ticksToReachStrikeBar, 
            whiteNotePrefab, blackNotePrefab, lanesParent, lanePrefab);

        UpdateLaneDimensions();
    }

    void InitNoteManger(List<Note> notes)
    {
        _noteManager = new(notes)
        {
            // Subscribe to events.
            AddedNextLatestNote = NewLatestNote,
            AddedNextEarliestNote = NewEarliestNote,
            RemovedLatestNote = RemovedLatestNote,
            RemovedEarliestNote = RemovedEarliestNote
        };
    }

    void InitLanes(List<Note> notes, float[] dimensions, float strikeBarHeight, long ticksToReachStrikeBar,
        GameObject whiteNotePrefab, GameObject blackNotePrefab, Transform lanesParent, GameObject lanePrefab)
    {
        _noteRange = GetNoteRange(notes);
        Debug.Log($"Range of notes {_noteRange.Len}.");
        _displayInfo = new(dimensions, strikeBarHeight, ticksToReachStrikeBar, _noteRange);
        _lanes = new NoteLane[_noteRange.Len];

        // Init note prefabs.
        _wNotePrefab = whiteNotePrefab;
        _bNotePrefab = blackNotePrefab;

        // Initalize lanes.
        for (int i = 0; i < _lanes.Length; i++)
        {
            var newLane = UnityEngine.Object.Instantiate(lanePrefab, lanesParent);
            _lanes[i] = newLane.GetComponent<NoteLane>();
            _lanes[i].SetPosition(_displayInfo.GetLaneXPos(i), _displayInfo.IsWhiteNoteLane(i) ? 1 : 0);
        }
    }
    #endregion

    #region Event Handlers
    void NewEarliestNote(Note note)
    {
        Debug.Log("Adding new earliest note.");
        var laneIndex = note.NoteNumber - _noteRange.Min;
        _lanes[laneIndex].AddNoteFront(note, 
            _displayInfo.IsWhiteNoteLane(laneIndex) ? _wNotePrefab : _bNotePrefab);
    }

    void RemovedEarliestNote(Note note)
    {
        Debug.Log("Removing earliest note.");
        var laneIndex = note.NoteNumber - _noteRange.Min;
        _lanes[laneIndex].RemoveNoteFront();
    }

    void NewLatestNote(Note note)
    {
        Debug.Log("Adding new latest note.");
        var laneIndex = note.NoteNumber - _noteRange.Min;
        _lanes[laneIndex].AddNoteLast(note,
            _displayInfo.IsWhiteNoteLane(laneIndex) ? _wNotePrefab : _bNotePrefab);
    }

    void RemovedLatestNote(Note note)
    {
        Debug.Log("Removing latest note.");
        var laneIndex = note.NoteNumber - _noteRange.Min;
        _lanes[laneIndex].RemoveNoteLast();
    }
    #endregion

    #region Methods
    IntRange GetNoteRange(List<Note> notes)
    {
        short min = short.MaxValue;
        short max = short.MinValue;

        foreach (var note in notes)
        {
            if (note.NoteNumber < min) min = note.NoteNumber;
            if (note.NoteNumber > max) max = note.NoteNumber;
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

    public void UpdateRunway(long playbackTick)
    {
        _noteManager.UpdateVisibleNotes(playbackTick, _displayInfo.TicksVisibleAboveStrike, _displayInfo.TicksVisibleBelowStrike);

        if (_lanes == null) return;
        foreach (var lane in _lanes)
        {
            lane.UpdateLane(playbackTick, _displayInfo.UnitsPerTick, _displayInfo.RunwayHeight / 2f);
        }
    }

    public void UpdateRunwayDimensions(float width, float height)
    {
        _displayInfo.RunwayWidth = width;
        _displayInfo.RunwayHeight = height;

        UpdateLaneDimensions();
    }
    public void ChangeNoteList(List<Note> notes)
    {
        Clear();
        float[] dimensions = new float[2] { _displayInfo.RunwayWidth, _displayInfo.RunwayHeight };
        InitLanes(notes, dimensions, _displayInfo._strikeBarHeightPercent, _displayInfo.TicksVisibleAboveStrike,
            _wNotePrefab, _bNotePrefab, _laneParent, _lanePrefab);
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
        _noteRange = null;
    }
    #endregion
}
