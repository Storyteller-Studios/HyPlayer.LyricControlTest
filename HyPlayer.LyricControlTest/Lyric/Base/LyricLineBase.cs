using System;

namespace HyPlayer.LyricControlTest.Lyric.Base;

public class LyricLineBase(TimeSpan startTime,TimeSpan endTime)
{
    public TimeSpan StartTime { get; set; } = startTime;
    public TimeSpan EndTime { get; set; } = endTime;
}