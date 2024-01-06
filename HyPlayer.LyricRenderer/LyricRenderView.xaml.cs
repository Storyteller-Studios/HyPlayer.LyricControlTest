using HyPlayer.LyricRenderer.Abstraction.Render;
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
        MediaPlayer _player = new MediaPlayer();

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
            get => (long)_player.PlaybackSession.Position.TotalMilliseconds;
            set => _currentLyricTime = value;
        }

        public double ItemSpacing
        {
            get => _itemSpacing;
            set => _itemSpacing = value;
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


        public long LineRollingDuration
        {
            get => _lineRollingDuration;
            set => _lineRollingDuration = value;
        }


        private const double Epsilon = 0.001;

        private Dictionary<int, LineRenderOffset> _renderOffsets = new();
        private readonly Timer _secondTimer = new(500);
        private double _renderingWidth;
        private double _renderingHeight;


        public LyricRenderView()
        {
            this.InitializeComponent();
            _secondTimer.Elapsed += SecondTimerOnElapsed;
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
                TopLeftPos += renderingLyricLine.RenderingHeight + lrv.ItemSpacing;
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
                firstIndex = RenderingLyricLines.FindLastIndex(x => x.StartTime <= CurrentLyricTime);
            if (firstIndex < 0) throw new Exception("请保证第一行歌词时间为 0");
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
                renderedAfterStartPosition += currentLine.RenderingHeight + ItemSpacing;
                if (renderedAfterStartPosition <= _renderingHeight) _itemsToBeRender.Add(currentLine);
            }

            // 算之前的
            for (var i = firstIndex - 1; i >= 0; i--)
            {
                var currentLine = RenderingLyricLines[i];
                if (currentLine.Hidden) continue;
                // 行前也要算一下
                renderedBeforeStartPosition -= currentLine.RenderingHeight + ItemSpacing;

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

            foreach (var key in _keyFrameRendered.Keys.ToArray())
            {
                if (key >= CurrentLyricTime || _keyFrameRendered[key]) continue;
                // 该 KeyFrame 尚未渲染
                _keyFrameRendered[key] = true;
                _lastKeyFrame = key;
                // 视图快照
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

            args.DrawingSession.DrawText("FPS: " + (curRenderedCount - lastRenderedCount).ToString(), 0, 70,
                Windows.UI.Colors.White);
            args.DrawingSession.DrawText("Rendering Count: " + (_itemsToBeRender.Count).ToString(), 0, 0,
                Windows.UI.Colors.White);
            args.DrawingSession.DrawText("CurTime: " + (CurrentLyricTime).ToString(), 0, 20, Windows.UI.Colors.White);
            var firstIndex =
                RenderingLyricLines.FindIndex(x => x.StartTime <= CurrentLyricTime && x.EndTime >= CurrentLyricTime);
            args.DrawingSession.DrawText("CurIndex: " + (firstIndex).ToString(), 0, 50, Windows.UI.Colors.White);

            args.DrawingSession.Dispose();
        }


        private double _sizeChangedWidth;
        private double _sizeChangedHeight;
        private List<RenderingLyricLine> _renderingLyricLines;
        private long _currentLyricTime;
        private double _itemSpacing;
        private double _lyricWidthRatio;
        private double _lyricPaddingTopRatio;
        private LineRollingCalculator _lineRollingEaseCalculator;
        private long _lineRollingDuration;

        private void LyricView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _sizeChangedWidth = e.NewSize.Width;
            _sizeChangedHeight = e.NewSize.Height;
        }

        // Fake Timer Tick every 100 ms
        protected override async void OnTapped(TappedRoutedEventArgs e)

        {
            FileOpenPicker fop = new FileOpenPicker();
            fop.FileTypeFilter.Add(".mp3");
            var file = await fop.PickSingleFileAsync();
            _player.Source = MediaSource.CreateFromStorageFile(file);
            _player.Play();
            _secondTimer.Start();
            int i = 0;
            RenderingLyricLines = new()
            {
                new TextRenderingLyricLine()
                    { StartTime = 0, EndTime = 8830, Id = i++, KeyFrames = [0, 8830], Text = "" },
                new TextRenderingLyricLine()
                {
                    StartTime = 8841, EndTime = 11787, Id = i++, KeyFrames = [8841, 11787],
                    Text = "Some deserts on this planet"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 11787, EndTime = 14162, Id = i++, KeyFrames = [11787, 14162], Text = "Were oceans once"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 15797, EndTime = 18868, Id = i++, KeyFrames = [15797, 18868],
                    Text = "Somewhere shrouded by the night"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 18868, EndTime = 21922, Id = i++, KeyFrames = [18868, 21922],
                    Text = "The sun will shine"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 22800, EndTime = 25875, Id = i++, KeyFrames = [22800, 25875],
                    Text = "Sometimes I see a dying bird"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 25875, EndTime = 28633, Id = i++, KeyFrames = [25875, 28633],
                    Text = "Fall to the ground"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 29402, EndTime = 35225, Id = i++, KeyFrames = [29402, 35225],
                    Text = "But it used to fly so high"
                },
                new TextRenderingLyricLine()
                    { StartTime = 35225, EndTime = 35658, Id = i++, KeyFrames = [35225, 35658], Text = "" },
                new TextRenderingLyricLine()
                {
                    StartTime = 35658, EndTime = 40155, Id = i++, KeyFrames = [35658, 40155],
                    Text = "I thought I were no more than a bystander"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 40155, EndTime = 42586, Id = i++, KeyFrames = [40155, 42586],
                    Text = "Till I felt a touch so real"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 42586, EndTime = 47300, Id = i++, KeyFrames = [42586, 47300],
                    Text = "I will no longer be a transient"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 47300, EndTime = 49720, Id = i++, KeyFrames = [47300, 49720],
                    Text = "When I see smiles with tears"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 49720, EndTime = 54007, Id = i++, KeyFrames = [49720, 54007],
                    Text = "If I had never known the sore of farewell"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 54007, EndTime = 57586, Id = i++, KeyFrames = [54007, 57586],
                    Text = "And the pain of sacrifice"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 57586, EndTime = 63505, Id = i++, KeyFrames = [57586, 63505],
                    Text = "What else should I engrave on my mind?"
                },
                new TextRenderingLyricLine()
                    { StartTime = 63505, EndTime = 65144, Id = i++, KeyFrames = [63505, 65144], Text = "" },
                new TextRenderingLyricLine()
                {
                    StartTime = 65144, EndTime = 68317, Id = i++, KeyFrames = [65144, 68317],
                    Text = "Frozen into icy rocks"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 68317, EndTime = 73096, Id = i++, KeyFrames = [68317, 70455],
                    Text = "That's how it starts"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 70030, EndTime = 73096, Id = i++, KeyFrames = [70030, 73096],
                    Text = "That's how it starts", HiddenOnBlur = true
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 72216, EndTime = 75317, Id = i++, KeyFrames = [72216, 75317],
                    Text = "Crumbled like the sands of time"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 75317, EndTime = 81284, Id = i++, KeyFrames = [75317, 78484],
                    Text = "That's how it ends"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 77189, EndTime = 81284, Id = i++, KeyFrames = [77189, 81284],
                    Text = "That's how it ends", HiddenOnBlur = true
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 79389, EndTime = 82564, Id = i++, KeyFrames = [79389, 82564],
                    Text = "Every page of tragedy"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 82564, EndTime = 86713, Id = i++, KeyFrames = [82564, 84854], Text = "Is thrown away"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 83722, EndTime = 86713, Id = i++, KeyFrames = [83722, 86713], Text = "Is thrown away",
                    HiddenOnBlur = true
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 86713, EndTime = 91633, Id = i++, KeyFrames = [86713, 91633],
                    Text = "Burned out in the flame"
                },
                new TextRenderingLyricLine()
                    { StartTime = 91633, EndTime = 92055, Id = i++, KeyFrames = [91633, 92055], Text = "" },
                new TextRenderingLyricLine()
                {
                    StartTime = 92055, EndTime = 96664, Id = i++, KeyFrames = [92055, 96664],
                    Text = "A shoulder for the past, let out the cries"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 96664, EndTime = 99183, Id = i++, KeyFrames = [96664, 99183],
                    Text = "Imprisoned for so long"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 99183, EndTime = 103678, Id = i++, KeyFrames = [99183, 103678],
                    Text = "A pair of wings for me at this moment"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 103678, EndTime = 106219, Id = i++, KeyFrames = [103678, 106219],
                    Text = "To soar above this world"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 106219, EndTime = 110494, Id = i++, KeyFrames = [106219, 110494],
                    Text = "Turn into a shooting star that briefly shines"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 106231, EndTime = 110494, Id = i++, KeyFrames = [106231, 109319], Text = "Ohhhhh",
                    HiddenOnBlur = true
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 110950, EndTime = 113686, Id = i++, KeyFrames = [110950, 113686],
                    Text = "But warms up every heart"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 114342, EndTime = 120598, Id = i++, KeyFrames = [114342, 120598],
                    Text = "May all the beauty be blessed"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 115006, EndTime = 120598, Id = i++, KeyFrames = [115006, 115887], Text = "May all",
                    HiddenOnBlur = true
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 121166, EndTime = 128509, Id = i++, KeyFrames = [121166, 128509],
                    Text = "May all the beauty be blessed"
                },
                new TextRenderingLyricLine()
                    { StartTime = 128509, EndTime = 129505, Id = i++, KeyFrames = [128509, 129505], Text = "" },
                new TextRenderingLyricLine()
                {
                    StartTime = 129505, EndTime = 134142, Id = i++, KeyFrames = [129505, 134142],
                    Text = "I will never go"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 136652, EndTime = 141167, Id = i++, KeyFrames = [136652, 141167],
                    Text = "There's a way back home"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 142811, EndTime = 151482, Id = i++, KeyFrames = [142811, 151482],
                    Text = "Brighter than tomorrow and yesterday"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 149461, EndTime = 155188, Id = i++, KeyFrames = [149461, 155188],
                    Text = "May all the beauty be blessed"
                },
                new TextRenderingLyricLine()
                    { StartTime = 155188, EndTime = 162640, Id = i++, KeyFrames = [155188, 162640], Text = "" },
                new TextRenderingLyricLine()
                {
                    StartTime = 162640, EndTime = 167170, Id = i++, KeyFrames = [162640, 167170],
                    Text = "Wave goodbye to the past when hope and faith"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 167170, EndTime = 170016, Id = i++, KeyFrames = [167170, 170016],
                    Text = "Have grown so strong and sound"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 167597, EndTime = 172130, Id = i++, KeyFrames = [167597, 172130],
                    Text = "Yeahhhh Have grown so strong and sound", HiddenOnBlur = true
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 169796, EndTime = 174427, Id = i++, KeyFrames = [169796, 174427],
                    Text = "Unfold this pair of wings for me again"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 174427, EndTime = 180463, Id = i++, KeyFrames = [174427, 176550],
                    Text = "To soar above this world "
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 176175, EndTime = 180463, Id = i++, KeyFrames = [176175, 180463],
                    Text = "Sore above this world", HiddenOnBlur = true
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 176826, EndTime = 181419, Id = i++, KeyFrames = [176826, 181419],
                    Text = "Turned into a moon that always tells the warmth"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 181419, EndTime = 186440, Id = i++, KeyFrames = [181419, 184442],
                    Text = "And brightness of the sun"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 183267, EndTime = 186440, Id = i++, KeyFrames = [183267, 186440],
                    Text = "And brightness of the sun", HiddenOnBlur = true
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 184785, EndTime = 191631, Id = i++, KeyFrames = [184785, 191491],
                    Text = "May all the beauty be blessed"
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 189651, EndTime = 191631, Id = i++, KeyFrames = [189651, 191631], Text = "Be Blessed",
                    HiddenOnBlur = true
                },
                new TextRenderingLyricLine()
                {
                    StartTime = 191631, EndTime = 199311, Id = i++, KeyFrames = [191631, 199311],
                    Text = "May all the beauty be blessed"
                },
            };
            OnLyricChanged(this, null);
            ItemSpacing = 30;
            LyricWidthRatio = 1;
            LyricPaddingTopRatio = 0.1;
            LineRollingDuration = 200;
            CurrentLyricTime = 0;
            LineRollingEaseCalculator = new SinRollingCalculator();
            _needRecalculate = true;
        }
    }
}