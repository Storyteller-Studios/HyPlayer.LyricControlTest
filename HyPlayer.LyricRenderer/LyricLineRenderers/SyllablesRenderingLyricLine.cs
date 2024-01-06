using HyPlayer.LyricRenderer.Abstraction.Render;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Text;
using HyPlayer.LyricRenderer.Abstraction;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using System.Diagnostics;

namespace HyPlayer.LyricRenderer.LyricLineRenderers
{
    public class RenderingSyllable
    {
        public string Syllable { get; set; }
        public long StartTime { get; set; }
        public long EndTime { get; set; }
    }

    public class SyllablesRenderingLyricLine : RenderingLyricLine
    {
        private CanvasTextFormat textFormat;
        private CanvasTextLayout textLayout;
        private bool _isFocusing;
        private float _canvasWidth;
        private float _canvasHeight;
        public List<RenderingSyllable> Syllables { get; set; } = [];

        public SyllablesRenderingLyricLine()
        {
            textFormat = new CanvasTextFormat()
            {
                FontSize = HiddenOnBlur ? 24 : 48,
                HorizontalAlignment = CanvasHorizontalAlignment.Left,
                VerticalAlignment = CanvasVerticalAlignment.Top,
                WordWrapping = CanvasWordWrapping.Wrap,
                Direction = CanvasTextDirection.LeftToRightThenTopToBottom,
                FontFamily = "Microsoft YaHei UI",
                FontWeight = HiddenOnBlur ? FontWeights.Normal : FontWeights.Bold
            };
            _text = string.Join("", Syllables.Select(t => t.Syllable));
        }

        public override void GoToReactionState(ReactionState state, long time)
        {
            //throw new NotImplementedException();
        }

        public override bool Render(CanvasDrawingSession session, LineRenderOffset offset, long currentLyricTime)
        {
            var actualTop = (float)offset.Y + (HiddenOnBlur ? 10 : 30);
            session.DrawTextLayout(textLayout, (float)offset.X, actualTop, Colors.Gray);
            /*
            session.DrawText(StartTime.ToString(), (float)offset.X, (float)offset.Y, Colors.Yellow);
            session.DrawText(offset.Y.ToString(CultureInfo.InvariantCulture), (float)offset.X, (float)offset.Y + 20,
                Colors.Yellow);
            */
            if (_isFocusing)
            {
                var highlightGeometry = CreateHighlightGeometry(currentLyricTime, textLayout, session);
                var textGeometry = CanvasGeometry.CreateText(textLayout);
                var highlightTextGeometry = highlightGeometry.CombineWith(textGeometry, Matrix3x2.Identity, CanvasGeometryCombine.Intersect);
                session.FillGeometry(highlightTextGeometry, (float)offset.X, actualTop, Colors.Yellow);
            }

            return true;
        }

        private CanvasGeometry CreateHighlightGeometry(long currentTime, CanvasTextLayout textLayout,
            ICanvasResourceCreator drawingSession)
        {
            var time = TimeSpan.Zero;
            var currentLyric = Syllables.Last();            
            var geos = new HashSet<CanvasGeometry>();
            var index = Syllables.FindLastIndex(t => t.StartTime <= currentTime);
            if (index == -1) index = 0;
            currentLyric = Syllables[index];
            var letterPosition = Syllables.GetRange(0, index).Sum(p => p.Syllable.Length);

            // 获取高亮的字符区域集合
            var regions = textLayout.GetCharacterRegions(0, letterPosition);
            foreach (var region in regions)
            {
                // 对每个字符创建矩形, 并加入到 geos
                geos.Add(CanvasGeometry.CreateRectangle(drawingSession, region.LayoutBounds));
            }

            // 获取当前字符的 Bound
            var currentRegions = textLayout.GetCharacterRegions(letterPosition, currentLyric.Syllable.Length);
            if (currentRegions is { Length: > 0 })
            {
                // 加个保险措施
                // 计算当前字符的进度
                var currentPercentage = (currentTime - currentLyric.StartTime) * 1.0 /
                                        (currentLyric.EndTime - currentLyric.StartTime);
                // 创建矩形
                var lastRect = CanvasGeometry.CreateRectangle(
                    drawingSession, (float)currentRegions[0].LayoutBounds.Left,
                    (float)currentRegions[0].LayoutBounds.Top,
                    (float)(currentRegions.Sum(t => t.LayoutBounds.Width) * currentPercentage),
                    (float)currentRegions.Sum(t => t.LayoutBounds.Height));
                geos.Add(lastRect);
            }

            // 拼合所有矩形
            return CanvasGeometry.CreateGroup(drawingSession, geos.ToArray());
        }


        public override void OnKeyFrame(CanvasDrawingSession session, long time)
        {
            // skip
            _isFocusing = (time >= StartTime && time < EndTime);
            Hidden = false;
            if (HiddenOnBlur && !_isFocusing)
            {
                Hidden = true;
            }
            _text = string.Join("", Syllables.Select(t => t.Syllable));
            textFormat = new CanvasTextFormat()
            {
                FontSize = HiddenOnBlur ? 24 : 48,
                HorizontalAlignment = CanvasHorizontalAlignment.Left,
                VerticalAlignment = CanvasVerticalAlignment.Top,
                WordWrapping = CanvasWordWrapping.Wrap,
                Direction = CanvasTextDirection.LeftToRightThenTopToBottom,
                FontFamily = "Microsoft YaHei UI",
                FontWeight = HiddenOnBlur ? FontWeights.Normal : FontWeights.Bold
            };
            if (_canvasWidth == 0.0f) return;
            textLayout = new CanvasTextLayout(session, _text, textFormat, _canvasWidth, _canvasHeight);
            RenderingHeight = textLayout.LayoutBounds.Height + (HiddenOnBlur ? 10 : 30);
            RenderingWidth = textLayout.LayoutBounds.Width + 10;
        }

        public bool HiddenOnBlur { get; set; }
        private string _text;

        public override void OnRenderSizeChanged(CanvasDrawingSession session, double width, double height)
        {
            if (HiddenOnBlur && !_isFocusing)
            {
                Hidden = true;
            }

            _canvasWidth = (float)width;
            _canvasHeight = (float)height;
            _text = string.Join("", Syllables.Select(t => t.Syllable));
            textLayout = new CanvasTextLayout(session, _text, textFormat, (float)width, (float)height);
            RenderingHeight = textLayout.LayoutBounds.Height + (HiddenOnBlur ? 10 : 30);
            RenderingWidth = textLayout.LayoutBounds.Width + 10;
        }
    }
}