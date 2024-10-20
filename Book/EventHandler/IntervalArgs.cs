using System;

namespace ShareInvest.EventHandler;

class IntervalArgs : EventArgs
{
    internal TimeSpan Interval
    {
        get;
    }

    internal IntervalArgs(TimeSpan interval)
    {
        Interval = interval;
    }
}