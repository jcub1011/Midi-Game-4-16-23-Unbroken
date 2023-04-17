using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DisplayManager : MonoBehaviour
{
    private float AspectRatio;
    private float Height;

    void UpdateDisplayInfo ()
    {
        AspectRatio = Camera.main.aspect;
        Height = Camera.main.orthographicSize;
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
