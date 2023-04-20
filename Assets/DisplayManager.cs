using Melanchall.DryWetMidi.Interaction;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


public class DisplayManager : MonoBehaviour
{
    private float AspectRatio;
    private float Height;
    private LinkedList<GameObject> Runways;
    public GameObject RunwayFab;

    public void Init(short[] NoteRange, TempoMap Tempo)
    {
        var NewRunway = Instantiate(RunwayFab, transform);
        NewRunway.GetComponent<Runway>().Init(NoteRange, (float)(0.0), new float[2] { Height * 2 * AspectRatio, Height * 2});
        Runways.AddFirst(NewRunway);
    }

    void UpdateDisplayInfo()
    {
        AspectRatio = Camera.main.aspect;
        Height = Camera.main.orthographicSize;
    }

    public void InitalizeRunway(short[] NoteRange, TempoMap Tempo, float[] Dimensions)
    {
        print("Initalizing runway.");

    }


    // Start is called before the first frame update
    void Start()
    {
        UpdateDisplayInfo();
        print($"Aspect ratio: {AspectRatio}\nHeight: {Height}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
