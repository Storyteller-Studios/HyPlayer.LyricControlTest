using HyPlayer.LyricControlTest.Lyric.Base;
using System;

namespace HyPlayer.LyricControlTest.Lyric;

public class EmptyLyricLine(TimeSpan startTime, TimeSpan endTime) : LyricLineBase(startTime, endTime)
{
}