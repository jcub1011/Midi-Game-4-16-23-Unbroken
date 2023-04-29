using System.Collections;
using System.Collections.Generic;
using MIDI = Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Core;
using UnityEngine;
using System.IO;
using Melanchall.DryWetMidi.Multimedia;
using System.Threading;
using System;
using UnityEditor;

/*
[System.Serializable]
public class InitializeDisplayManager : UnityEngine.Events.UnityEvent<short[], TempoMap> { }*/

public struct NoteData
{
    public short Number;
    public float Length;
    public double Time;
    public short Channel;
}

public class MidiHandler : MonoBehaviour
{
    private MIDI.Playback _playbackEngine = null;
    private MIDI.InputDevice _inputMidi = null;
    private MIDI.OutputDevice _outputMidi = null;
    private MidiFile _currentMidi = null;
    private string _midiBaseDirectory = $"{Directory.GetCurrentDirectory()}\\Assets\\MidiFiles";
    private float _playbackOffset = 0; // miliseconds.
    private Queue<NoteData> _displayQueue = new();
    private CustomTickGenerator _introInterpolater; // For inserting notes before actual audio playback begins.
    private DisplayHandler _displayHandler = new();
    public GameObject Runway;
    private Runway _runwayScript;
    private float[] _prevDimensions = new float[2] { 0, 0 };
    public float CurrentDisplayTime
    {
        get
        {
            if (_playbackEngine == null || _introInterpolater == null) return 0f;
            return (float)(_playbackEngine.GetCurrentTime<MetricTimeSpan>().TotalMilliseconds + _introInterpolater.GetCurrentTime());
        }
    }
    public float CurrentPlaybackTime
    {
        get
        {
            if (_playbackEngine == null || _introInterpolater == null) return 0f;
            return (float)(CurrentDisplayTime - _playbackOffset);
        }
    }

    /// <summary>
    /// Attempts to open a midi file and initalize midi variables.
    /// </summary>
    /// <param name="FileName">Name of midi file to open.</param>
    /// <returns>If successfully opened midi.</returns>
    bool LoadMidi(string FileName)
    {
        // Clear PlaybackEngine.
        if (_playbackEngine != null)
        {
            print("Clearing playback engine.");
            _playbackEngine.Stop();
            _playbackEngine.Dispose();
            _playbackEngine = null;
        }

        // Read file.
        print($"Loading midi file: {FileName}");
        _currentMidi = MidiFile.Read($"{_midiBaseDirectory}\\{FileName}.mid");
        print("Successfully loaded midi file.");

        GetMidiInformationForDisplay();

        // Init display queue.
        foreach (var note in _currentMidi.GetNotes())
        {
            var noteData = new NoteData();

            noteData.Number = note.NoteNumber;
            noteData.Length = (float)TimeConverter.ConvertTo<MetricTimeSpan>(note.Length,
                _currentMidi.GetTempoMap()).TotalMilliseconds;
            noteData.Time = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time,
                _currentMidi.GetTempoMap()).TotalMilliseconds;

            _displayQueue.Enqueue(noteData);
        }

        // Get input midi.
        if (GetInputMidi())
        {
            print($"Using input device: {_inputMidi.Name}");
        }


        // Init playback settings.
        MIDI.PlaybackSettings settings = new MIDI.PlaybackSettings
        {
            ClockSettings = new MIDI.MidiClockSettings
            {
                CreateTickGeneratorCallback = () => new MIDI.RegularPrecisionTickGenerator()
            }
        };

        // Init Playback.
        if (GetOutputMidi())
        {
            _playbackEngine = _currentMidi.GetPlayback(_outputMidi, settings);
            print($"Using output device: {_outputMidi.Name}");
        }
        else
        {
            _playbackEngine = _currentMidi.GetPlayback(settings);
        }

