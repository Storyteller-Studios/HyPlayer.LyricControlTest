using System.Collections.Generic;
using Microsoft.Graphics.Canvas;

namespace HyPlayer.LyricRenderer.Abstraction.Render;

public abstract class RenderingLyricLine
{
    public int Id { get; set; }

    public double RenderingHeight { get; set; }
    public double RenderingWidth { get; set; }

    public bool Hidden { get; set; }

    public List<long> KeyFrames { get; set; }

    public long StartTime { get; set; }
    public long EndTime { get; set; }

    public abstract void GoToReactionState(ReactionState state, long time);
    public abstract bool Render(CanvasDrawingSession session, LineRenderOffset offset, long currentLyricTime);
    public abstract void OnKeyFrame(CanvasDrawingSession session,long time);
    public abstract void OnRenderSizeChanged(CanvasDrawingSession session, double width, double height);
}

public enum ReactionState
{
    Leave,
    Enter,
    Press
}