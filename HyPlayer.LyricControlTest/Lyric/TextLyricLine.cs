using System;
using HyPlayer.LyricControlTest.Lyric.Base;
using HyPlayer.LyricControlTest.Lyric.Interfaces;

namespace HyPlayer.LyricControlTest.Lyric;

public class TextLyricLine(TimeSpan startTime, TimeSpan endTime ,string text) : LyricLineBase(startTime, endTime), ITextLyricLine
{
    public string? Text { get ; set ; } = text;
}