using Melanchall.DryWetMidi.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class DisplayManager : MonoBehaviour
{
    private float AspectRatio;
    private float Height;
    private LinkedList<GameObject>[] Notes;
    public GameObject NoteFab;

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
