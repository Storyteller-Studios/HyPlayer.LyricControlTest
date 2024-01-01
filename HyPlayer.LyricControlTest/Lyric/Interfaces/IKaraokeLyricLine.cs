using System.Collections.Generic;

namespace HyPlayer.LyricControlTest.Lyric.Interfaces;

public interface IKaraokeLyricLine : ITextLyricLine
{
    List<KaraokeWordInfo> KaraokeWords { get; set; }    
}