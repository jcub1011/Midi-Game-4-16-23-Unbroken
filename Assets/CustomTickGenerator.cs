using UnityEngine;
using System.Timers;

public class CustomTickGenerator
{
    private static Timer Ticker;
    private double Tick = 0;
    private double TickIncrementFactor = 1;
    private bool Counting = false;
    private double TickLength; // In Milliseconds..
    private double PlaybackSpeed = 1;
    private double Time = 0.0;

    /// <summary>
    /// Creates a new tick generator.
    /// </summary>
    /// <param name="interval">The miliseconds between each tick.</param>
    public CustomTickGenerator(double interval)
    {
        MonoBehaviour.print("Initalizing new playback.");
        Ticker = new System.Timers.Timer();
        TickLength = interval;
        Ticker.Elapsed += IncrementTick;
        // Ticker.Elapsed += callbackFunction;
        Ticker.AutoReset = true;
        Ticker.Enabled = true;
        SetPlaybackSpeed(1);
        Tick = 0;
    }

    /// <summary>
    /// Recovers the resources used by the tick generator.
    /// </summary>
    public void DisposeTimer()
    {
        MonoBehaviour.print("Disposing timer.");
        Ticker.Stop();
        Ticker.AutoReset = false;
        Ticker.Enabled = false;
        Ticker.Dispose();
    }

    private void IncrementTick(System.Object source, ElapsedEventArgs evt)
    {
        if (Counting)
        {
            MonoBehaviour.print("Timer Tick");
            Tick += TickIncrementFactor;
            Time = Tick * TickLength;
        }
    }

    /// <summary>
    /// Changes the playback speed.
    /// </summary>
    /// <param name="speed">The new playback speed. 1 is normal speed.</param>
    public void SetPlaybackSpeed(double speed)
    {
        const double MAX_PRECISION = 16; // Maximum precision of typical system timer.
        PlaybackSpeed = speed;
        var interval = TickLength / PlaybackSpeed;
        MonoBehaviour.print($"Time between ticks: {interval}");
        MonoBehaviour.print($"Setting playback speed: {speed}x");

        if (interval >= MAX_PRECISION)
        {
            MonoBehaviour.print("Within max precision.");
            TickIncrementFactor = 1;
            Ticker.Interval = interval;
        }
        else
        {
            MonoBehaviour.print("Outside of max precision. Interpolating ticks.");
            TickIncrementFactor = MAX_PRECISION / interval; // Linear Approximation
            Ticker.Interval = MAX_PRECISION;
            MonoBehaviour.print($"Interpolation factor: {TickIncrementFactor}");
        }
    }

    /// <summary>
    /// Gets the speed the tick generator is operating at.
    /// </summary>
    /// <returns></returns>
    public double GetPlaybackSpeed()
    {
        return PlaybackSpeed;
    }

    /// <summary>
    /// Stops the tick generator.
    /// </summary>
    public void Stop()
    {
        MonoBehaviour.print("Stopping playback.");
        Counting = false;
        Ticker.Stop();
    }

    /// <summary>
    /// Starts the tick generator.
    /// </summary>
    public void Start()
    {
        MonoBehaviour.print("Starting playback.");
        Counting = true;
        Ticker.Start();
        MonoBehaviour.print("Playback started.");
    }

    /// <summary>
    /// Gets the current tick.
    /// </summary>
    /// <returns>The current tick.</returns>
    public double GetCurrentTick()
    {
        return Tick;
    }

    /// <summary>
    /// Gets the current time in miliseconds.
    /// </summary>
    /// <returns>The current time in miliseconds.</returns>
    public double GetCurrentTime()
    {
        return Time;
    }

    /// <summary>
    /// Overrides the current time.
    /// </summary>
    /// <param name="time">The time in miliseconds to overwrite with.</param>
    public void FixTime(double time)
    {
        Tick = time / TickLength;
        Time = time;
    }
}