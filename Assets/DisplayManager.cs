using Melanchall.DryWetMidi.Interaction;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

struct Runaway
{
    public GameObject UnityInstance;
    public Runway Script;
}


public class DisplayManager : MonoBehaviour
{
    private float AspectRatio;
    private float Height;
    private LinkedList<Runaway> Runways = new();
    public GameObject RunwayFab;

    public void CreateRunway(short[] NoteRange, TempoMap Tempo)
    {
        print("Initalizing runway.");
        
        UpdateDisplayInfo();
        print($"Aspect ratio: {AspectRatio}\nHeight: {Height}");

        // Get note speed.
        float[] Dimensions = new float[2] { Height * AspectRatio, Height * 2 }; // Dimensions of runway.
        const short LenRunway = 2; // In bars.
        var MiliPerQuarter = TimeConverter.ConvertTo<MetricTimeSpan>(new MusicalTimeSpan(4), Tempo).TotalMilliseconds; // Converts a quarter note to miliseconds.
        print($"Miliseconds per quarter note: {MiliPerQuarter}");
        float DistPerMs = Dimensions[1] / (float)(4.0 * MiliPerQuarter * LenRunway); // Distance note should travel every milisecond.
                                                                                     // (Length runway / Time in ms to reach end of runway)

        // Initalize runway.
        Runaway NewRunway;
        NewRunway.UnityInstance = Instantiate(RunwayFab, transform);
        NewRunway.Script = NewRunway.UnityInstance.GetComponent<Runway>();
        NewRunway.Script.Init(NoteRange, DistPerMs, Dimensions);
        Runways.AddFirst(NewRunway);
    }

    void UpdateDisplayInfo()
    {
        AspectRatio = Camera.main.aspect;
        Height = Camera.main.orthographicSize;
    }

    public void AddNoteToRunway(short NoteNumber, float NoteLength)
    {
        Runways.First().Script.AddNoteToQueue(NoteNumber, NoteLength);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
