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

[System.Serializable]
public class InitializeDisplayManager : UnityEngine.Events.UnityEvent<short[], TempoMap> { }

public struct NoteData
{
    public short Number;
    public float Length;
    public double Time;
    public short Channel;
}

public class MidiHandler : MonoBehaviour
{
    // TODO: Implement way of spawning notes a set amount of bars ahead of playback
    // and keep synchronization so notes hit input bar at the same time playback sends the note on event.
    // Stay synchronized regardless of framerate.
    // IDEA: Create function that looks at notes a set time ahead of playback so that when the note reaches the strike bar playback will have reached that note.
    // Delay the start of playback.
    // IDEA: Loop through notes and delay playback by set time. Have playback start immediately instead and create custom function to spawn upcomming notes
    // a set amount ahead of time.

    private MIDI.Playback PlaybackEngine = null;
    private MIDI.InputDevice InputMidi = null;
    private MIDI.OutputDevice OutputMidi = null;
    private MidiFile CurrentMidi = null;
    private string MidiBaseDirectory = $"{Directory.GetCurrentDirectory()}\\Assets\\MidiFiles";
    public GameObject NoteDisplayManager;
    private DisplayManager DisplayManagerScript;
    private float PlaybackOffset = 0;
    private Queue<NoteData> DisplayQueue = new();
    public InitializeDisplayManager InitDisplayManager;

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

        print($"Loading midi file: {FileName}");
        CurrentMidi = MidiFile.Read($"{MidiBaseDirectory}\\{FileName}.mid");
        print("Successfully loaded midi file.");

        GetMidiInformationForDisplay();
        OffsetNotesPlayback();

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
            return true;
        }
        else
        {
            InputMidi = null;
            return false;
        }
    }

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
    /// Gets the amount of time it takes for a note to touch the strikebar from the top of the runway.
    /// </summary>
    /// <param name="QuarterNotesToDisplay">Quarter note leadup length.</param>
    /// <returns>Miliseconds.</returns>
    float GetTimeToHitStrikebar (int QuarterNotesToDisplay)
    {
        var MiliPerQuarterNote = TimeConverter.ConvertTo<MetricTimeSpan>(new MusicalTimeSpan(4), CurrentMidi.GetTempoMap()).TotalMilliseconds; // Converts a quarter note to miliseconds.
        print($"Miliseconds per quarter note: {MiliPerQuarterNote}");
        return (float)(MiliPerQuarterNote * QuarterNotesToDisplay);
    }

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

        DisplayManagerScript = NoteDisplayManager.GetComponent<DisplayManager>();
        PlaybackOffset = GetTimeToHitStrikebar(12);
        DisplayManagerScript.CreateRunway(NoteRange, PlaybackOffset);
        // InitDisplayManager.Invoke(NoteRange, CurrentMidi.GetTempoMap());
    }

    void PushNotesToRunway()
    {
        if (PlaybackEngine == null)
        {
            return;
        }
        var CurrentTime = PlaybackEngine.GetCurrentTime<MetricTimeSpan>().TotalMilliseconds + PlaybackOffset;
        while (DisplayQueue.Count > 0)
        {
            if (DisplayQueue.Peek().Time < CurrentTime)
            {
                var note = DisplayQueue.Dequeue();
                print($"Time: {note.Time}, Length: {note.Length}");

                DisplayManagerScript.AddNoteToRunway(note.Number, note.Length);
            } else
            {
                break;
            }
        }
    }

    void OffsetNotesPlayback()
    {
        var OffsetInMetric = new MetricTimeSpan((long)(PlaybackOffset * 1000));
        var OffsetInTicks = TimeConverter.ConvertFrom(OffsetInMetric, CurrentMidi.GetTempoMap());

        //CurrentMidi.ShiftEvents(OffsetInMetric);
        
        Action<TimedEvent> shiftEvt = (evt) => {
            if (evt.Event.EventType != MidiEventType.NoteOn && evt.Event.EventType != MidiEventType.NoteOff)
            {
                // If not a note.
                evt.Time = evt.Time + OffsetInTicks;
            }
        };
        Action<Chord> shiftChord = (chord) =>
        {
            chord.Time = chord.Time + OffsetInTicks;
        };

        CurrentMidi.ProcessTimedEvents(shiftEvt);
        CurrentMidi.ProcessChords(shiftChord);
    }

    void Start()
    {
        InitDisplayManager = new();
        LoadMidi("TomOdelAnotherLove");
        PlaybackEngine.Start();
    }

    // Update is called once per frame
    void Update()
    {
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
