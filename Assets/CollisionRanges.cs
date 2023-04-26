using UnityEditor;
using UnityEngine;

public class CollisionRanges
{
    public float OnPerfectRange { get; private set; }
    public float OnGoodRange { get; private set; }
    public float OnOkayRange { get; private set; }

    public float OffPerfectRange { get; private set; }
    public float OffGoodRange { get; private set; }
    public float OffOkayRange { get; private set; }

    /// <summary>
    /// Creates a new set of collsion ranges.
    /// </summary>
    /// <param name="OnRanges">(Length 3) List of collision ranges. [perfect, good, okay]</param>
    /// <param name="OffRanges">(Length 3) List of collision ranges. [perfect, good, okay]</param>
    public CollisionRanges(float[] OnRanges, float[] OffRanges)
    {
        OnPerfectRange = OnRanges[0];
        OnGoodRange = OnRanges[1];
        OnOkayRange = OnRanges[2];

        OffPerfectRange = OffRanges[0];
        OffGoodRange = OffRanges[1];
        OffOkayRange = OffRanges[2];
    }

    /// <summary>
    /// Creates a new set of collsion ranges.
    /// </summary>
    /// <param name="Ranges">(Length 3) List of collision ranges for both on and off collsion. [perfect, good, okay]</param>
    public CollisionRanges(float[] Ranges)
    {
        OnPerfectRange = Ranges[0];
        OnGoodRange = Ranges[1];
        OnOkayRange = Ranges[2];

        OffPerfectRange = OnPerfectRange;
        OffGoodRange = OnGoodRange;
        OffOkayRange = OnOkayRange;
    }
}