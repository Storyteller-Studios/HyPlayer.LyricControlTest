using System;
using System.Collections.Generic;
using System.Text;
using HyPlayer.LyricControlTest.Lyric.Base;
using HyPlayer.LyricControlTest.Lyric.Interfaces;

namespace HyPlayer.LyricControlTest.Lyric;

public class KaraokeLyricLine(TimeSpan startTime, TimeSpan endTime, List<KaraokeWordInfo> karaokeWords,string? translation = null) : LyricLineBase(startTime, endTime), IKaraokeLyricLine
{
    public List<KaraokeWordInfo> KaraokeWords { get; set; } = karaokeWords;
    public string? Translation { get; set; } = translation;
    public string Text { 
        get {
            var sb = new StringBuilder();
            foreach(var word in KaraokeWords)
                sb.Append(word.Text);
            return sb.ToString();
        }}
}

