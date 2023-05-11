using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NoteEvtData
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
    public float CurrentTime
    {
        get
        {
            return _tickLength * CurrentTick;
        }
    }
    public float ClockSpeedFactor { get; private set; } = 1f;
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
    private RunwayWrapper[] _runways = null;
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
    public float PlaybackTime
    {
        get
        {
            return _ticker.CurrentTime;
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

        _forgiveness = forgiveness;
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

class SongManagerSettings
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

public class SongManager
{
    #region Properties
    PlaybackClock _clock;
    OutputDevice _outputDevice;
    List<Runway> _runways;
    #endregion

    #region Getters and Setters
    public bool IsPlaying { get; private set; } = false;
    #endregion

    #region Constructors
    SongManager(MidiFile midiFile, OutputDevice outputDevice, SongManagerSettings settings)
    {
        var tracks = midiFile.GetTrackChunks().ToList();
        var tracksForPlayers = settings.GetPlayerPlayedTracks().ToList();
        var eventsAutoPlayList = new List<TrackChunk>();

        // Get list of events to auto play.
        for (int i = 0; i < tracks.Count; i++)
        {
            if (tracksForPlayers.Contains(i)) continue;
            eventsAutoPlayList.Add(tracks[i]);
        }

        var autoPlayMidiFile = new MidiFile(eventsAutoPlayList);

        // Get list of notes for players to play.
        var notesForPlayers = new List<List<Note>>();
        foreach (var trackIndicies in settings.PlayersAndTracks)
        {
            var notesForPlayer = new List<Note>();

            // Implement way of geting notes that each player has to play.
            // notesForPlayers.Add();
        }

        float tickLen = (float)TimeConverter.ConvertTo<MetricTimeSpan>(1, midiFile.GetTempoMap()).TotalMilliseconds;
        _clock = new PlaybackClock(tickLen);
    }
    #endregion

    #region Methods
    void SendEventToOutput()
    {

    }
    #endregion
}