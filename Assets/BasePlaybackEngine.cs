﻿using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using System;
using System.Collections.Generic;
using System.Linq;

public class NoteEvtData
{
    public short number;
    public float onTime;
    public float offTime;
    public float len;
}

public delegate void OnTimeInterval(float time);

public class PlaybackClock
{
    #region Properties
    private const float MAX_PRECISION = 16f;
    private readonly float _tickLength = 1f;
    private float _tickIncrementFactor = 1f;
    private System.Timers.Timer _intervalTimer = null;
    public event OnTimeInterval TickIncremented;
    private readonly float _minTime;
    #endregion

    #region GetterSetters
    public float CurrentTick { get; private set; } = 0f;
    public float CurrentTime
    {
        get
        {
            return _tickLength * CurrentTick;
        }
    }
    public float ClockSpeedFactor { get; private set; } = 1f;
    public bool InReverse { get; set; } = false;
    #endregion

    #region Methods
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
        CurrentTick += _tickIncrementFactor * (InReverse ? -1f : 1f );

        if (CurrentTick < _minTime / _tickLength)
        {
            CurrentTick = _minTime / _tickLength;
        }

        TickIncremented.Invoke(CurrentTick);
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

    public void OverwriteTime(float newTime)
    {
        float newTick = newTime / _tickLength;
        CurrentTick = newTick;
        TickIncremented.Invoke(newTick);
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
        _minTime = initalTime;
        CurrentTick = _minTime / msInterval;
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

public class BasePlaybackEngine
{
    #region Properties
    EventPlaybackManager _eventPlaybackManager;
    private float _forgiveness = 400f;
    private RunwayWrapper[] _runways = null;
    #endregion

    #region GetterSetterMethods
    public bool IsPlaying { get; private set; } = false;
    public float PlaybackSpeed
    {
        get
        {
            return _eventPlaybackManager?.PlaybackSpeed ?? 0f;
        }
        set
        {
            if (_eventPlaybackManager == null) return;
            _eventPlaybackManager.PlaybackSpeed = value;
        }
    }
    public float PlaybackTime
    {
        get
        {
            return _eventPlaybackManager.CurrentTime;
        }
    }
    #endregion

    #region Init Methods
    public void InitPrivateMembers(MidiFile midiFile, OutputDevice outputDevice, short qNoteLeadup,
        float forgiveness, PlaybackSettings playbackSettings)
    {
        // Init eventPlaybackManager
        _eventPlaybackManager = new(midiFile, outputDevice, qNoteLeadup, 1f);



        

        _forgiveness = forgiveness;
    }
    #endregion

    #region Methods
    #endregion
}

public class PlaybackSettings
{
    public List<List<int>> PlayersAndTracks { get; private set; } = null;

    public void AddPlayerAndRespectiveTracks(List<int> tracks)
    {
        PlayersAndTracks ??= new();
        PlayersAndTracks.Add(tracks);
    }

    public List<int> GetPlayerPlayedTracks()
    {
        var tracks = new List<int>();
        foreach (var trackSet in  PlayersAndTracks)
        {
            foreach(var track in trackSet)
            {
                if (tracks.Contains(track)) continue;
                tracks.Add(track);
            }
        }

        return tracks;
    }
}

internal class EventPlaybackManager
{
    #region Properties
    Stack<TimedEvent> _eventsToPlay;
    Stack<TimedEvent> _eventsPlayed;
    PlaybackClock _clock;
    OutputDevice _outputDevice;
    readonly float _startTime;
    #endregion

    #region Getter and Setter Methods
    public float CurrentTick{
        get
        {
            return _clock.CurrentTick;
        }
    }
    public float CurrentTime
    {
        get 
        {
            return _clock.CurrentTime; 
        }
    }
    public float PlaybackSpeed
    {
        get
        {
            return _clock.ClockSpeedFactor;
        }
        set
        {
            _clock.SetIncrementMultiplier(value);
        }
    }
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new event playback manager.
    /// </summary>
    /// <param name="midiFile">File containing the midi events to play.</param>
    /// <param name="outputDevice">Device to send midi events to.</param>
    /// <param name="qNoteLeadup">How much leadup to give clock in quarter notes.</param>
    /// <param name="playbackSpeed">How fast the clock ticks.</param>
    public EventPlaybackManager(MidiFile midiFile, OutputDevice outputDevice, short qNoteLeadup, float playbackSpeed)
    {
        // Get output device ready.
        outputDevice.PrepareForEventsSending();

        Stack<TimedEvent> _temp = new();

        // Get interval.
        float msPerTick = (float)TimeConverter.ConvertTo<MetricTimeSpan>(1, midiFile.GetTempoMap()).TotalMilliseconds;
        var qNote = TimeConverter.ConvertTo<MusicalTimeSpan>(4, midiFile.GetTempoMap());
        float ticksPerQNote = TimeConverter.ConvertFrom(qNote * qNoteLeadup, midiFile.GetTempoMap());

        // Init properties.
        _eventsToPlay = new();
        _eventsPlayed = new();
        _startTime = - msPerTick * ticksPerQNote;
        _clock = new(msPerTick, _startTime);
        _clock.TickIncremented += TickUpdated;
        _clock.SetIncrementMultiplier(playbackSpeed);

        // For reversing list.
        foreach (var evt in midiFile.GetTimedEvents())
        {
            _temp.Push(evt);
        }

        // Push reversed list to eventsToPlay.
        while (_temp.Count > 0)
        {
            _eventsToPlay.Push(_temp.Pop());
        }
    }
    #endregion

    #region Methods
    void TickUpdated(float newTick)
    {
        // Update playhead.
        while (newTick > _eventsToPlay.Peek().Time)
        {
            AdvancePlayHead();
        }

        while (newTick > _eventsToPlay.Peek().Time)
        {
            RegressPlayHead();
        }
    }

    /// <summary>
    /// Shifts playback head forwards one event.
    /// </summary>
    void AdvancePlayHead()
    {
        if (_eventsToPlay.Count < 1) return;
        _eventsPlayed.Push(_eventsToPlay.Pop());
        _outputDevice.SendEvent(_eventsPlayed.Peek().Event);
    }

    /// <summary>
    /// Shifts playback head back one event.
    /// </summary>
    void RegressPlayHead()
    {
        if (_eventsPlayed.Count < 1) return;
        _eventsToPlay.Push(_eventsPlayed.Pop());
        _outputDevice.TurnAllNotesOff();
    }

    /// <summary>
    /// Resumes playback.
    /// </summary>
    public void Start()
    {
        _clock.Start();
    }

    /// <summary>
    /// Pauses playback.
    /// </summary>
    public void Pause()
    {
        _clock.Stop();
        _outputDevice.TurnAllNotesOff();
    }

    /// <summary>
    /// Resets playhead to beginning.
    /// </summary>
    public void Restart()
    {
        _clock.Stop();
        _clock.OverwriteTime(_startTime);
        _clock.Start();
    }

    /// <summary>
    /// Releases resources.
    /// </summary>
    public void Dispose()
    {
        _clock.Dispose();
        _outputDevice.Dispose();
        _clock = null;
        _outputDevice = null;
    }
    #endregion
}

public class ScrubbablePlaybackEngine
{
    #region Properties
    EventPlaybackManager _autoPlayedNotes;
    #endregion

    #region Constructors
    ScrubbablePlaybackEngine()
    {

    }
    #endregion
}