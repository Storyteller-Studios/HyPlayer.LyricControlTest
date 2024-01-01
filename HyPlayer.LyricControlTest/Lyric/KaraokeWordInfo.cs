using System;

namespace HyPlayer.LyricControlTest.Lyric;

public class KaraokeWordInfo(TimeSpan startTime,TimeSpan endTime,string text)
{
    public TimeSpan StartTime { get; set; } = startTime;
    public TimeSpan EndTime { get; set; } = endTime;
    public string Text { get; set; } = text;
}