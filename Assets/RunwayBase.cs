using Melanchall.DryWetMidi.Interaction;
using MIDIGame.Lane;
using System;
using System.Collections.Generic;
using UnityEngine;
using MIDIGame.Ranges;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;

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

    public class RunwayDisplayInfo
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

    public class NotePool : IDisposable
    {
        GameObject _notePrefab;
        Stack<GameObject> _notes;
        Transform _poolStorageParent;
        private bool disposedValue;
        readonly int _capacity;

        public NotePool(GameObject notePrefab, Transform poolStorageParent, int capacity)
        {
            _notePrefab = notePrefab;
            _poolStorageParent = poolStorageParent;
            _capacity = capacity;

            while (_notes.Count < _capacity)
            {
                _notes.Push(UnityEngine.Object.Instantiate(_notePrefab, _poolStorageParent));
            }
        }

        public GameObject GetNotePrefab()
        {
            if (_notes.Count > 0) return _notes.Pop();
            return UnityEngine.Object.Instantiate( _notePrefab, _poolStorageParent);
        }

        public void ReturnNotePrefab(GameObject notePrefab)
        {
            if (_notes.Count < _capacity) _notes.Push(notePrefab);
            else UnityEngine.Object.Destroy(notePrefab);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _poolStorageParent = null;
                    _notePrefab = null;
                }

                foreach (var prefab in _notes)
                {
                    UnityEngine.Object.Destroy(prefab);
                }
                _notes.Clear();
                _notes = null;

                disposedValue = true;
            }
        }

        ~NotePool()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class RunwayBase
    {
        #region Properties
        IntRange _noteRange;
        RunwayDisplayInfo _displayInfo;
        NoteLane[] _lanes;
        NotePool _notePool;
        readonly Transform _laneParent;
        readonly GameObject _lanePrefab;
        readonly Transform _poolStorage;
        #endregion

        #region Constructors
        public RunwayBase(List<Note> notes, float[] dimensions, float strikeBarHeight,
            long ticksToReachStrikeBar, Transform lanesParent, GameObject lanePrefab,
            GameObject notePrefab, Transform poolStorage)
        {
            _poolStorage = poolStorage;

            InitLanes(notes, dimensions, strikeBarHeight, ticksToReachStrikeBar,
                notePrefab, lanesParent, lanePrefab);

            UpdateLaneDimensions();
        }

        void InitLanes(List<Note> notes, float[] dimensions, float strikeBarHeight, long ticksToReachStrikeBar,
            GameObject notePrefab, Transform lanesParent, GameObject lanePrefab)
        {
            _noteRange = AnalyzeNotes(in notes, out int notePoolSize, strikeBarHeight, ticksToReachStrikeBar);
            _notePool?.Dispose();
            _notePool = null;

            // Debug.Log($"Range of notes {_noteRange.Len}.");
            _displayInfo = new(dimensions, strikeBarHeight, ticksToReachStrikeBar, _noteRange);
            _lanes = new NoteLane[_noteRange.Len];

            // Init note prefabs.
            _notePool = new(notePrefab, _poolStorage, notePoolSize);

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
                _lanes[i].Initalize(notesByNoteNumber[i], in _displayInfo, ref _notePool, i);
            }
        }
        #endregion

        #region Methods
        IntRange AnalyzeNotes(in List<Note> notes, out int notePoolSize, float strikeBarHeight, long ticksToReachStrikeBar)
        {
            short min = short.MaxValue;
            short max = short.MinValue;
            long visibleWindowBeforeBar = ticksToReachStrikeBar;
            long visibleWindowAfterBar = (long)(ticksToReachStrikeBar * strikeBarHeight);

            /* Idea is to find standard deviation of the maximum notes visible within the sliding window
             * and set the respective pool sizes accordingly.
             * Welford's Online Algorithm: https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance
             */

            // Thanks to Joni from the mindful programmer for providing the pseudo code.
            // https://jonisalonen.com/2013/deriving-welfords-method-for-computing-variance/
            double mean = 0;
            double oldMean;
            double sumSquares = 0;
            double variance;

            int startIndex = 0;
            int endIndex = 0;
            long currentNoteCount;

            for (int i = 0; i < notes.Count; i++)
            {
                // Analyze min and max.
                if (notes[i].NoteNumber < min) min = notes[i].NoteNumber;
                if (notes[i].NoteNumber > max) max = notes[i].NoteNumber;

                // Slide start index up until it reaches visible range.
                while (startIndex < i && notes[startIndex].EndTime < notes[i].Time - visibleWindowAfterBar) startIndex++;
                // Slide end index up just before leaving visible range.
                while (endIndex < (notes.Count - 1) && notes[endIndex + 1].Time < notes[i].Time + visibleWindowBeforeBar) endIndex++;
                currentNoteCount = endIndex - startIndex + 1;


                oldMean = mean;
                mean += (currentNoteCount - mean) / (i + 1);
                sumSquares += (currentNoteCount - mean) * (currentNoteCount - oldMean);
            }

            // Debug.Log($"Note range: {min} - {max}");
            variance = sumSquares / (notes.Count - 1); // Sample variance.
            notePoolSize = (int)Math.Ceiling(mean + Math.Pow(variance, 0.5) * 2.0);
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

        private double CalculateVariance(in List<int> values)
        {
            // Thanks to Joni from the mindful programmer for providing the pseudo code.
            // https://jonisalonen.com/2013/deriving-welfords-method-for-computing-variance/
            double mean = 0;
            double oldMean;
            double variance = 0;

            for (int i = 0; i < values.Count; i++)
            {
                oldMean = mean;
                mean += (values[i] - mean) / (i + 1);
                variance += (values[i] - mean) * (values[i] - oldMean);
            }

            return variance / (values.Count - 1);
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
                _notePool.GetNotePrefab(), _laneParent, _lanePrefab);
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
