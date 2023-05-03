﻿using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Tools;
using System.Timers;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;

public class CustomPlaybackEngine
{
    #region Properties
    private float _currentTick = 0f;
    private static System.Timers.Timer _intervalTimer = null;
    private float _tickIncrementFactor = 1f;
    private float _tickLength = 1f;
    private float _forgivness = 400f;
    #endregion

    #region GetterSetterMethods
    public bool IsPlaying { get; private set; } = false;
    public float CurrentPlaybackTimeMs
    {
        get { return _currentTick * _tickLength; }
    }
    public float PlaybackSpeed { get; set; } = 1f;
    #endregion

    #region Constructor
    CustomPlaybackEngine(string songFilePath, OutputDevice outputDevice, float playbackSpeed = 1f, 
        float forgiveness = 400f, int qNoteLeadup = 4, bool forScrubbing = false)
    {
        if (forScrubbing) throw new System.NotImplementedException("Scrubbing method is not implemented yet.");

        var midiFile = MidiFile.Read(songFilePath);

        InitProperties(midiFile.GetTempoMap(), qNoteLeadup, playbackSpeed);
    }
    #endregion

    #region Methods
    private void InitProperties(TempoMap tempoMap, int qNoteLeadup, float playbackSpeed)
    {
        const float MAX_INTERVAL_PRECISION = 16f;
        float msInterval = 0f;
        // Get tick length.
        _tickLength = (float)TimeConverter.ConvertTo<MetricTimeSpan>(1, tempoMap).TotalMilliseconds;

        // Get tick increment factor and interval.
        if (_tickLength < MAX_INTERVAL_PRECISION)
        {
            _tickIncrementFactor = MAX_INTERVAL_PRECISION / _tickLength;
            msInterval = MAX_INTERVAL_PRECISION;
            Debug.Log("Time per tick smaller than timer precision. " +
                $"Interpolating ticks by factor {_tickIncrementFactor}.");
        } else
        {
            _tickIncrementFactor = 1f;
            msInterval = _tickLength;
            Debug.Log($"Tick interval {_tickLength}ms.");
        }
        
        // Get leadup time.
        var qNotes = new MusicalTimeSpan(4) * qNoteLeadup;
        var tickPerQnote = TimeConverter.ConvertFrom(qNotes, tempoMap);

        // Update current ticks.
        _currentTick = -tickPerQnote;

        // Set playback speed.
        PlaybackSpeed = playbackSpeed;

        // Init interval timer.
        _intervalTimer = new System.Timers.Timer
        {
            Interval = msInterval,
            AutoReset = true,
            Enabled = false
        };
        _intervalTimer.Elapsed += IncrementTicks;
    }

    private void IncrementTicks(System.Object source, ElapsedEventArgs evt)
    {
        _currentTick += _tickIncrementFactor;
    }

    private void Stop()
    {
        Debug.Log("None");
    }
    #endregion
}