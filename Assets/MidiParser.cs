using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class TrackInstrumentWrapper
{
    public List<Note> notes = new List<Note>();
    public string Instrument = "";
}


public class MidiParser
{
    #region Properties
    private MidiFile _midiFile;
    #endregion

    #region Constructors
    public MidiParser (string midiFile)
    {
        _midiFile = MidiFile.Read(midiFile);

        var tracks = GetTracksList();

        Debug.Log($"Number of tracks: {tracks.Count}");
    }
    #endregion

    #region Methods
    private List<List<Note>> GetTracksList()
    {
        var tracks = new List<List<Note>>();

        foreach (var chunk in _midiFile.GetTrackChunks())
        {
            var track = new List<Note>();

            foreach (var note in chunk.GetNotes())
            {
                track.Add(note);
            }
        }

        return tracks;
    }
    #endregion
}