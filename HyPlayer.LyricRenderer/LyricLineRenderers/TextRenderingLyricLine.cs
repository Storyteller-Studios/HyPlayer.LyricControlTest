using Windows.Foundation;
using Windows.UI;
using HyPlayer.LyricRenderer.Abstraction.Render;
using Microsoft.Graphics.Canvas;
using System;
using System.Diagnostics;
using Microsoft.Graphics.Canvas.Text;
using Windows.UI.Text;
using Microsoft.Graphics.Canvas.Effects;

namespace HyPlayer.LyricRenderer.LyricLineRenderers;

public class TextRenderingLyricLine : RenderingLyricLine
{
    public string Text { get; set; }

    private CanvasTextFormat textFormat;
    private CanvasTextLayout textLayout;
    private CanvasTextLayout focusingTextLayout;

    public TextRenderingLyricLine()
    {
        textFormat = new CanvasTextFormat()
        {
            FontSize = 48,
            HorizontalAlignment = CanvasHorizontalAlignment.Left,
            VerticalAlignment = CanvasVerticalAlignment.Top,
            Options = CanvasDrawTextOptions.EnableColorFont,
            WordWrapping = CanvasWordWrapping.Wrap,
            Direction = CanvasTextDirection.LeftToRightThenTopToBottom,
            FontFamily = "Microsoft YaHei UI",
            FontWeight = FontWeights.Bold
        };

    }

    public override bool Render(CanvasDrawingSession session, LineRenderOffset offset, long currentLyricTime)
    {
        session.FillRectangle((float)offset.X, (float)offset.Y, (float)RenderingWidth, (float)RenderingHeight, _isFocusing ? Colors.Blue : Colors.Indigo);
        session.DrawTextLayout(textLayout, (float)offset.X, (float)offset.Y, Colors.Gray);
        session.DrawText(StartTime.ToString(), (float)offset.X, (float)offset.Y, Colors.Yellow);
        if (_isFocusing)
        {
            // 做一下扫词
            var currentProgress = (currentLyricTime - StartTime) * 1.0 / (EndTime - StartTime);
            if (currentProgress < 0) return true;
            var cl = new CanvasCommandList(session);
            using (CanvasDrawingSession clds = cl.CreateDrawingSession())
            {
                clds.DrawTextLayout(textLayout, 0,0, Colors.White);
            }
            var accentLyric = new CropEffect {
                Source = cl,
                SourceRectangle = new Rect(textLayout.LayoutBounds.Left, textLayout.LayoutBounds.Top, currentProgress * textLayout.LayoutBounds.Width, textLayout.LayoutBounds.Height),
            };
            session.DrawImage(accentLyric,(float)offset.X, (float)offset.Y);
        }
        return true;
    }

    private bool _isFocusing = false;

    public bool HiddenOnBlur = false;

    public override void OnKeyFrame(CanvasDrawingSession session, long time)
    {
        // skip
        _isFocusing = (time >= StartTime && time < EndTime);
        Hidden = false;
        if (HiddenOnBlur && !_isFocusing)
        {
            Hidden = true;
        }
    }

    public override void OnRenderSizeChanged(CanvasDrawingSession session, double width, double height)
    {
        if (HiddenOnBlur && !_isFocusing)
        {
            Hidden = true;
        }
        textLayout = new CanvasTextLayout(session, Text, textFormat, (float)width, (float)height);
        RenderingHeight = textLayout.LayoutBounds.Height + 10;
        RenderingWidth = textLayout.LayoutBounds.Width + 10;
    }
}