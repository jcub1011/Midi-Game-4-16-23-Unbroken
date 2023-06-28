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
internal delegate void NoteChangeEvent(Note note, int noteIndex);

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
    /// <summary>
    /// Indicies cannot be lower than -1.
    /// </summary>
    /// <param name="lowerIndex"></param>
    /// <param name="upperIndex"></param>
    public void OverwriteIndicies(int lowerIndex, int upperIndex)
    {
        _lowerIndex = lowerIndex > -1 ? lowerIndex : -1;
        _upperIndex = upperIndex > -1 ? upperIndex : -1;
    }

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
        OverwriteIndicies(lowerIndex, upperIndex);
    }
    #endregion
}

internal class NoteManager
{
    #region Properties
    IndexManager _indexManager;

    readonly List<Note> _notes;
    List<int> _activeIndicies;
    long[] _previousBounds;

    public IndexChangeEvent AddedNextLatestNote;
    public IndexChangeEvent RemovedLatestNote;
    public IndexChangeEvent AddedNextEarliestNote;
    public IndexChangeEvent RemovedEarliestNote;
    public NoteChangeEvent NoteAdded;
    public NoteChangeEvent NoteRemoved;
    #endregion

    #region Constructors
    public NoteManager(List<Note> notes)
    {
        _indexManager = new(0, -1);

        _notes = notes;
    }
    #endregion

    #region Methods
    private bool InRange(long value, long lowerBound, long upperBound)
    {
        return lowerBound <= value && value <= upperBound;
    }

    private bool RangesOverlap(long[] firstRange, long[] secondRange)
    {
        if (InRange(firstRange[0], secondRange[0], secondRange[1])) return true;
        if (InRange(firstRange[1], secondRange[0], secondRange[1])) return true;
        if (InRange(secondRange[0], firstRange[0], firstRange[1])) return true;
        if (InRange(secondRange[1], firstRange[0], firstRange[1])) return true;
        return false;
    }

    /// <summary>
    /// Updates what notes are visible.
    /// </summary>
    /// <param name="playbackTick">Current tick of playback.</param>
    /// <param name="ticksBeforeStrike">Ticks notes can be seen before hitting strike bar.</param>
    /// <param name="ticksAfterStrike">Ticks notes can be seen after hitting stike bar.</param>
    public void UpdateVisibleNotes(long playbackTick, long ticksBeforeStrike, long ticksAfterStrike)
    {
        // Check if any of the notes from the past frame will be in this frame and seeks the playback if not.
        long[] newBounds = new long[2] { playbackTick - ticksBeforeStrike, playbackTick + ticksAfterStrike };
        if (_previousBounds == null || !RangesOverlap(_previousBounds, newBounds))
        {
            SeekTo(playbackTick);
        }

        AddVisibleEarliestNotes(playbackTick, ticksAfterStrike);
        AddVisibleLatestNotes(playbackTick, ticksBeforeStrike);
        RemoveHiddenLatestNotes(playbackTick, ticksBeforeStrike);
        RemoveHiddenEarliestNotes(playbackTick, ticksAfterStrike);

        _previousBounds = newBounds;
    }

    void AddVisibleLatestNotes(long playbackTick, long ticksBeforeStrike)
    {
        while(PeekNextLatest() != null)
        {
            // If note visible.
            long normalizedNoteTime = PeekNextLatest().Time - playbackTick;
            if (normalizedNoteTime <= ticksBeforeStrike) AddNextLatest();
            else break;
        }
    }

    void RemoveHiddenLatestNotes(long playbackTick, long ticksBeforeStrike)
    {
        while (PeekCurrentLatest() != null)
        {
            // Stop when notes are visible.
            long normalizedNoteTime = PeekCurrentLatest().Time - playbackTick;
            if (normalizedNoteTime <= ticksBeforeStrike) break;
            else RemoveCurrentLatest();
        }
    }

