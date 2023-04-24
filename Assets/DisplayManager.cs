using UnityEngine;

struct Runaway
{
    public GameObject UnityInstance;
    public Runway Script;
}


public class DisplayHandler
{
    public float AspectRatio
    {
        get 
        {
            return Camera.main.aspect;
        }
    }
    public float Height
    {
        get
        {
            return Camera.main.orthographicSize * 2;
        }
    }

    public float Width
    {
        get
        {
            return Height * AspectRatio;
        }
    }
}
