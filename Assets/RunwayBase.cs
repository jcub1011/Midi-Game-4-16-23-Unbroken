using Melanchall.DryWetMidi.Interaction;
using MIDIGame.Lane;
using System;
using System.Collections.Generic;
using UnityEngine;
using MIDIGame.Ranges;
using System.Linq;

namespace MIDIGame.Runway
{
    public interface IRunwayInterface
    {
        public void UpdatePlaybackTick(long playbackTick);
        public void SetRunwayDimensions(float width, float height);
        public void SetNoteList(ICollection<Note> notes);
        public void ClearRunway();
        public void SetObjectSkin(GameObject skin, RenderableType type);

    }

    public class Runway : IRunwayInterface
    {
        #region Properties
        ILane _lanes;
        
        #endregion

        #region Runway Interface Implementations
        public void ClearRunway()
        {
            throw new NotImplementedException();
        }

        public void SetNoteList(ICollection<Note> notes)
        {
            throw new NotImplementedException();
        }

        public void SetObjectSkin(GameObject skin, RenderableType type)
        {
            throw new NotImplementedException();
        }

        public void SetRunwayDimensions(float width, float height)
        {
            throw new NotImplementedException();
        }

        public void UpdatePlaybackTick(long playbackTick)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

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
                return (long)(TicksVisibleAboveStrike * _strikeBarHeightPercent);
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

            // Debug.Log($"Width in white notes: {_numWhiteNoteLanes}");
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
        void RemoveNote(int noteIndex)
        {
            if (!_activeNotes.ContainsKey(noteIndex)) return;

            var note = _activeNotes[noteIndex];
            _activeNotes.Remove(noteIndex);
            NoteRemoved?.Invoke(note, noteIndex);

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

            // Debug.Log("Hiding note.");
        }

        void AddNote(int noteIndex)
        {
            if (_activeNotes.ContainsKey(noteIndex)) return;

            _activeNotes.Add(noteIndex, _notes[noteIndex]);
            NoteAdded?.Invoke(_notes[noteIndex], noteIndex);

            // Update min and max.
            if (noteIndex < _lowestIndex) _lowestIndex = noteIndex;
            if (noteIndex > _highestIndex) _highestIndex = noteIndex;

            // Debug.Log("Showing note.");
        }

        bool NoteVisible(Note note, long lowerTickBound, long upperTickBound)
        {
            return lowerTickBound <= note.EndTime && note.Time <= upperTickBound;
        }

        void RemoveHiddenNotes(long lowerTickBound, long upperTickBound)
        {
            var keys = _activeNotes.Keys.ToArray();
            foreach (var key in keys)
            {
                if (!NoteVisible(_activeNotes[key], lowerTickBound, upperTickBound))
                {
                    RemoveNote(key);
                }
            }
        }

        int FindClosestNoteIndex(long tickToSearch)
        {
            if (_notes.Count == 0) return -1;

            int lowerBound = 0;
            int upperBound = _notes.Count - 1;
            int middle = 0;

            while (lowerBound < upperBound)
            {
                middle = (lowerBound + upperBound) / 2;
                if (_notes[middle].Time == tickToSearch) return middle;

                if (_notes[middle].Time < tickToSearch)
                {
                    lowerBound = middle + 1;
                } else if (_notes[middle].Time > tickToSearch)
                {
                    upperBound = middle - 1;
                }
            }

            return middle;
        }

        void AddNotesLeftOfIndex(int index, long lowerTickBound, long upperTickBound)
        {
            // Find notes lower than minimum.
            for (; index >= 0; index--)
            {
                if (NoteVisible(_notes[index], lowerTickBound, upperTickBound)) AddNote(index);
                else break;
            }
        }

        void AddNotesRightOfIndex(int index, long lowerTickBound, long upperTickBound)
        {
            // Find notes lower than minimum.
            for (; index < _notes.Count; index++)
            {
                if (NoteVisible(_notes[index], lowerTickBound, upperTickBound)) AddNote(index);
                else break;
            }
        }

