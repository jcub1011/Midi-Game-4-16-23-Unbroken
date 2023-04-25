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
    private MIDI.Playback PlaybackEngine = null;
    private MIDI.InputDevice InputMidi = null;
    private MIDI.OutputDevice OutputMidi = null;
    private MidiFile CurrentMidi = null;
    private string MidiBaseDirectory = $"{Directory.GetCurrentDirectory()}\\Assets\\MidiFiles";
    private float PlaybackOffset = 0;
    private Queue<NoteData> DisplayQueue = new();
    private CustomTickGenerator IntroInterpolater; // For inserting notes before actual audio playback begins.
    private DisplayHandler DisplayHandler = new();
    public GameObject Runway;
    private Runway RunwayScript;
    private float[] PrevDimensions = new float[2] { 0, 0 };

    /// <summary>
    /// Attempts to open a midi file and initalize midi variables.
    /// </summary>
    /// <param name="FileName">Name of midi file to open.</param>
    /// <returns>If successfully opened midi.</returns>
    bool LoadMidi(string FileName)
    {
        // Clear PlaybackEngine.
        if (PlaybackEngine != null)
        {
            print("Clearing playback engine.");
            PlaybackEngine.Stop();
            PlaybackEngine.Dispose();
            PlaybackEngine = null;
        }

        // Read file.
        print($"Loading midi file: {FileName}");
        CurrentMidi = MidiFile.Read($"{MidiBaseDirectory}\\{FileName}.mid");
        print("Successfully loaded midi file.");

        GetMidiInformationForDisplay();

        // Init display queue.
        foreach (var note in CurrentMidi.GetNotes())
        {
            var noteData = new NoteData();

            noteData.Number = note.NoteNumber;
            noteData.Length = (float)TimeConverter.ConvertTo<MetricTimeSpan>(note.Length,
                CurrentMidi.GetTempoMap()).TotalMilliseconds;
            noteData.Time = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time,
                CurrentMidi.GetTempoMap()).TotalMilliseconds;

            DisplayQueue.Enqueue(noteData);
        }

        // Get input midi.
        if (GetInputMidi())
        {
            print($"Using input device: {InputMidi.Name}");
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
            PlaybackEngine = CurrentMidi.GetPlayback(OutputMidi, settings);
            print($"Using output device: {OutputMidi.Name}");
        }
        else
        {
            PlaybackEngine = CurrentMidi.GetPlayback(settings);
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
            OutputMidi = MIDI.OutputDevice.GetByIndex(0);
            OutputMidi.PrepareForEventsSending();
            return true;
        }
        else
        {
            OutputMidi = null;
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
            InputMidi = MIDI.InputDevice.GetByIndex(0);
            InputMidi.EventReceived += OnEventRecieved;
            InputMidi.StartEventsListening();
            return true;
        }
        else
        {
            InputMidi = null;
            return false;
        }
    }

    void OnEventRecieved(object sender, MidiEventReceivedEventArgs evt)
    {
        switch (evt.Event.EventType)
        {
            case MidiEventType.NoteOn:
                var NoteOn = (NoteOnEvent)evt.Event;
                print($"Note number: {NoteOn.NoteNumber}, Velocity: {NoteOn.Velocity}");
                break;

            case MidiEventType.NoteOff:
                var NoteOff = (NoteOffEvent)evt.Event;
                print($"Note number: {NoteOff.NoteNumber}, Off Velocity: {NoteOff.Velocity}");
                break;
        }
    }

    /// <summary>
    /// Gets the range of notes in the midi file.
    /// </summary>
    /// <returns>An array of length 2 containing the minimum and maximum note number.</returns>
    short[] GetNoteRange()
    {
        // Keep track of min and max note.
        short MinNote = short.MaxValue;
        short MaxNote = short.MinValue;

        // Find min and max note.
        foreach (var note in CurrentMidi.GetNotes())
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

        return new short[2] { MinNote, MaxNote };
    }

    /// <summary>
    /// Checks if the range will fit the other range.
    /// </summary>
    /// <param name="Range">The range you want to check.</param>
    /// <param name="RangeToFit">The range to compare against.</param>
    /// <returns>Bool</returns>
    bool FitsInRange(short[] Range, short[] RangeToFit)
    {
        if (Range[0] <= RangeToFit[0] && Range[1] >= RangeToFit[1])
        {
            return true;
        } else
        {
            return false;
        }
    }

    /// <summary>
    /// Updates the display manager script, playback offset, and creates a runway in display manager.
    /// </summary>
    void GetMidiInformationForDisplay()
    {
        short KeyboardSize = 0; // Number of notes.
        short[] SmallKeyboardRange = new short[2] {36, 96}; // 61 Key
        short[] MediumKeyboardRange = new short[2] {28, 103}; // 76 Key
        short[] LargeKeyboardRange = new short[2] {21, 108}; // 88 Key
        short[] NoteRange = GetNoteRange();

        // Check ranges.
        if (FitsInRange(SmallKeyboardRange, NoteRange))
        {
            KeyboardSize = 61;
        } else if (FitsInRange(MediumKeyboardRange, NoteRange))
        {
            KeyboardSize = 76;
        } else if (FitsInRange(LargeKeyboardRange, NoteRange))
        {
            KeyboardSize = 88;
        } else
        {
            KeyboardSize = (short)(NoteRange[1] - NoteRange[0] + 1);
        }

        print($"Fits {KeyboardSize} key keyboard.");
    }

    /// <summary>
    /// Adds upcomming notes to the runway display queue.
    /// </summary>
    void PushNotesToRunway()
    {
        if (PlaybackEngine == null || IntroInterpolater == null)
        {
            return;
        }

        if (!PlaybackEngine.IsRunning)
        {
            // Stop interpolater when intro offset is reached.
            if (IntroInterpolater.GetCurrentTime() > PlaybackOffset)
            {
                IntroInterpolater.Stop();
                IntroInterpolater.FixTime(PlaybackOffset);
                PlaybackEngine.Start();
            }
        }

        var CurrentTime = PlaybackEngine.GetCurrentTime<MetricTimeSpan>().TotalMilliseconds + IntroInterpolater.GetCurrentTime();

        while (DisplayQueue.Count > 0 && RunwayScript != null)
        {
            if (DisplayQueue.Peek().Time < CurrentTime)
            {
                var note = DisplayQueue.Dequeue();
                // print($"Time: {note.Time}, Length: {note.Length}");

                RunwayScript.AddNoteToQueue(note.Number, note.Length, (float)note.Time);
                // DisplayManagerScript.AddNoteToRunway(note.Number, note.Length, (float)note.Time);
            } else
            {
                break;
            }
        }

        // DisplayManagerScript.UpdateRunways((float)CurrentTime);
        RunwayScript.UpdateNotesPositions((float)CurrentTime);
    }

    void Start()
    {
        LoadMidi("NeverGonnaGiveYouUp");
        // Init interpolater.
        IntroInterpolater = new(TimeConverter.ConvertTo<MetricTimeSpan>(1, CurrentMidi.GetTempoMap()).TotalMilliseconds);

        // Init runway.
        RunwayScript = Runway.GetComponent<Runway>();
        float[] Dimensions = new float[2] { DisplayHandler.Width, DisplayHandler.Height };
        // Get time to hit runway.
        var TimeSpanQNotes = new MusicalTimeSpan(4) * 32;
        PlaybackOffset = (float)TimeConverter.ConvertTo<MetricTimeSpan>(TimeSpanQNotes, CurrentMidi.GetTempoMap()).TotalMilliseconds;
        RunwayScript.Init(GetNoteRange(), Dimensions, 4, PlaybackOffset);

        // Start playback.
        IntroInterpolater.Start();
    }

    bool ScreenResized()
    {
        if (DisplayHandler.Width != PrevDimensions[0] || DisplayHandler.Height != PrevDimensions[1])
        {
            print($"Screen dimensions changed to '{DisplayHandler.Width} X {DisplayHandler.Height}'.");
            PrevDimensions[0] = DisplayHandler.Width;
            PrevDimensions[1] = DisplayHandler.Height;
            return true;
        }

        return false;
    }

    // Update is called once per frame
    void Update()
    {
        if (ScreenResized())
        {
            RunwayScript.UpdateNoteDisplayInfo(new float[] { DisplayHandler.Width, DisplayHandler.Height });
        }
        PushNotesToRunway();
    }

    private void OnApplicationQuit()
    {
        // Cleanup.
        print("Releasing resources.");
        if (PlaybackEngine != null)
        {
            PlaybackEngine.Stop();
            PlaybackEngine.Dispose();
            PlaybackEngine = null;
            print("Disposed playback engine.");
        }
        if (IntroInterpolater != null)
        {
            IntroInterpolater.Stop();
            IntroInterpolater.DisposeTimer();
            IntroInterpolater = null;
            print("Disposed intro interpolater.");
        }
        if (OutputMidi != null)
        {
            OutputMidi.Dispose();
        }
        if (InputMidi != null)
        {
            InputMidi.Dispose();
        }
    }
}
