using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Tools;
using System;
using System.Diagnostics.Tracing;
using UnityEditor;
using UnityEngine;

public struct NoteEvtData
{
    public short number;
    public float onTime;
    public float offTime;
    public float len;
}

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
    /// <param name="msInterval">Ms per tick.</param>
    /// <param name="initalTime">Inital time of clock in ms.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">msInterval must be greater than 0.</exception>
    public PlaybackClock (float msInterval, float initalTime = 0f)
    {
        if (!(msInterval > 0f))
        {
            throw new System.ArgumentOutOfRangeException("msInterval must be greater than 0.");
        }

        _intervalTimer = new System.Timers.Timer();
        _tickLength = msInterval;
        SetIncrementMultiplier(1f);
        _intervalTimer.Elapsed += IncrementTicks; // Set callback.

        // Set inital tick.
        CurrentTick = initalTime / msInterval;
    }
    #endregion
}

public class NoteDisplayArgs : EventArgs
{
    public short NoteNumber { get; set; }
    public short Channel { get; set; }
    public float PlaybackPosition { get; set; }
    public float Length { get; set; }
}

public class CustomPlaybackEngine
{
    #region Properties
    private static PlaybackClock _ticker = null;
    private float _forgiveness = 400f;
    private OutputDevice _outputDevice = null;
    #endregion

    #region events
    public event EventHandler<NoteDisplayArgs> NoteReachedRunway;
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
    public CustomPlaybackEngine(string songFilePath, OutputDevice outputDevice = null, float playbackSpeed = 1f, 
        float forgiveness = 400f, int qNoteLeadup = 4, bool forScrubbing = false)
    {
        if (forScrubbing) throw new System.NotImplementedException("Scrubbing method is not implemented yet.");

        // Read midi file.
        var midiFile = MidiFile.Read(songFilePath);

        InitTicker(midiFile.GetTempoMap(), qNoteLeadup);

        InitOutputDevice(outputDevice);
    }
    #endregion

    #region Methods
    #endregion

    #region Initalization Methods
    private void InitOutputDevice(OutputDevice outputDevice)
    {
        if (outputDevice == null) return;
        if (_outputDevice != null)
        {
            _outputDevice.Dispose();
            _outputDevice = null;
        }

        _outputDevice = outputDevice;
        _outputDevice.PrepareForEventsSending();

    }

    private void InitTicker(TempoMap tempoMap, int qNoteLeadup)
    {
        if (_ticker != null)
        {
            _ticker.Dispose();
            _ticker = null;
        }

        var msPerTick = (float)TimeConverter.ConvertTo<MetricTimeSpan>(1, tempoMap).TotalMilliseconds;
        var ticksPerQNote = TimeConverter.ConvertFrom(new MusicalTimeSpan(4), tempoMap);
        var msLeadup = ticksPerQNote * qNoteLeadup * msPerTick;

        _ticker = new PlaybackClock(msPerTick, -msLeadup);
    }
    #endregion
}