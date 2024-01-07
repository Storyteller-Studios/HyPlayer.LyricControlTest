﻿using HyPlayer.LyricRenderer.Abstraction.Render;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Timers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using HyPlayer.LyricRenderer.LyricLineRenderers;
using Microsoft.Graphics.Canvas;
using System.Diagnostics;
using Windows.Media.Playback;
using Windows.Storage.Pickers;
using Windows.Media.Core;
using Windows.UI;
using HyPlayer.LyricRenderer.RollingCalculators;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace HyPlayer.LyricRenderer
{
    public sealed partial class LyricRenderView : UserControl
    {
        public List<RenderingLyricLine> RenderingLyricLines
        {
            get => _renderingLyricLines;
            set
            {
                _renderingLyricLines = value;
                OnLyricChanged(this, null);
            }
        }

        public long CurrentLyricTime
        {
            get => _currentLyricTime;
            set => _currentLyricTime = value;
        }

        public double LyricWidthRatio
        {
            get => _lyricWidthRatio;
            set => _lyricWidthRatio = value;
        }

        public double LyricPaddingTopRatio
        {
            get => _lyricPaddingTopRatio;
            set => _lyricPaddingTopRatio = value;
        }

        /// <summary>
        /// 行滚动的缓动函数, 返回值需为 0 - 1
        /// 参数1 为滑动开始时间
        /// 参数1 为当前理应进度
        /// 参数2 为间距
        /// </summary>
        public LineRollingCalculator LineRollingEaseCalculator
        {
            get => _lineRollingEaseCalculator;
            set => _lineRollingEaseCalculator = value;
        }
        


        private const double Epsilon = 0.001;

        private Dictionary<int, LineRenderOffset> _renderOffsets = new();
        private readonly Timer _secondTimer = new(500);
        private double _renderingWidth;
        private double _renderingHeight;

        public delegate void OnBeforeRenderDelegate(LyricRenderView view);
        public event OnBeforeRenderDelegate OnBeforeRender;

        public LyricRenderView()
        {
            this.InitializeComponent();
            _secondTimer.Elapsed += SecondTimerOnElapsed;
            _secondTimer.Start();
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
            if (Math.Abs(_sizeChangedWidth - _renderingWidth) > Epsilon ||
                Math.Abs(_sizeChangedHeight - _renderingHeight) > Epsilon)
            {
                _renderingHeight = _sizeChangedHeight;
                _renderingWidth = _sizeChangedWidth;
                _needRecalculateSize = true;
                _needRecalculate = true;
            }
        }

        private bool _initializing = true;

        private static void OnLyricChanged(DependencyObject obj, DependencyPropertyChangedEventArgs _)
        {
            if (obj is not LyricRenderView lrv) return;
            // Refresh _timeTickesToRerender
            lrv._initializing = true;
            lrv._renderingLyricLines = lrv.RenderingLyricLines;
            lrv._keyFrameRendered.Clear();
            lrv._targetingKeyFrames.Clear();
            lrv._renderOffsets.Clear();
            double TopLeftPos = lrv._sizeChangedHeight * lrv.LyricPaddingTopRatio;

            foreach (var renderingLyricLine in lrv.RenderingLyricLines)
            {
                // 初始化 Offset
                lrv._renderOffsets[renderingLyricLine.Id] = new LineRenderOffset();
                lrv._offsetBeforeRolling[renderingLyricLine.Id] = TopLeftPos;
                TopLeftPos += renderingLyricLine.RenderingHeight;
                // 获取 Keyframe
                lrv._keyFrameRendered[renderingLyricLine.StartTime] = false;
                lrv._keyFrameRendered[renderingLyricLine.EndTime] = false;
                if (!lrv._targetingKeyFrames.ContainsKey(renderingLyricLine.StartTime))
                    lrv._targetingKeyFrames[renderingLyricLine.StartTime] = new List<RenderingLyricLine>();
                if (!lrv._targetingKeyFrames.ContainsKey(renderingLyricLine.EndTime))
                    lrv._targetingKeyFrames[renderingLyricLine.EndTime] = new List<RenderingLyricLine>();
                lrv._targetingKeyFrames[renderingLyricLine.StartTime].Add(renderingLyricLine);
                lrv._targetingKeyFrames[renderingLyricLine.EndTime].Add(renderingLyricLine);
                if (renderingLyricLine.KeyFrames is not { Count: > 0 }) continue;
                foreach (var renderOptionsKey in renderingLyricLine.KeyFrames)
                {
                    if (!lrv._targetingKeyFrames.ContainsKey(renderOptionsKey))
                        lrv._targetingKeyFrames[renderOptionsKey] = new List<RenderingLyricLine>();
                    lrv._targetingKeyFrames[renderOptionsKey].Add(renderingLyricLine);
                    lrv._keyFrameRendered[renderOptionsKey] = false;
                }
            }

            // Calculate Init Size and Offset
            lrv._initializing = false;
            lrv._needRecalculateSize = true;
            lrv._needRecalculate = true;
        }

        private void RecalculateItemsSize(CanvasDrawingSession session)
        {
            var itemWidth = _renderingWidth * LyricWidthRatio;
            foreach (var renderingLyricLine in RenderingLyricLines)
            {
                renderingLyricLine.OnRenderSizeChanged(session, itemWidth, _renderingHeight);
            }
        }

        private readonly HashSet<RenderingLyricLine> _itemsToBeRender = new();
        private readonly Dictionary<long, bool> _keyFrameRendered = new();
        private readonly Dictionary<long, List<RenderingLyricLine>> _targetingKeyFrames = new();
        private readonly Dictionary<int, double> _offsetBeforeRolling = new();
        private long _lastKeyFrame = 0;

        private void RecalculateRenderOffset(CanvasDrawingSession session)
        {
            if (RenderingLyricLines is { Count: <= 0 }) return;
            var firstIndex =
                RenderingLyricLines.FindIndex(x =>
                    x.StartTime <= CurrentLyricTime && x.EndTime >= CurrentLyricTime);
            if (firstIndex < 0)
                firstIndex = RenderingLyricLines.FindIndex(x => x.StartTime >= CurrentLyricTime);
            if (firstIndex < 0) firstIndex = RenderingLyricLines.Count - 1;
            _itemsToBeRender.Clear();
            var theoryRenderStartPosition = LyricPaddingTopRatio * _renderingHeight;
            var renderedAfterStartPosition = theoryRenderStartPosition;
            var renderedBeforeStartPosition = theoryRenderStartPosition;

            var hiddenLinesCount = 0;
            for (var i = firstIndex; i < RenderingLyricLines.Count; i++)
            {
                var currentLine = RenderingLyricLines[i];
                if (currentLine.Hidden)
                {
                    hiddenLinesCount++;
                    _renderOffsets[currentLine.Id].Y = renderedAfterStartPosition;
                    continue;
                }

                if (renderedAfterStartPosition <= _renderingHeight) // 在可视区域, 需要缓动
                    if (_offsetBeforeRolling.ContainsKey(currentLine.Id) &&
                        Math.Abs(_offsetBeforeRolling[currentLine.Id] - renderedAfterStartPosition) > Epsilon)
                    {
                        renderedAfterStartPosition = LineRollingEaseCalculator.CalculateCurrentY(
                            _offsetBeforeRolling[currentLine.Id], renderedAfterStartPosition, i - firstIndex,
                            _lastKeyFrame,
                            CurrentLyricTime);
                        _needRecalculate = true; // 滚动中, 下一帧继续渲染
                    }

                _renderOffsets[currentLine.Id].Y = renderedAfterStartPosition;
                if (renderedAfterStartPosition <= _renderingHeight) _itemsToBeRender.Add(currentLine);
                renderedAfterStartPosition += currentLine.RenderingHeight;
            }

            // 算之前的
            for (var i = firstIndex - 1; i >= 0; i--)
            {
                var currentLine = RenderingLyricLines[i];
                if (currentLine.Hidden) continue;
                // 行前也要算一下
                renderedBeforeStartPosition -= currentLine.RenderingHeight;

                if (renderedBeforeStartPosition + currentLine.RenderingHeight > 0) // 可见区域, 需要判断缓动
                {
                    if (_offsetBeforeRolling.ContainsKey(currentLine.Id) &&
                        Math.Abs(_offsetBeforeRolling[currentLine.Id] - renderedBeforeStartPosition) > Epsilon)
                    {
                        renderedBeforeStartPosition = LineRollingEaseCalculator.CalculateCurrentY(
                            _offsetBeforeRolling[currentLine.Id], renderedBeforeStartPosition, i - firstIndex,
                            _lastKeyFrame,
                            CurrentLyricTime);
                        _needRecalculate = true; // 滚动中, 下一帧继续渲染
                    }
                }

                _renderOffsets[currentLine.Id].Y = renderedBeforeStartPosition;
                _renderOffsets[currentLine.Id].X = 0;
                if (renderedBeforeStartPosition + currentLine.RenderingHeight >= 0)
                    _itemsToBeRender.Add(currentLine);
                if (i > 0)
                    if (renderedBeforeStartPosition + RenderingLyricLines[i - 1].RenderingHeight < 0)
                        break;
            }
        }

        // DEBUG
        private long lastRenderedCount = 0;
        private long curRenderedCount = 0;
        private int lastUpdateSecond = 0;

        private void LyricView_Draw(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender,
            Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args)
        {
            if (_initializing) return;
            OnBeforeRender?.Invoke(this);
            foreach (var key in _keyFrameRendered.Keys.ToArray())
            {
                if (key >= CurrentLyricTime || _keyFrameRendered[key]) continue;
                // 该 KeyFrame 尚未渲染
                _keyFrameRendered[key] = true;
                _lastKeyFrame = key;
                // 视图快照
                //if (!_needRecalculate)
                foreach (var (i, value) in _renderOffsets)
                {
                    _offsetBeforeRolling[i] = value.Y;
                }

                foreach (var renderingLyricLine in _targetingKeyFrames[key])
                {
                    renderingLyricLine.OnKeyFrame(args.DrawingSession, key);
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

            foreach (var renderingLyricLine in _itemsToBeRender)
            {
                renderingLyricLine.Render(args.DrawingSession, _renderOffsets[renderingLyricLine.Id], CurrentLyricTime);
            }

            var curSec = ((int)args.Timing.TotalTime.TotalSeconds);
            if (curSec != lastUpdateSecond)
            {
                lastUpdateSecond = curSec;
                lastRenderedCount = curRenderedCount;
                curRenderedCount = args.Timing.UpdateCount;
            }
            /*
            args.DrawingSession.DrawText("FPS: " + (curRenderedCount - lastRenderedCount).ToString(), 0, 70,
                Windows.UI.Colors.White);
            args.DrawingSession.DrawText("Rendering Count: " + (_itemsToBeRender.Count).ToString(), 0, 0,
                Windows.UI.Colors.White);
            args.DrawingSession.DrawText("CurTime: " + (CurrentLyricTime).ToString(), 0, 20, Windows.UI.Colors.White);
            var firstIndex =
                RenderingLyricLines.FindIndex(x => x.StartTime <= CurrentLyricTime && x.EndTime >= CurrentLyricTime);
            args.DrawingSession.DrawText("CurIndex: " + (firstIndex).ToString(), 0, 50, Windows.UI.Colors.White);
            */
            args.DrawingSession.Dispose();
        }


        private double _sizeChangedWidth;
        private double _sizeChangedHeight;
        private List<RenderingLyricLine> _renderingLyricLines = new();
        private long _currentLyricTime;
        private double _lyricWidthRatio;
        private double _lyricPaddingTopRatio;
        private LineRollingCalculator _lineRollingEaseCalculator;

        private void LyricView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _sizeChangedWidth = e.NewSize.Width;
            _sizeChangedHeight = e.NewSize.Height;
        }
        
    }
}