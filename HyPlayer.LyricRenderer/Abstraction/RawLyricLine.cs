namespace HyPlayer.LyricRenderer.Abstraction;

public class RawLyricLine : ILyricLine
{
    public uint StartTime { get; set; }
    public uint EndTime { get; set; }
}