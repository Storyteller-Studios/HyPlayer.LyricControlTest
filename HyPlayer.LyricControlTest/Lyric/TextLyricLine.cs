using System;
using HyPlayer.LyricControlTest.Lyric.Base;
using HyPlayer.LyricControlTest.Lyric.Interfaces;

namespace HyPlayer.LyricControlTest.Lyric;

public class TextLyricLine(TimeSpan startTime, TimeSpan endTime ,string text, string? translation = null) : LyricLineBase(startTime, endTime), ITextLyricLine
{
    public string? Text { get ; set ; } = text;
    public string? Translation { get; set; } = translation;
}