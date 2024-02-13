﻿using HyPlayer.LyricRenderer.Abstraction.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Graphics.Canvas;
using System.Diagnostics;
using Windows.UI.Xaml.Input;
using HyPlayer.LyricRenderer.Abstraction;
using HyPlayer.LyricRenderer.LyricLineRenderers;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace HyPlayer.LyricRenderer
{
    public sealed partial class LyricRenderView : UserControl
    {
        public RenderContext Context { get; } = new();

        private const double Epsilon = 0.001;

        private readonly Timer _secondTimer = new(500);

        public delegate void BeforeRenderDelegate(LyricRenderView view);

        public event BeforeRenderDelegate OnBeforeRender;

        public delegate void RequestSeekDelegate(long time);

        public event RequestSeekDelegate OnRequestSeek;

        public LyricRenderView()
        {
            InitializeComponent();
            _secondTimer.Elapsed += SecondTimerOnElapsed;
            _secondTimer.Start();
        }

        private bool _isTypographyChanged = true;

        public void ChangeRenderColor(Color idleColor, Color focusingColor)
        {
            Context.PreferTypography.IdleColor = idleColor;
            Context.PreferTypography.FocusingColor = focusingColor;
            _isTypographyChanged = true;
        }

        public void ChangeRenderFontSize(double lyricSize, double translationSize, double transliterationSize)
        {
            Context.PreferTypography.LyricFontSize = lyricSize;
            Context.PreferTypography.TranslationFontSize = translationSize;
            Context.PreferTypography.TransliterationFontSize = transliterationSize;
            _isTypographyChanged = true;
        }

        public void ChangeAlignment(TextAlignment alignment)
        {
            Context.PreferTypography.Alignment = alignment;
            _isTypographyChanged = true;
        }

        public void ChangeBeatPerMinute(double beatPerMinute)
        {
            Context.BeatPerMinute = beatPerMinute;
            _isTypographyChanged = true;
        }

        public void ReflowTime(long time)
        {
            var keys = _keyFrameRendered.Keys.ToArray();
            foreach (var key in keys)
            {
                if (key >= time) _keyFrameRendered[key] = false;
            }

            _needRecalculate = true;
        }

        private bool _needRecalculate = false;
        private bool _needRecalculateSize = false;

        private void SecondTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (Math.Abs(_sizeChangedWidth - Context.ViewWidth) > Epsilon ||
                Math.Abs(_sizeChangedHeight - Context.ViewHeight) > Epsilon)
            {
                Context.ViewWidth = _sizeChangedWidth;
                Context.ViewHeight = _sizeChangedHeight;
                _needRecalculateSize = true;
                _needRecalculate = true;
            }
        }

        private bool _initializing = true;

        public void SetLyricLines(List<RenderingLyricLine> lines)
        {
            _initializing = true;
            Context.LyricLines.Clear();
            Context.LyricLines.AddRange(lines);
            _keyFrameRendered.Clear();
            // 将 Id 换为 Index, 方便后续读取
            for (var i = 0; i < Context.LyricLines.Count; i++)
            {
                Context.LyricLines[i].Id = i;
            }

            _keyFrameRendered.Clear();
            _targetingKeyFrames.Clear();
            Context.RenderOffsets.Clear();
            _keyFrameRendered[0] = false; // 将 0 时刻添加到 KeyFrame, 以便初始化时渲染

            // 初始化位置
            double topleftPosition = Context.ViewHeight * Context.LyricPaddingTopRatio;

            foreach (var renderingLyricLine in Context.LyricLines)
            {
                Context.RenderOffsets[renderingLyricLine.Id] = new LineRenderOffset
                {
                    Y = topleftPosition
                };
                Context.SnapshotRenderOffsets[renderingLyricLine.Id] = new LineRenderOffset();
                topleftPosition += renderingLyricLine.RenderingHeight;
                // 获取 Keyframe
                _keyFrameRendered[renderingLyricLine.StartTime] = false;
                _keyFrameRendered[renderingLyricLine.EndTime] = false;
                // 设置对照表
                if (!_targetingKeyFrames.ContainsKey(renderingLyricLine.StartTime))
                    _targetingKeyFrames[renderingLyricLine.StartTime] = new List<RenderingLyricLine>();
                if (!_targetingKeyFrames.ContainsKey(renderingLyricLine.EndTime))
                    _targetingKeyFrames[renderingLyricLine.EndTime] = new List<RenderingLyricLine>();
                _targetingKeyFrames[renderingLyricLine.StartTime].Add(renderingLyricLine);
                _targetingKeyFrames[renderingLyricLine.EndTime].Add(renderingLyricLine);
                if (renderingLyricLine.KeyFrames is not { Count: > 0 }) continue;

                // 添加自定义 KeyFrame
                foreach (var renderOptionsKey in renderingLyricLine.KeyFrames)
                {
                    if (!_targetingKeyFrames.ContainsKey(renderOptionsKey))
                        _targetingKeyFrames[renderOptionsKey] = new List<RenderingLyricLine>();
                    _targetingKeyFrames[renderOptionsKey].Add(renderingLyricLine);
                    _keyFrameRendered[renderOptionsKey] = false;
                }
            }

            _initializing = false;
        }

        private void RecalculateItemsSize(CanvasDrawingSession session)
        {
            Context.ItemWidth = Context.ViewWidth * Context.LyricWidthRatio;
            foreach (var renderingLyricLine in Context.LyricLines)
            {
                renderingLyricLine.OnRenderSizeChanged(session, Context);
            }
        }

        private readonly Dictionary<long, bool> _keyFrameRendered = new();
        private readonly Dictionary<long, List<RenderingLyricLine>> _targetingKeyFrames = new();

        private void RecalculateRenderOffset(CanvasDrawingSession session)
        {
            if (Context.LyricLines is { Count: <= 0 }) return;
            Context.CurrentLyricLineIndex =
                Context.LyricLines.FindIndex(x =>
                    x.StartTime <= Context.CurrentLyricTime && x.EndTime >= Context.CurrentLyricTime);
            if (Context.CurrentLyricLineIndex < 0)
                Context.CurrentLyricLineIndex = Context.LyricLines.FindIndex(x => x.StartTime >= Context.CurrentLyricTime);
            if (Context.CurrentLyricLineIndex < 0) Context.CurrentLyricLineIndex = Context.LyricLines.Count - 1;
            Context.CurrentLyricLine = Context.LyricLines[Context.CurrentLyricLineIndex];
            Context.RenderingLyricLines.Clear();
            var theoryRenderStartPosition = Context.LyricPaddingTopRatio * Context.ViewHeight + Context.ScrollingDelta;
            var renderedAfterStartPosition = theoryRenderStartPosition;
            var renderedBeforeStartPosition = theoryRenderStartPosition;

            for (var i = Context.CurrentLyricLineIndex; i < Context.LyricLines.Count; i++)
            {
                var currentLine = Context.LyricLines[i];
                if (currentLine.Hidden)
                {
                    Context.RenderOffsets[currentLine.Id].Y = renderedAfterStartPosition;
                    continue;
                }

                if (renderedAfterStartPosition <= Context.ViewHeight) // 在可视区域, 需要缓动
                    if (Context.SnapshotRenderOffsets.ContainsKey(currentLine.Id) &&
                        Math.Abs(Context.SnapshotRenderOffsets[currentLine.Id].Y - renderedAfterStartPosition) >
                        Epsilon)
                    {
                        renderedAfterStartPosition = Context.LineRollingEaseCalculator.CalculateCurrentY(
                            Context.SnapshotRenderOffsets[currentLine.Id].Y, renderedAfterStartPosition,
                            currentLine, Context);
                        _needRecalculate = true; // 滚动中, 下一帧继续渲染
                    }

                Context.RenderOffsets[currentLine.Id].Y = renderedAfterStartPosition;
                if (renderedAfterStartPosition <= Context.ViewHeight) Context.RenderingLyricLines.Add(currentLine);
                renderedAfterStartPosition += currentLine.RenderingHeight;
            }

            // 算之前的
            for (var i = Context.CurrentLyricLineIndex - 1; i >= 0; i--)
            {
                var currentLine = Context.LyricLines[i];
                if (currentLine.Hidden) continue;
                // 行前也要算一下
                renderedBeforeStartPosition -= currentLine.RenderingHeight;

                if (renderedBeforeStartPosition + currentLine.RenderingHeight > 0) // 可见区域, 需要判断缓动
                {
                    if (Context.SnapshotRenderOffsets.ContainsKey(currentLine.Id) &&
                        Math.Abs(Context.SnapshotRenderOffsets[currentLine.Id].Y - renderedBeforeStartPosition) >
                        Epsilon)
                    {
                        renderedBeforeStartPosition = Context.LineRollingEaseCalculator.CalculateCurrentY(
                            Context.SnapshotRenderOffsets[currentLine.Id].Y, renderedBeforeStartPosition,
                            currentLine, Context);

                        _needRecalculate = true; // 滚动中, 下一帧继续渲染
                    }
                }

                Context.RenderOffsets[currentLine.Id].Y = renderedBeforeStartPosition;
                Context.RenderOffsets[currentLine.Id].X = 4;
                if (renderedBeforeStartPosition + currentLine.RenderingHeight >= 0)
                    Context.RenderingLyricLines.Add(currentLine);
                if (i <= 0) continue;
                if (renderedBeforeStartPosition + Context.LyricLines[i - 1].RenderingHeight < 0)
                    break;
            }
        }


        private void LyricView_Draw(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender,
            Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args)
        {
            Context.RenderTick = args.Timing.TotalTime.Ticks;
            if (_initializing) return;
            OnBeforeRender?.Invoke(this);
            // 鼠标滚轮时间 5 s 清零
            if (Context.ScrollingDelta != 0 && Context.RenderTick - _lastWheelTime > 50000000)
            {
                // 缓动来一下吧
                // 0.5 秒缓动到 0
                Context.IsScrolling = false;
                var progress = Math.Clamp((Context.RenderTick - _lastWheelTime - 50000000) / 5000000.0, 0, 1);
                Context.ScrollingDelta = (int)(Context.ScrollingDelta * (1 - progress));
                if (Math.Abs(progress - 1) < Epsilon)
                {
                    _lastWheelTime = 0;
                    Context.ScrollingDelta = 0;                    
                }

                _needRecalculate = true;
            }

            if (_isTypographyChanged)
            {
                _isTypographyChanged = false;
                foreach (var renderingLyricLine in Context.LyricLines)
                {
                    renderingLyricLine.OnTypographyChanged(args.DrawingSession, Context);
                }
            }

            foreach (var key in _keyFrameRendered.Keys.ToArray())
            {
                if (_keyFrameRendered[key] == true) continue;
                if (key >= Context.CurrentLyricTime && key != 0) continue;
                // 该 KeyFrame 尚未渲染
                _keyFrameRendered[key] = true;
                //if (!_needRecalculate)
                Context.CurrentKeyframe = key;
                // 视图快照
                //if (!_needRecalculate)
                foreach (var (i, value) in Context.RenderOffsets)
                {
                    Context.SnapshotRenderOffsets[i].Y = value.Y;
                }

                
                // 0 时刻渲染所有, 也就是初始化
                var targets = key == 0 ? Context.LyricLines : _targetingKeyFrames[key];

                foreach (var renderingLyricLine in targets)
                {
                    renderingLyricLine.OnKeyFrame(args.DrawingSession, Context);
                }

                _needRecalculate = true;
                _needRecalculateSize = true;
            }

            if (_needRecalculateSize)
            {
                _needRecalculateSize = false;
                RecalculateItemsSize(args.DrawingSession);
            }

            if (_needRecalculate)
            {
                _needRecalculate = false;
                RecalculateRenderOffset(args.DrawingSession);
            }

            foreach (var renderingLyricLine in Context.RenderingLyricLines)
            {
                var doRender = renderingLyricLine.Render(args.DrawingSession, Context.RenderOffsets[renderingLyricLine.Id], Context);
                if (doRender == false) break;
            }

            if (Context.Debug)
            {
                args.DrawingSession.DrawText($"绘制时间: {args.Timing.ElapsedTime}", 0,0, Colors.Yellow);
                args.DrawingSession.DrawText($"滚动偏移: {Context.ScrollingDelta}", 0, 15, Colors.Yellow);
                args.DrawingSession.DrawText($"歌词时间: {Context.CurrentLyricTime}", 0, 30, Colors.Yellow);
                args.DrawingSession.DrawText($"歌词时间: {Context.CurrentLyricTime}", 0, 30, Colors.Yellow);
                // 绘制绘制边框
                args.DrawingSession.DrawRectangle(0,0,(float)Context.ViewWidth, (float)Context.ViewHeight, Colors.Red, 5);
            }
            args.DrawingSession.Dispose();
        }


        private double _sizeChangedWidth;
        private double _sizeChangedHeight;


        private void LyricView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _sizeChangedWidth = e.NewSize.Width;
            _sizeChangedHeight = e.NewSize.Height;
        }

        private long _lastWheelTime = 0;

        private void LyricView_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            Context.ScrollingDelta += e.GetCurrentPoint(this).Properties.MouseWheelDelta;
            Context.IsScrolling = true;
            _lastWheelTime = Context.RenderTick;
            _needRecalculate = true;
        }


        private void LyricView_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // 指针事件
            // 获取在指针范围的行
            var focusingLine = -1;
            foreach (var renderOffsetsKey in Context.RenderOffsets.Keys)
            {
                if (Context.LyricLines[renderOffsetsKey].Hidden)
                    continue;
                if (Context.RenderOffsets[renderOffsetsKey].Y <= e.GetCurrentPoint(this).Position.Y &&
                    Context.RenderOffsets[renderOffsetsKey].Y + Context.LyricLines[renderOffsetsKey].RenderingHeight >=
                    e.GetCurrentPoint(this).Position.Y)
                {
                    if (Context.PointerFocusingIndex == renderOffsetsKey) return;
                    Context.LyricLines[renderOffsetsKey].GoToReactionState(ReactionState.Enter, Context);
                    focusingLine = renderOffsetsKey;
                    break;
                }
            }

            if (Context.PointerFocusingIndex != focusingLine)
            {
                if (Context.PointerFocusingIndex != -1 && Context.LyricLines.Count > (Context.PointerFocusingIndex))
                    Context.LyricLines[Context.PointerFocusingIndex].GoToReactionState(ReactionState.Leave, Context);
                Context.PointerFocusingIndex = focusingLine;
            }
        }

        private void LyricRenderView_OnUnloaded(object sender, RoutedEventArgs e)
        {
            _secondTimer.Stop();
            _secondTimer.Dispose();
            LyricView.RemoveFromVisualTree();
            LyricView = null;
        }

        private void LyricView_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (Context.PointerFocusingIndex != -1 && Context.LyricLines.Count > Context.PointerFocusingIndex)
                Context.LyricLines[Context.PointerFocusingIndex].GoToReactionState(ReactionState.Leave, Context);
            Context.PointerFocusingIndex = -1;
        }


        private void LyricView_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            foreach (var renderOffsetsKey in Context.RenderOffsets.Keys)
            {
                if (Context.LyricLines[renderOffsetsKey].Hidden)
                    continue;
                if (Context.RenderOffsets[renderOffsetsKey].Y <= e.GetCurrentPoint(this).Position.Y &&
                    Context.RenderOffsets[renderOffsetsKey].Y + Context.LyricLines[renderOffsetsKey].RenderingHeight >=
                    e.GetCurrentPoint(this).Position.Y)
                {
                    Context.LyricLines[renderOffsetsKey].GoToReactionState(ReactionState.Press, Context);
                    OnRequestSeek?.Invoke(Context.LyricLines[renderOffsetsKey].StartTime);
                    break;
                }
            }
        }
    }
}