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

    public double GetPlaybackSpeed()
    {
        return PlaybackSpeed;
    }

    public void Stop()
    {
        MonoBehaviour.print("Stopping playback.");
        Counting = false;
        Ticker.Stop();
    }

    public void Start()
    {
        MonoBehaviour.print("Starting playback.");
        Counting = true;
        Ticker.Start();
        MonoBehaviour.print("Playback started.");
    }

    public double GetCurrentTick()
    {
        return Tick;
    }

    public double GetCurrentTime()
    {
        return Time;
    }

    public void FixTime(double time)
    {
        Tick = time / TickLength;
        Time = time;
    }
}