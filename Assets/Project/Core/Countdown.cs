using System;

public class Countdown
{
    private readonly float _duration;
    private float _startTime;
    private bool _hasStarted;
    private bool _isRunning;

    public Countdown(float duration)
    {
        if (duration < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration must not be negative.");
        }
        _duration = duration;
    }

    public void Start(float now)
    {
        ThrowIfNegative(now);
        _startTime = now;
        _hasStarted = true;
        _isRunning = true;
    }

    public void Run(float now)
    {
        if (!_hasStarted)
        {
            throw new InvalidOperationException("Countdown has not been started.");
        }
        if (!_isRunning)
        {
            return; // already finished, nothing left to do
        }

        ThrowIfNegative(now);
        if (now < _startTime)
        {
            throw new ArgumentOutOfRangeException(nameof(now), now, $"Must not be before start time {_startTime}.");
        }

        _isRunning = (now - _startTime) <= _duration;
    }

    public bool IsFinished() => _hasStarted && !_isRunning;

    public bool IsRunning() => _isRunning;

    private static void ThrowIfNegative(float now)
    {
        if (now < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(now), now, "Time must not be negative.");
        }
    }
}