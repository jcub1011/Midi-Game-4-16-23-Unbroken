using Melanchall.DryWetMidi.Interaction;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace MIDIGame.Lane
{
    public delegate void NoteMissEvt(Note note);

    public enum RenderableType
    {
        Bar = 130
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
        public int ID { get; } // Values 0-127 denote midi note numbers. See the RenderableType enum for definition of values 130+
        public int Index { get; set; }
    }

    public interface IRenderableObject : IDisposable, IPlaybackData
    {
        public GameObject GameObject { get; set; }
        public bool Played { get; set; }

        public void UpdateGameObject(long playbackTick, float unitsPerTick, float runwayTopY);
        public void UpdateGameObject(float positionOffset);
    }

    public interface ILane : IDisposable
    {
        public void SetNoteList(ICollection<IPlaybackData> notes);
        public void UpdateLane(long playbackTick, float unitsPerTick, float runwayTopY);
        public void SetVisibleRange(long ticksBeforePlaybackTick, long ticksAfterPlaybackTick);
    }

    public class NoteLane : MonoBehaviour, ILane
    {
        #region Properies
        List<IPlaybackData> _notes;
        LinkedList<IRenderableObject> _renderableNotes;
        float _zPos;
        float _xPos;
        public NoteMissEvt OnNoteMissed;
        bool _disposed;
        #endregion

        #region ILane Implementations
        public void SetNoteList(ICollection<IPlaybackData> notes)
        {
            _notes = notes.ToList();
            for (int i = 0; i < notes.Count; i++)
            {
                _notes[i].Index = i;
            }
        }

        public void UpdateLane(long playbackTick, float unitsPerTick, float topYPos)
        {
            
        }
        #endregion

        #region Utility Methods
        void CrawlBoundsInwards(long lowerTickBound, long upperTickBound)
        {
            // Remove notes past playback range.
            for(var node = _renderableNotes.First; node != null; node = node.Next)
            {
                if (node.Value.PlaybackEndPosition < lowerTickBound)
                {
                    _renderableNotes.RemoveFirst();
                }
            }

            // Remove notes before playback range.
            for(var node = _renderableNotes.Last; node != null; node = node.Previous)
            {
                if (node.Value.PlaybackStartPosition > upperTickBound)
                {
                    _renderableNotes.RemoveLast();
                }
            }
        }

        void CrawlBoundsOutwards(long lowerTickBound, long upperTickBound)
        {
            if (_renderableNotes.Count == 0)
            {
                // Find new playback position.
            }

            for (var i = _renderableNotes.First.Value.Index; i >= 0; i--)
            {
                if (_notes[i].PlaybackEndPosition >= lowerTickBound)
                {
                    _renderableNotes.AddFirst(_notes[i]);
                }
            }
        }

        IRenderableObject CreateRenderableObject(IPlaybackData playbackData)
        {
            return new IRenderableObject();
        }
        #endregion

        #region IDisposable Implementation
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed) return;
            
            if (disposing)
            {
                foreach (var item in _renderableNotes)
                {
                    item.Dispose();
                }
                _renderableNotes = null;
            }

            _notes = null;
            OnNoteMissed = null;
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
        /// Updates the scale and position of all the notes in the lane.
        /// </summary>
        /// <param name="playbackTick"></param>
        /// <param name="unitsPerTick"></param>
        /// <param name="topYPos"></param>
        public void UpdateLane(long playbackTick, float unitsPerTick, float topYPos)
        {
            if (_renderedNotes == null || _renderedNotes.Count == 0)
            {
                Debug.Log($"Lane has no notes.");
                return;
            }

            // Update positions for all managed notes.
            foreach (var noteObject in _renderedNotes.Values)
            {
                // Get new scale.
                var newScale = new Vector3
                {
                    x = 1,
                    y = noteObject.Data.Length * unitsPerTick,
                    z = 1
                };

                // Get new position.
                var newPosition = new Vector3
                {
                    x = 0,
                    y = topYPos - (unitsPerTick * (playbackTick - noteObject.Data.Time)) + newScale.y / 2,
                    z = 0
                };

                // Update scale and position.
                noteObject.Note.transform.localPosition = newPosition;
                noteObject.Note.transform.localScale = newScale;
            }
        }

        Note FindNextUnplayedOnEvent(long rangeMin, long rangeMax)
        {
            Note soonestNote = null;

            foreach (NoteObject note in _renderedNotes.Values)
            {
                // Check if sooner than current soonest note.
                if (soonestNote == null || note.Data.Time < soonestNote.Time)
                {
                    // Only keep track of unplayed notes within forgiveness threshold.
                    if (!note.Played && InRange(note.Data.Time, rangeMin, rangeMax))
                    {
                        soonestNote = note.Data;
                    }
                }
            }

            return soonestNote;
        }

        Note FindNextUnplayedOffEvent(long rangeMin, long rangeMax)
        {
            Note soonestNote = null;

            foreach (NoteObject note in _renderedNotes.Values)
            {
                // Check if sooner than current soonest note.
                if (soonestNote == null || note.Data.EndTime < soonestNote.EndTime)
                {
                    // Only keep track of unplayed notes within forgiveness threshold.
                    if (!note.Played && InRange(note.Data.EndTime, rangeMin, rangeMax))
                    {
                        soonestNote = note.Data;
                    }
                }
            }

            return soonestNote;
        }

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