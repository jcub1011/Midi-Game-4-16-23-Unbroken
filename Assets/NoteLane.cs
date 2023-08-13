using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MIDIGame.Lane
{
    public delegate void NoteMissEvt(Note note);

    public enum RenderableType
    {
        Note = 0,
        Bar
    }

    public class NoteObject
    {
        public GameObject Note { get; private set; }
        public Note Data { get; private set; }
        public bool Played;

        /// <summary>
        /// Bundles the supplied data and game object.
        /// </summary>
        /// <param name="notePrefab">The game object to attach the data to.</param>
        /// <param name="data">The data to attach the game object to.</param>
        public NoteObject(GameObject notePrefab, Note data)
        {
            Note = notePrefab;
            Data = data;
            Played = false;
        }

        /// <summary>
        /// Updates the position and scale of the GameObject.
        /// </summary>
        /// <param name="playbackTick">Current tick of playback.</param>
        /// <param name="unitsPerTick">Units per tick.</param>
        /// <param name="runwayTopY">Y position of the top of the runway.</param>
        public void UpdateGameObject(long playbackTick, float unitsPerTick, float runwayTopY)
        {
            Note.transform.localScale = new Vector3(1, Data.Length * unitsPerTick, 1);
            Note.transform.localPosition = new Vector3(1, runwayTopY + Note.transform.localScale.y / 2f - playbackTick * unitsPerTick, 0);
        }
    }

    public interface IPlaybackData
    {
        public long PlaybackStartPosition { get; }
        public long PlaybackEndPosition { get; }
        public long LengthTicks { get; }
        public int Index { get; }
        public int NoteNum { get; }
        public RenderableType ID { get; }
    }

    public interface IRenderableObject : IDisposable
    {
        public GameObject GameObject { get; }
        public bool Played { get; }

        public IPlaybackData PlaybackData { get; }

        public void UpdateGameObject(long futureVisibleTick, float laneTopYPos, float unitsPerTick, float zPos);
        public void UpdateGameObject(float yPositionOffset);
    }

    public interface ILane : IDisposable
    {
        public void Initalize(ICollection<Note> notes, long ticksVisibleInFuture, long ticksVisibleInPast);
        public void UpdateLane(long playbackTick, float unitsPerTick, float runwayTopY);
        public void UpdateVisibleRange(long ticksVisibleInFuture, long ticksVisibleInPast);
        public GameObject ObjectsSkin { get; set; }
    }

    internal class NoteData : IPlaybackData
    {
        private Note _data;
        private int _index;
        private RenderableType _id;

        #region Getters
        public long PlaybackStartPosition
        {
            get { return _data.Time; }
        }

        public long PlaybackEndPosition
        {
            get { return _data.EndTime; }
        }

        public long LengthTicks
        {
            get { return _data.Length; }
        }

        public int Index
        {
            get { return _index; }
        }

        public int NoteNum
        {
            get { return _data.NoteNumber; }
        }

        public RenderableType ID
        {
            get { return _id; }
        }
        #endregion

        #region Constructors
        public NoteData(Note data, int index)
        {
            _data = data;
            _index = index;
            _id = RenderableType.Note;
        }

        public NoteData(RenderableType type, int index)
        {
            _data = null;
            _index = index;
            _id = type;
        }
        #endregion
    }

    internal class NoteBlock : IRenderableObject
    {
        private bool _disposedValue;
        private GameObject _gameObject;
        private bool _played;
        private IPlaybackData _data;

        public GameObject GameObject
        {
            get { return _gameObject; }
        }
        public bool Played
        {
            get { return _played; }
        }

        public IPlaybackData PlaybackData
        {
            get { return _data; }
        }

        public NoteBlock(GameObject skin, IPlaybackData data)
        {
            _played = false;
            _data = data;
            _gameObject = skin;
            _disposedValue = false;
        }

        /// <summary>
        /// Updates the position of the game object.
        /// </summary>
        /// <param name="futureVisibleTick">Playback tick + ticks visible in future.</param>
        /// <param name="laneTopYPos"></param>
        /// <param name="unitsPerTick"></param>
        /// <param name="zPos">Z position of object.</param>
        public void UpdateGameObject(long futureVisibleTick, float laneTopYPos, float unitsPerTick, float zPos)
        {
            float newYPos = laneTopYPos + _gameObject.transform.localScale.y / 2 + (_data.PlaybackStartPosition - futureVisibleTick) * unitsPerTick;
            _gameObject.transform.localPosition = new Vector3(0, newYPos, zPos);
        }

        public void UpdateGameObject(float yPositionOffset)
        {
            _gameObject.transform.localPosition += new Vector3(0f, yPositionOffset, 0f);
        }

        #region IDispose Implementation and Finalizer
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _played = false;
                    _data = null;
                }

                if (_gameObject != null) UnityEngine.Object.Destroy(_gameObject);
                _disposedValue = true;
            }
        }

        ~NoteBlock()
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
        #endregion
    }

    public class NoteLane : MonoBehaviour, ILane
    {
        #region Properies
        List<NoteData> _notes;
        LinkedList<IRenderableObject> _renderableNotes;
        float _zPos;
        float _xPos;
        public NoteMissEvt OnNoteMissed;
        bool _disposed;
        long _ticksBeforePlaybackTick;
        long _ticksAfterPlaybackTick;
        public bool Initialized { get; private set; }
        public GameObject ObjectsSkin { get; set; }
        #endregion

        #region ILane Implementations
        public void Initalize(ICollection<Note> notes, long ticksBeforePlaybackTick, long ticksAfterPlaybackTick)
        {
            _notes = new();
            int i = 0;
            foreach (Note note in notes)
            {
                _notes.Add(new NoteData(note, i++));
            }

            UpdateVisibleRange(ticksBeforePlaybackTick, ticksAfterPlaybackTick);
            Initialized = true;
        }

        public void UpdateLane(long playbackTick, float unitsPerTick, float topYPos)
        {
            if (!Initialized) Debug.Log("Lane not initialized.");
        }

        public void UpdateVisibleRange(long ticksBeforePlaybackTick, long ticksAfterPlaybackTick)
        {
            _ticksBeforePlaybackTick = ticksBeforePlaybackTick;
            _ticksAfterPlaybackTick = ticksAfterPlaybackTick;
        }
        #endregion

        #region Utility Methods
        void CrawlBoundsInwards(long pastTickBound, long futureTickBound)
        {
            // Remove notes past playback range.
            for(var node = _renderableNotes.First; node != null; node = node.Next)
            {
                if (node.Value.PlaybackData.PlaybackEndPosition < pastTickBound)
                {
                    _renderableNotes.RemoveFirst();
                }
            }

            // Remove notes before playback range.
            for(var node = _renderableNotes.Last; node != null; node = node.Previous)
            {
                if (node.Value.PlaybackData.PlaybackStartPosition > futureTickBound)
                {
                    _renderableNotes.RemoveLast();
                }
            }
        }

        bool WithinVisibleRange(NoteData value, long lowerTickBound, long upperTickBound)
        {
            return value.PlaybackEndPosition >= lowerTickBound && value.PlaybackStartPosition <= upperTickBound;
        }

        /// <summary>
        /// Finds index of note within bounds.
        /// </summary>
        /// <param name="pastTickBound">Lower bound.</param>
        /// <param name="futureTickBound">Upper bound.</param>
        /// <returns>-1 if failed.</returns>
        int FindIndexWithinRange(long pastTickBound, long futureTickBound)
        {
            int lowerBound = 0;
            int upperBound = _notes.Count - 1;

            while (lowerBound <= upperBound)
            {
                NoteData middle = _notes[(lowerBound + upperBound) / 2];

                if (middle.PlaybackEndPosition < pastTickBound) lowerBound = middle.Index + 1;
                else if (middle.PlaybackStartPosition > futureTickBound) upperBound = middle.Index - 1;
                else return middle.Index;
            }

            return -1;
        }

        void CrawlBoundsOutwards(long pastTickBound, long futureTickBound)
        {
            if (_renderableNotes.Count == 0)
            {
                Debug.Log("Empty lane.");
                
                int result = FindIndexWithinRange(pastTickBound, futureTickBound);
                if (result == -1) { return; }

                _renderableNotes.AddFirst(CreateRenderableObject(_notes[result]));
            }

            for (var i = _renderableNotes.First.Value.PlaybackData.Index - 1; i >= 0; i--)
            {
                if (_notes[i].PlaybackEndPosition >= pastTickBound)
                {
                    _renderableNotes.AddFirst(CreateRenderableObject(_notes[i]));
                }
                else break;
            }

            for (var i = _renderableNotes.Last.Value.PlaybackData.Index + 1; i < _notes.Count; i++)
            {
                if (_notes[i].PlaybackStartPosition <= futureTickBound)
                {
                    _renderableNotes.AddLast(CreateRenderableObject(_notes[i]));
                }
                else break;
            }
        }

        IRenderableObject CreateRenderableObject(IPlaybackData playbackData)
        {
            return new NoteBlock(ObjectsSkin, playbackData); 
        }
        #endregion

        #region IDisposable Implementation and Finalizer
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            
            if (disposing)
            {
                _notes = null;
                OnNoteMissed = null;
            }

            foreach (var item in _renderableNotes)
            {
                item.Dispose();
            }
            _renderableNotes = null;

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~NoteLane() 
        {
            Dispose(disposing: false);
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Gets the accuracy of the note play.
        /// </summary>
        /// <param name="differenceTicks">Difference between the current tick and the note event tick.</param>
        /// <param name="forgiveness">Range of forgiveness in ticks.</param>
        /// <returns>Accuracy where 100 is 100%.</returns>
        float CalculateAccuracy(long differenceTicks, long forgiveness)
        {
            if (differenceTicks < 0) differenceTicks *= -1; // Make it positive.

            if (differenceTicks == 0)
            {
                return 100f;
            }

            if (differenceTicks > forgiveness)
            {
                return 0f;
            }

            float accuracy = 100f - (float)differenceTicks / forgiveness * 100f;

            return accuracy;
        }

        /// <summary>
        /// Checks if a number is between two other numbers.
        /// </summary>
        /// <param name="number">Number to check.</param>
        /// <param name="rangeMin">Range min.</param>
        /// <param name="rangeMax">Range max.</param>
        /// <returns></returns>
        bool InRange(long number, long rangeMin, long rangeMax)
        {
            return rangeMin <= number && number <= rangeMax;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns percent accuracy.
        /// </summary>
        /// <param name="time">Current playback time in ticks.</param>
        /// <param name="forgiveness">Forgivness range in ticks.</param>
        /// <param name="noteOnEvent">True if note on event, otherwise note off event.</param>
        /// <returns>Accuracy</returns>
        public float NoteEventAccuracy(long time, long forgiveness, bool noteOnEvent)
        {
            print($"Current time: {time}");
            if (_renderedNotes.Count == 0)
            {
                Debug.Log($"No notes to check accuracy against.");
                return 0f;
            }

            // Find lowest tick unplayed note.
            Note soonestNote = noteOnEvent ? 
                FindNextUnplayedOnEvent(time - forgiveness, time + forgiveness) : 
                FindNextUnplayedOffEvent(time - forgiveness, time + forgiveness);

            if (soonestNote == null)
            {
                print("Total miss.");
                return 0f;
            }

            var evtTime = noteOnEvent ? soonestNote.Time : soonestNote.EndTime;
            var accuracy = CalculateAccuracy(time - evtTime, forgiveness);
            print($"Note event accuracy: {accuracy}%\n" +
                $"Note time position: {evtTime}ms\n" +
                $"Played note time distance: {time - evtTime}ms");

            return accuracy;
        }

        public void SetPosition(float xPos, float zPos)
        {
            _xPos = xPos;
            _zPos = zPos;
            transform.localPosition = new Vector3(_xPos, 0, _zPos);
        }

        public void SetPosition(float xPos)
        {
            SetPosition(xPos, _zPos);
        }
        #endregion

        #region Getter Setter Methods
        private float _width;
        public float Width
        {
            get { return _width; }
            set
            {
                _width = value;
                transform.localScale = new Vector3(_width, 1f, 1f);
            }
        }
        #endregion

        #region Notes Get Set Remove Functions
        NoteObject MakeNoteObject(Note noteData, GameObject prefab)
        {
            var obj = new NoteObject(UnityEngine.Object.Instantiate(prefab, transform), noteData);
            obj.Note.GetComponent<SpriteRenderer>().enabled = true;
            return obj;
        }

        /// <summary>
        /// Adds the note with the given id.
        /// </summary>
        /// <param name="noteData">Data for note.</param>
        /// <param name="notePrefab">Prefab to use for note.</param>
        public void AddNote(Note noteData, int noteIndex, GameObject notePrefab)
        {
            _renderedNotes ??= new();
            if (_renderedNotes.ContainsKey(noteIndex)) return;
            _renderedNotes.Add(noteIndex, MakeNoteObject(noteData, notePrefab));
        }

        /// <summary>
        /// Removes the note with the given id.
        /// </summary>
        public void RemoveNote(int noteIndex)
        {
            _renderedNotes ??= new();
            if (!_renderedNotes.ContainsKey(noteIndex)) return;
            if (_renderedNotes.Count == 0) return;
            var note = _renderedNotes[noteIndex];
            _renderedNotes.Remove(noteIndex);
            Destroy(note.Note);
        }

        public void ResetNotesPlayed()
        {
            _renderedNotes ??= new();
            foreach (var obj in _renderedNotes.Values)
            {
                obj.Played = false;
            }
        }
        #endregion
    }
}