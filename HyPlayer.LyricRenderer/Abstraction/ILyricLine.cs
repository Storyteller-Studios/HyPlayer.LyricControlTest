namespace HyPlayer.LyricRenderer.Abstraction
{
    public interface ILyricLine
    {
        public uint StartTime { get; set; }
        public uint EndTime { get; set; }
    }
}