using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Tools;
using System;
using System.Diagnostics.Tracing;
using UnityEditor;
using UnityEngine;

public class PlaybackClock
{
    #region Properties
    private const float MAX_PRECISION = 16f;
    private readonly float _tickLength = 1f;
    private float _tickIncrementFactor = 1f;
    private static System.Timers.Timer _intervalTimer = null;
    #endregion

    #region GetterSetters
    public float CurrentTick { get; private set; } = 0f;
    public float ClockSpeedFactor { get; private set; } = 1f;
    #endregion

    #region Methods
    /// <summary>
    /// Returns the current time in ms.
    /// </summary>
    /// <returns>Miliseconds.</returns>
    public float CurrentTimeMs()
    {
        return _tickLength * CurrentTick;
    }

    /// <summary>
    /// Sets the speed mulitplier of the timer tick.
    /// </summary>
    /// <param name="tickSpeedMultiplicationFactor">Multiplication of tick speed.</param>
    public void SetIncrementMultiplier(float tickSpeedMultiplicationFactor)
    {
        if (_intervalTimer == null) return;
        ClockSpeedFactor = tickSpeedMultiplicationFactor;

        var newInterval = _tickLength / ClockSpeedFactor;

        // Update tick increment factor.
        if (newInterval < MAX_PRECISION)
        {
            newInterval = MAX_PRECISION;
            _tickIncrementFactor = MAX_PRECISION / _tickLength;
        }
        else
        {
            _tickIncrementFactor = 1f;
        }

        // Update interval timer.
        _intervalTimer.Interval = newInterval;
    }

    private void IncrementTicks(System.Object source, System.Timers.ElapsedEventArgs evt)
    {
        CurrentTick += _tickIncrementFactor;
    }

    public void Start()
    {
        _intervalTimer.Start();
        _intervalTimer.AutoReset = true;
        _intervalTimer.Enabled = true;
    }

    public void Stop()
    {
        _intervalTimer.Stop();
        _intervalTimer.AutoReset = false;
        _intervalTimer.Enabled = false;
    }

    public void Dispose()
    {
        Stop();
        _intervalTimer.Dispose();
        _intervalTimer = null;
    }
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new playback clock.
    /// </summary>
    /// <param name="msInterval"></param>
    /// <exception cref="System.ArgumentOutOfRangeException"></exception>
    public PlaybackClock (float msInterval)
    {
        if (!(msInterval > 0f))
        {
            throw new System.ArgumentOutOfRangeException("msInterval must be greater than 0.");
        }

        _intervalTimer = new System.Timers.Timer();
        _tickLength = msInterval;
        SetIncrementMultiplier(1f);
        _intervalTimer.Elapsed += IncrementTicks; // Set callback.
    }
    #endregion
}

public class CustomPlaybackEngine
{
    #region Properties
    private static PlaybackClock _ticker = null;
    private float _forgiveness = 400f;
    #endregion

    #region GetterSetterMethods
    public bool IsPlaying { get; private set; } = false;
    public float PlaybackSpeed
    {
        get 
        {
            if (_ticker == null) return 0f;
            return _ticker.ClockSpeedFactor;
        }
        set
        {
            if (_ticker == null) return;
            _ticker.SetIncrementMultiplier(value);
        }
    }
    #endregion

    #region Constructor
    CustomPlaybackEngine(string songFilePath, OutputDevice outputDevice, float playbackSpeed = 1f, 
        float forgiveness = 400f, int qNoteLeadup = 4, bool forScrubbing = false)
    {
        if (forScrubbing) throw new System.NotImplementedException("Scrubbing method is not implemented yet.");

        // Read midi file.
        var midiFile = MidiFile.Read(songFilePath);

        var msPerTick = (float)TimeConverter.ConvertTo<MetricTimeSpan>(1, midiFile.GetTempoMap()).TotalMilliseconds;
        _ticker = new PlaybackClock(msPerTick);
    }
    #endregion

    #region Methods
    #endregion
}