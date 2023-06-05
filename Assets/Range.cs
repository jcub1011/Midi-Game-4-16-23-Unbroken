
public class IntRange
{
    public int Min { get; private set; }
    public int Max { get; private set; }
    public int Len
    {
        get
        {
            return Max - Min + 1;
        }
    }
    public int Range 
    { 
        get
        {
            return Max - Min;
        } 
    }

    /// <summary>
    /// Creates a new range.
    /// </summary>
    /// <param name="min">Range minimum.</param>
    /// <param name="max">Range maximum.</param>
    public IntRange (int min, int max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Checks if the given range fits entirely inside this range.
    /// </summary>
    /// <param name="range">Range to check.</param>
    /// <returns>bool</returns>
    public bool InRange (IntRange range)
    {
        return (InRange(range.Min) && InRange(range.Max));
    }

    /// <summary>
    /// Checks if the point is inclusively within this range.
    /// </summary>
    /// <param name="point">Point to check.</param>
    /// <returns>bool</returns>
    public bool InRange(int point)
    {
        return Min <= point && point <= Max;
    }

    /// <summary>
    /// Checks if there is any overlap between the ranges.
    /// </summary>
    /// <param name="range">Range to compare with.</param>
    /// <returns>bool</returns>
    public bool RangesCollide (IntRange range)
    {
        // Checks if range is inside this range or this range is inside other range.
        if (InRange(range) || range.InRange(this))
        {
            return true;
        }

        // Checks if either min or max is inside this range.
        else if (InRange(range.Min) || InRange(range.Max))
        {
            return true;
        }

        return false;
    }
}

public class FloatRange
{
    public float Min { get; private set; }
    public float Max { get; private set; }
    public float Range
    {
        get
        {
            return Max - Min;
        }
    }

    /// <summary>
    /// Creates a new range.
    /// </summary>
    /// <param name="min">Range minimum.</param>
    /// <param name="max">Range maximum.</param>
    public FloatRange(float min, float max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Checks if the given range fits entirely inside this range.
    /// </summary>
    /// <param name="range">Range to check.</param>
    /// <returns>bool</returns>
    public bool InRange(FloatRange range)
    {
        return (InRange(range.Min) && InRange(range.Max));
    }

    /// <summary>
    /// Checks if the point is inclusively within this range.
    /// </summary>
    /// <param name="point">Point to check.</param>
    /// <returns>bool</returns>
    public bool InRange(float point)
    {
        return Min <= point && point <= Max;
    }

    /// <summary>
    /// Checks if there is any overlap between the ranges.
    /// </summary>
    /// <param name="range">Range to compare with.</param>
    /// <returns>bool</returns>
    public bool RangesCollide(FloatRange range)
    {
        // Checks if range is inside this range or this range is inside other range.
        if (InRange(range) || range.InRange(this))
        {
            return true;
        }

        // Checks if either min or max is inside this range.
        else if (InRange(range.Min) || InRange(range.Max))
        {
            return true;
        }

        return false;
    }
}