        void AddVisibleNotes(long lowerTickBound, long upperTickBound)
        {
            // Find visible notes based on existing notes.
            if (_activeNotes.Count > 0)
            {
                // Find notes lower than minimum.
                AddNotesLeftOfIndex(_lowestIndex - 1, lowerTickBound, upperTickBound);
                // Find notes higher than max.
                AddNotesRightOfIndex(_highestIndex + 1, lowerTickBound, upperTickBound);
                return;
            }

            // When there are no existing notes.
            else
            {
                long boundRadius = (lowerTickBound + upperTickBound) / 2;
                int index = FindClosestNoteIndex(lowerTickBound + boundRadius);
                if (index < 0) return;

                if (NoteVisible(_notes[index], lowerTickBound, upperTickBound))
                {
                    AddNote(index);
                    AddVisibleNotes(lowerTickBound, upperTickBound);
                    return;
                }
                else
                {
                    // Seek in either direction to look for a valid note.
                    for (int seekIndex = index - 1; seekIndex >= 0; seekIndex--)
                    {
                        if (_notes[seekIndex].Time < lowerTickBound) break;
                        if (NoteVisible(_notes[seekIndex], lowerTickBound, upperTickBound))
                        {
                            AddNote(seekIndex);
                            AddVisibleNotes(lowerTickBound, upperTickBound);
                            return;
                        }
                    }
                    for (int seekIndex = index + 1; seekIndex < _notes.Count; seekIndex++)
                    {
                        if (_notes[seekIndex].Time < lowerTickBound) break;
                        if (NoteVisible(_notes[seekIndex], lowerTickBound, upperTickBound))
                        {
                            AddNote(seekIndex);
                            AddVisibleNotes(lowerTickBound, upperTickBound);
                            return;
                        }
                    }
                }
            }
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
            InitLanes(notes, dimensions, strikeBarHeight, ticksToReachStrikeBar,
                whiteNotePrefab, blackNotePrefab, lanesParent, lanePrefab);

            UpdateLaneDimensions();
        }

        void InitLanes(List<Note> notes, float[] dimensions, float strikeBarHeight, long ticksToReachStrikeBar,
            GameObject whiteNotePrefab, GameObject blackNotePrefab, Transform lanesParent, GameObject lanePrefab)
        {
            _noteRange = GetNoteRange(notes);
            // Debug.Log($"Range of notes {_noteRange.Len}.");
            _displayInfo = new(dimensions, strikeBarHeight, ticksToReachStrikeBar, _noteRange);
            _lanes = new NoteLane[_noteRange.Len];

            // Init note prefabs.
            _wNotePrefab = whiteNotePrefab;
            _bNotePrefab = blackNotePrefab;

            List<Note>[] notesByNoteNumber = new List<Note>[_noteRange.Len];
            for (int i = 0; i < notesByNoteNumber.Length; ++i)
            {
                notesByNoteNumber[i] = new();
            }

            foreach (Note note in notes)
            {
                int i = note.NoteNumber - _noteRange.Min;
                notesByNoteNumber[i].Add(note);
            }

            // Initalize lanes.
            for (int i = 0; i < _lanes.Length; i++)
            {
                var newLane = UnityEngine.Object.Instantiate(lanePrefab, lanesParent);
                _lanes[i] = newLane.GetComponent<NoteLane>();
                _lanes[i].Initalize(notesByNoteNumber[i], _displayInfo.TicksVisibleBelowStrike, 
                    _displayInfo.TicksVisibleAboveStrike, _displayInfo.IsWhiteNoteLane(i) ? _wNotePrefab : _bNotePrefab, _displayInfo.GetLaneXPos(i), 
                    _displayInfo.IsWhiteNoteLane(i) ? 1 : 0, _displayInfo.GetLaneWidth(i));
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

            // Debug.Log($"Note range: {min} - {max}");

            var noteRange = new IntRange(min, max);
            IntRange rangeToReturn;


            if (RunwayDisplayInfo.FourtyNineKeyKeyboard.InRange(noteRange))
            {
                rangeToReturn = RunwayDisplayInfo.FourtyNineKeyKeyboard;
                //UnityEngine.GameObject.Find("49KeyKeyboard").GetComponent<SpriteRenderer>().enabled = true;
            }
            else if (RunwayDisplayInfo.SixtyOneKeyKeyboard.InRange(noteRange))
            {
                rangeToReturn = RunwayDisplayInfo.SixtyOneKeyKeyboard;
                //UnityEngine.GameObject.Find("61KeyKeyboard").GetComponent<SpriteRenderer>().enabled = true;
            }
            else if (RunwayDisplayInfo.SeventySixKeyKeyboard.InRange(noteRange))
            {
                rangeToReturn = RunwayDisplayInfo.SeventySixKeyKeyboard;
                //UnityEngine.GameObject.Find("76KeyKeyboard").GetComponent<SpriteRenderer>().enabled = true;
            }
            else if (RunwayDisplayInfo.EightyEightKeyKeyboard.InRange(noteRange))
            {
                rangeToReturn = RunwayDisplayInfo.EightyEightKeyKeyboard;
                //UnityEngine.GameObject.Find("88KeyKeyboard").GetComponent<SpriteRenderer>().enabled = true;
            }
            else
            {
                rangeToReturn = RunwayDisplayInfo.MaxKeyKeyboard;
                //UnityEngine.GameObject.Find("128KeyKeyboard").GetComponent<SpriteRenderer>().enabled = true;
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
