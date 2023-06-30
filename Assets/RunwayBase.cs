using Melanchall.DryWetMidi.Interaction;
using MIDIGame.Lane;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MIDIGame.Runway
{
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

    internal class NoteManager
    {
        #region Properties
        readonly List<Note> _notes;

        readonly Dictionary<int, Note> _activeNotes;
        int _lowestIndex;
        int _highestIndex;

        public NoteChangeEvent NoteAdded;
        public NoteChangeEvent NoteRemoved;
        #endregion

        #region Constructors
        public NoteManager(List<Note> notes)
        {
            _notes = notes;
            _activeNotes = new();
            _lowestIndex = -1;
            _highestIndex = -1;
        }
        #endregion

        #region Methods
        void RemoveNote(Note note, int noteIndex)
        {
            if (!_activeNotes.ContainsKey(noteIndex)) return;

            _activeNotes.Remove(noteIndex);

            // Find next index.
            // Base case.
            if (_activeNotes.Count == 0)
            {
                _lowestIndex = int.MaxValue;
                _highestIndex = int.MinValue;
                return;
            }
            else if (noteIndex <= _lowestIndex)
            {
                // Find next lowest index.
                while (!_activeNotes.ContainsKey(noteIndex))
                {
                    noteIndex++;
                }
                _lowestIndex = noteIndex;
            }
            else if (noteIndex >= _highestIndex)
            {
                // Find next highest index.
                while (!_activeNotes.ContainsKey(noteIndex))
                {
                    noteIndex--;
                }
                _highestIndex = noteIndex;
            }

            NoteRemoved?.Invoke(note, noteIndex);
        }

        void AddNote(Note note, int noteIndex)
        {
            if (_activeNotes.ContainsKey(noteIndex)) return;

            _activeNotes.Add(noteIndex, note);

            // Update min and max.
            if (noteIndex < _lowestIndex) _lowestIndex = noteIndex;
            if (noteIndex > _highestIndex) _highestIndex = noteIndex;

            NoteAdded?.Invoke(note, noteIndex);
        }

        bool NoteVisible(Note note, long lowerTickBound, long upperTickBound)
        {
            return lowerTickBound <= note.EndTime && note.Time <= upperTickBound;
        }

        void RemoveHiddenNotes(long lowerTickBound, long upperTickBound)
        {
            foreach (var kvp in _activeNotes)
            {
                if (!NoteVisible(kvp.Value, lowerTickBound, upperTickBound))
                {
                    RemoveNote(kvp.Value, kvp.Key);
                }
            }
        }

        void AddVisibleNotes(long lowerTickBound, long upperTickBound)
        {
            // Find visible notes based on existing notes.
            if (_activeNotes.Count > 0)
            {
                // Find notes lower than minimum.
                for (int i = _lowestIndex - 1; i >= 0; i--)
                {
                    if (NoteVisible(_notes[i], lowerTickBound, upperTickBound)) AddNote(_notes[i], i);
                    else break;
                }

                // Find notes higher than max.
                for (int i = _highestIndex + 1; i < _activeNotes.Count; i++)
                {
                    if (NoteVisible(_notes[i], lowerTickBound, upperTickBound)) AddNote(_notes[i], i);
                    else break;
                }

                return;
            }

            // When there are no existing notes.
        }

        public void UpdateNotesVisible(long playbackTick, long ticksBeforeStrike, long ticksAfterStrike)
        {
            RemoveHiddenNotes(playbackTick - ticksBeforeStrike, playbackTick + ticksAfterStrike);
            AddVisibleNotes(playbackTick - ticksBeforeStrike, playbackTick + ticksAfterStrike);
        }
        #endregion
    }

    public class RunwayBase
    {
        #region Properties
        IntRange _noteRange;
        NoteManager _noteManager;
        RunwayDisplayInfo _displayInfo;
        NoteLane[] _lanes;
        GameObject _wNotePrefab;
        GameObject _bNotePrefab;
        readonly Transform _laneParent;
        readonly GameObject _lanePrefab;
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
            _noteManager = new(notes);
            _noteManager.NoteAdded += AddNote;
            _noteManager.NoteRemoved += RemoveNote;
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
        void AddNote(Note note, int noteIndex)
        {
            Debug.Log($"Removing note {note.NoteName} @ index {noteIndex}.");
            var laneIndex = note.NoteNumber - _noteRange.Min;
            _lanes[laneIndex].AddNote(note, noteIndex,
                _displayInfo.IsWhiteNoteLane(laneIndex) ? _wNotePrefab : _bNotePrefab);
        }

        void RemoveNote(Note note, int noteIndex)
        {
            Debug.Log($"Removing note {note.NoteName} @ index {noteIndex}.");
            var laneIndex = note.NoteNumber - _noteRange.Min;
            _lanes[laneIndex].RemoveNote(noteIndex);
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
            for (int i = 0; i < _lanes.Length; ++i)
            {
                _lanes[i].Width = _displayInfo.GetLaneWidth(i);
                _lanes[i].SetPosition(_displayInfo.GetLaneXPos(i));
            }
        }

        public void UpdateRunway(long playbackTick)
        {
            _noteManager.UpdateNotesVisible(playbackTick, _displayInfo.TicksVisibleAboveStrike, _displayInfo.TicksVisibleBelowStrike);

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
                foreach (var lane in _lanes)
                {
                    UnityEngine.Object.Destroy(lane.gameObject);
                }
            }

            _lanes = null;
            _noteRange = null;
        }
        #endregion
    }
}
