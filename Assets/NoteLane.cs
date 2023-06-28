using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace MIDIGame.Lane
{
    public delegate void NoteMissEvt(Note note);

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

    public class NoteLane : MonoBehaviour
    {
        #region Properies
        Dictionary<int, NoteObject> _notes;
        float _zPos;
        float _xPos;
        public NoteMissEvt OnNoteMissed;
        #endregion

        #region Utility Methods
        /// <summary>
        /// Gets the accuracy of the note play.
        /// </summary>
        /// <param name="differenceTicks">Difference between the actual note tick and the played note tick.</param>
        /// <param name="forgiveness">Range of forgiveness in ticks.</param>
        /// <returns></returns>
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
            if (_notes == null || _notes.Count == 0)
            {
                Debug.Log($"Lane has no notes.");
                return;
            }

            // Update positions for all managed notes.
            foreach (var noteObject in _notes.Values)
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

        /// <summary>
        /// Returns percent accuracy.
        /// </summary>
        /// <param name="time">Current playback time in ticks.</param>
        /// <param name="forgiveness">Forgivness range in ticks.</param>
        /// <param name="NoteOnEvent">True if note on event, otherwise note off event.</param>
        /// <returns>Accuracy</returns>
        public float NoteEventAccuracy(long time, long forgiveness, bool NoteOnEvent)
        {
            //float eventTimeToCompareWith = 2f * GameData.Forgiveness;
            long evtTime = -10;

            print($"Current time: {time}");
            if (_notes.Count == 0)
            {
                Debug.Log($"No notes to check accuracy against.");
                return 0f;
            }

            // Find minimum index.
            int lowestIndex = int.MaxValue;
            foreach (int index in _notes.Keys)
            {
                if (index < lowestIndex) lowestIndex = index;
            }

            // Find next playable note time.
            for (int index = lowestIndex; ; index++)
            {

            }
            foreach (var note in _notes)
            {
                var noteTime = NoteOnEvent ? note.Data.Time : note.Data.EndTime;

                if (InRange(time, noteTime - forgiveness, noteTime + forgiveness) && !note.Played)
                {
                    note.Played = true;
                    evtTime = noteTime;
                    break;
                }

            }

            // Only when there is no note within forgiveness range.
            if (evtTime < 0)
            {
                print("Total miss.");
                return 0;
            }

            var msDist = evtTime - time;
            var accuracy = CalculateAccuracy(msDist, forgiveness);
            print($"Note event accuracy: {accuracy}%\n" +
                "Note time position: {eventTimeToCompareWith}ms\n" +
                $"Played note time distance: {msDist}ms");

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
        /// Adds note to end of list.
        /// </summary>
        /// <param name="noteData">Data for note.</param>
        /// <param name="notePrefab">Prefab to use for note.</param>
        public void AddNote(Note noteData, int noteIndex, GameObject notePrefab)
        {
            _notes ??= new();
            _notes.Add(noteIndex, MakeNoteObject(noteData, notePrefab));
        }

        /// <summary>
        /// Removes the last note.
        /// </summary>
        public void RemoveNote(int noteIndex)
        {
            _notes ??= new();
            if (_notes.Count == 0) return;
            var note = _notes[noteIndex];
            _notes.Remove(noteIndex);
            Destroy(note.Note);
        }

        public void ResetNotesPlayed()
        {
            _notes ??= new();
            foreach (var obj in _notes.Values)
            {
                obj.Played = false;
            }
        }
        #endregion
    }
}