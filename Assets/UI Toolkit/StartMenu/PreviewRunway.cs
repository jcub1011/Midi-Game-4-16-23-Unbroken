using System.Collections.Generic;
using UnityEngine;
using Melanchall.DryWetMidi.Interaction;

namespace MIDIGame.Runway
{
    public class PreviewRunway : MonoBehaviour
    {
        #region Properties
        RunwayBase _runway;
        public GameObject LanePrefab;
        public GameObject NotePrefab;
        public GameObject NotePoolStorage;
        #endregion

        #region Methods
        public void Initalize(List<Note> notes, float strikeBarHeight, long tickToReachStrikeBar,
            long startTick)
        {
            if (notes.Count == 0) throw new System.Exception("There are no notes to display.");
            Debug.Log($"Preview has {notes.Count} notes.");

            float height = Camera.main.orthographicSize * 2f;
            float width = height * Camera.main.aspect;
            float[] dimensions = new float[2] { width, height };

            Unload();
            _runway = new(notes, dimensions, strikeBarHeight,
                tickToReachStrikeBar, transform, LanePrefab, NotePrefab, NotePoolStorage.transform);
        }

        public void Unload()
        {
            print("Unloading preview runway.");
            _runway?.Clear();
            _runway = null;
        }

        public void UpdateTime(long playbackTick)
        {
            _runway?.UpdateRunway(playbackTick);
        }
        #endregion
    }
}