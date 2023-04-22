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

    public void CreateRunway(short[] NoteRange, float TimeToReachStrikeBar)
    {
        print("Initalizing runway.");
        
        UpdateDisplayInfo();
        print($"Aspect ratio: {AspectRatio}\nHeight: {Height}");
        var StrikebarHeight = 4; // Height of strikebar from the bottom.

        // Get note speed.
        float[] Dimensions = new float[2] { Height * AspectRatio * 2, Height * 2 }; // Dimensions of runway.
        float DistPerMs = (Dimensions[1] - StrikebarHeight) / TimeToReachStrikeBar;

        // Initalize runway.
        Runaway NewRunway;
        NewRunway.UnityInstance = Instantiate(RunwayFab, transform);
        NewRunway.Script = NewRunway.UnityInstance.GetComponent<Runway>();
        NewRunway.Script.Init(NoteRange, DistPerMs, Dimensions, StrikebarHeight);
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