        return true;
    }

    /// <summary>
    /// Gets a midi device and assigns it to OutputMidi. Overwrites with null if unavailable.
    /// </summary>
    /// <returns>If sucessfully found output device.</returns>
    bool GetOutputMidi()
    {
        if (MIDI.OutputDevice.GetDevicesCount() > 0)
        {
            _outputMidi = MIDI.OutputDevice.GetByIndex(0);
            _outputMidi.PrepareForEventsSending();
            return true;
        }
        else
        {
            _outputMidi = null;
            return false;
        }
    }

    /// <summary>
    /// Gets a midi device and assigns it to InputMidi. Overwrites with null if unavailable.
    /// </summary>
    /// <returns>If sucessfully found input device.</returns>
    bool GetInputMidi()
    {
        if (MIDI.InputDevice.GetDevicesCount() > 0)
        {
            _inputMidi = MIDI.InputDevice.GetByIndex(0);
            _inputMidi.EventReceived += OnEventRecieved;
            _inputMidi.StartEventsListening();
            return true;
        }
        else
        {
            _inputMidi = null;
            return false;
        }
    }

    void OnEventRecieved(object sender, MidiEventReceivedEventArgs evt)
    {
        switch (evt.Event.EventType)
        {
            case MidiEventType.NoteOn:
                var NoteOn = (NoteOnEvent)evt.Event;
                // print($"Note number: {NoteOn.NoteNumber}, Velocity: {NoteOn.Velocity}");
                _runwayScript.GetNoteInputAccuracy(CurrentPlaybackTime, NoteOn);
                break;

            case MidiEventType.NoteOff:
                var NoteOff = (NoteOffEvent)evt.Event;
                // print($"Note number: {NoteOff.NoteNumber}, Off Velocity: {NoteOff.Velocity}");
                _runwayScript.GetNoteInputAccuracy(CurrentPlaybackTime, NoteOff);
                break;
        }
    }

    /// <summary>
    /// Gets the range of notes in the midi file.
    /// </summary>
    /// <returns>An array of length 2 containing the minimum and maximum note number.</returns>
    IntRange GetNoteRange()
    {
        // Keep track of min and max note.
        short MinNote = short.MaxValue;
        short MaxNote = short.MinValue;

        // Find min and max note.
        foreach (var note in _currentMidi.GetNotes())
        {
            if (note.NoteNumber < MinNote)
            {
                MinNote = note.NoteNumber;
            }
            if (note.NoteNumber > MaxNote)
            {
                MaxNote = note.NoteNumber;
            }
        }

        return new IntRange ( MinNote, MaxNote );
    }

    /// <summary>
    /// Updates the display manager script, playback offset, and creates a runway in display manager.
    /// </summary>
    void GetMidiInformationForDisplay()
    {
        short KeyboardSize = 0; // Number of notes.
        IntRange SmallKeyboardRange = new (36, 96); // 61 Key
        IntRange MediumKeyboardRange = new (28, 103); // 76 Key
        IntRange LargeKeyboardRange = new (21, 108); // 88 Key
        IntRange NoteRange = GetNoteRange();

        // Check ranges.
        if (SmallKeyboardRange.InRange(NoteRange))
        {
            KeyboardSize = 61;
        } else if (MediumKeyboardRange.InRange(NoteRange))
        {
            KeyboardSize = 76;
        } else if (LargeKeyboardRange.InRange(NoteRange))
        {
            KeyboardSize = 88;
        } else
        {
            KeyboardSize = (short)NoteRange.Len;
        }

        print($"Fits {KeyboardSize} key keyboard.");
    }

    /// <summary>
    /// Adds upcomming notes to the runway display queue.
    /// </summary>
    void PushNotesToRunway()
    {
        if (_playbackEngine == null || _introInterpolater == null)
        {
            return;
        }

        if (!_playbackEngine.IsRunning)
        {
            // Stop interpolater when intro offset is reached.
            if (_introInterpolater.GetCurrentTime() > _playbackOffset)
            {
                _introInterpolater.Stop();
                _introInterpolater.FixTime(_playbackOffset);
                _playbackEngine.Start();
            }
        }

        var CurrentTime = CurrentDisplayTime;

        while (_displayQueue.Count > 0 && _runwayScript != null)
        {
            if (_displayQueue.Peek().Time < CurrentTime)
            {
                var note = _displayQueue.Dequeue();
                // print($"Time: {note.Time}, Length: {note.Length}");

                _runwayScript.AddNoteToQueue(note.Number, note.Length, (float)note.Time);
                // DisplayManagerScript.AddNoteToRunway(note.Number, note.Length, (float)note.Time);
            } else
            {
                break;
            }
        }

        // DisplayManagerScript.UpdateRunways((float)CurrentTime);
        _runwayScript.UpdateLanes((float)CurrentTime);
    }

    void Start()
    {
        LoadMidi("Collision Test");
        // Init interpolater.
        _introInterpolater = new(TimeConverter.ConvertTo<MetricTimeSpan>(1, _currentMidi.GetTempoMap()).TotalMilliseconds);

        // Init runway.
        _runwayScript = Runway.GetComponent<Runway>();
        float[] Dimensions = new float[2] { _displayHandler.Width, _displayHandler.Height };

        // Get time to hit runway.
        var TimeSpanQNotes = new MusicalTimeSpan(4) * 8;
        _playbackOffset = (float)TimeConverter.ConvertTo<MetricTimeSpan>(TimeSpanQNotes, _currentMidi.GetTempoMap()).TotalMilliseconds;
        _runwayScript.Init(GetNoteRange(), Dimensions, 4, _playbackOffset, 400f);

        // Start playback.
        _introInterpolater.Start();
    }

    bool ScreenResized()
    {
        if (_displayHandler.Width != _prevDimensions[0] || _displayHandler.Height != _prevDimensions[1])
        {
            print($"Screen dimensions changed to '{_displayHandler.Width} X {_displayHandler.Height}'.");
            _prevDimensions[0] = _displayHandler.Width;
            _prevDimensions[1] = _displayHandler.Height;
            return true;
        }

        return false;
    }

    // Update is called once per frame
    void Update()
    {
        if (ScreenResized())
        {
            _runwayScript.UpdateNoteDisplayInfo(new float[] { _displayHandler.Width, _displayHandler.Height });
        }
        PushNotesToRunway();
    }

    private void OnApplicationQuit()
    {
        // Cleanup.
        print("Releasing resources.");
        if (_playbackEngine != null)
        {
            _playbackEngine.Stop();
            _playbackEngine.Dispose();
            _playbackEngine = null;
            print("Disposed playback engine.");
        }
        if (_introInterpolater != null)
        {
            _introInterpolater.Stop();
            _introInterpolater.DisposeTimer();
            _introInterpolater = null;
            print("Disposed intro interpolater.");
        }
        if (_outputMidi != null)
        {
            _outputMidi.Dispose();
        }
        if (_inputMidi != null)
        {
            _inputMidi.Dispose();
        }
    }
}
