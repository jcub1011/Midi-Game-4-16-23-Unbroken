using System.Collections;
using System.Collections.Generic;
using MIDI = Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Core;
using UnityEngine;
using System.IO;
using UnityEngine.XR;
using Melanchall.DryWetMidi.Multimedia;

public class MidiHandler : MonoBehaviour
{
    private MIDI.Playback PlaybackEngine = null;
    private MIDI.InputDevice InputMidi = null;
    private MIDI.OutputDevice OutputMidi = null;
    private MidiFile CurrentMidi = null;
    private string MidiBaseDirectory = $"{Directory.GetCurrentDirectory()}\\Assets\\MidiFiles";

    /// <summary>
    /// Attempts to open a midi file and initalize midi variables.
    /// </summary>
    /// <param name="FileName">Name of midi file to open.</param>
    /// <returns>If successfully opened midi.</returns>
    bool LoadMidi(string FileName)
    {
        print($"Loading midi file: {FileName}");
        CurrentMidi = MidiFile.Read($"{MidiBaseDirectory}\\{FileName}.mid");
        print("Successfully loaded midi file.");

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
        PlaybackEngine.Start();

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

    void Start()
    {
        LoadMidi("TomOdelAnotherLove");
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnApplicationQuit()
    {
        print("Releasing resources.");
        if (PlaybackEngine != null)
        {
            PlaybackEngine.Stop();
            PlaybackEngine.Dispose();
            print("Disposed playback engine.");
        }
    }
}