    void AddVisibleEarliestNotes(long playbackTick, long ticksAfterStrike)
    {
        while (PeekNextEarliest() != null)
        {
            // If note visible.
            long normalizedNoteTime =  playbackTick - PeekNextEarliest().EndTime;
            if (normalizedNoteTime <= ticksAfterStrike) AddNextEarliest();
            else break;
        }
    }

    void RemoveHiddenEarliestNotes(long playbackTick, long ticksAfterStrike)
    {
        while (PeekCurrentEarliest() != null)
        {
            // Stop when notes are visible.
            long normalizedNoteTime = playbackTick - PeekCurrentEarliest().EndTime;
            if (normalizedNoteTime <= ticksAfterStrike) break;
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

    private int FindIndexClosestToTick(long tickToFind, int lowerBound, int upperBound)
    {
        // Binary search.
        if (lowerBound > upperBound) return lowerBound;

        int middleIndex = (lowerBound + upperBound) / 2;
        long foundNoteTick = _notes[middleIndex].Time;

        if (foundNoteTick == tickToFind)
        {
            return middleIndex;
        }
        else if (foundNoteTick > tickToFind)
        {
            return FindIndexClosestToTick(tickToFind, lowerBound, middleIndex - 1);
        }
        else
        {
            return FindIndexClosestToTick(tickToFind, middleIndex + 1, upperBound);
        }
    }

    private bool ValidIndex(int index)
    {
        if (index < 0) return false;
        if (index >= _notes.Count) return false;
        return true;
    }

    private void RemoveNote(Note note, int index)
    {
        NoteRemoved?.Invoke(note, index);
    }

    private void AddNote(Note note, int index)
    {
        NoteAdded?.Invoke(note, index);
    }

    private void UpdateNoteListPastStrikeBar(int index, long tickBound)
    {
        for (; index >= 0; index--)
        {
            if (_notes[index].Time < tickBound) break;
            AddNote(_notes[index], index);
        }
    }

    private void RemoveHiddenNotesLeftOfIndex(int index, long tickBound)
    {
        for (; index >= 0; index --)
        {
            
        }
    }

    private void AddVisibleNotesRightOfIndex(int index, long tickBound)
    {
        for (;  index <= _notes.Count; index++)
        {
            if (_notes[index].Time > tickBound) break;
            AddNote(_notes[index], index);
        }
    }

    private void SeekTo(long tick)
    {
        Debug.Log($"Playback seeked to {tick}.");

        int indexNoteClosestToTick = FindIndexClosestToTick(tick);

        // Update indicies. The lower bound is increased by 1 so that when the new notes are added the
        // the note that was found also gets added. I know it doens't make any sense.
        _indexManager.OverwriteIndicies(indexNoteClosestToTick, indexNoteClosestToTick);
        Debug.Log($"Selected index: {indexNoteClosestToTick}");
    }
    #endregion
}

/*
internal class LaneManager
{
    #region Properties
    IntRange _noteRange;
    NoteLane[] _lanes;
    GameObject _wNotePrefab;
    GameObject _bNotePrefab;
    GameObject _lanePrefab;
    Transform _parent;
    #endregion

    #region Constructors
    public LaneManager(List<Note> notes, IntRange noteRange, Transform lanesParent, GameObject lanePrefab,
        GameObject whiteNotePrefab, GameObject blackNotePrefab)
    {
        _lanes = new NoteLane[noteRange.Len];

        for(int i = 0; i < _lanes.Length; i++)
        {
            _lanes[i] = UnityEngine.Object.Instantiate(lanePrefab).GetComponent<NoteLane>();
            _lanes[i].SetPosition(_displayInfo.GetLaneXPos(i), _displayInfo.IsWhiteNoteLane(i) ? 1 : 0);
        }
    }
    #endregion

    #region Methods

    #endregion
}*/

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

    void ClearLanes()
    {
        foreach(var lane in _lanes)
        {
            lane.ClearNotes();
        }
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
