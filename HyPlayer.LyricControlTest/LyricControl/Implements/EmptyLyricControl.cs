using HyPlayer.LyricControlTest.LyricControl.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using HyPlayer.LyricControlTest.Lyric.Base;
using HyPlayer.LyricControlTest.Lyric;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Composition;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace HyPlayer.LyricControlTest.LyricControl.Implements;

public sealed class EmptyLyricControl : LyricControlBase
{
    public EmptyLyricControl()
    {
        this.DefaultStyleKey = typeof(EmptyLyricControl);
    }
    private ProgressBar? _progressBar;
    public EmptyLyricLine EmptyLyricLine
    {
        get => (EmptyLyricLine)GetValue(TextLyricLineProperty);
        set => SetValue(TextLyricLineProperty, value);
    }

    public static readonly DependencyProperty TextLyricLineProperty =
        DependencyProperty.Register(nameof(EmptyLyricLine), typeof(EmptyLyricLine), typeof(EmptyLyricControl), new PropertyMetadata(default));
    public override LyricLineBase LyricLine => EmptyLyricLine;


    public TimeSpan CurrentTime
    {
        get => (TimeSpan)GetValue(CurrentTimeProperty);
        set => SetValue(CurrentTimeProperty, value);
    }

    public static readonly DependencyProperty CurrentTimeProperty =
        DependencyProperty.Register(nameof(CurrentTime), typeof(TimeSpan), typeof(EmptyLyricControl), new PropertyMetadata(default,new PropertyChangedCallback(OnCurrentTimeUpdated)));

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _progressBar = (ProgressBar)GetTemplateChild("MainProgressBar");
    }

    private static void OnCurrentTimeUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (EmptyLyricControl)d;
        var value = control.EasingFunction.Ease(((control.CurrentTime - control.EmptyLyricLine.StartTime) / (control.EmptyLyricLine.EndTime- control.EmptyLyricLine.StartTime))) * 100;
        if (!(0 < value && value < 100)) return;
        control._progressBar.Value = value;
    }


    public EaseFunctionBase EasingFunction
    {
        get => (EaseFunctionBase)GetValue(EasingFunctionProperty);
        set => SetValue(EasingFunctionProperty, value);
    }

    public static readonly DependencyProperty EasingFunctionProperty =
        DependencyProperty.Register(nameof(EasingFunction), typeof(EaseFunctionBase), typeof(EmptyLyricControl), new PropertyMetadata(new CustomCircleEase { EasingMode = EasingMode.EaseOut}));





    public abstract class EaseFunctionBase
    {
        public EasingMode EasingMode { get; set; }
        protected abstract double EaseInCore(double normalizedTime);

        public double Ease(double normalizedTime)
        {
            switch (EasingMode)
            {
                case EasingMode.EaseIn:
                    return EaseInCore(normalizedTime);
                case EasingMode.EaseOut:
                    // EaseOut is the same as EaseIn, except time is reversed & the result is flipped.
                    return 1.0 - EaseInCore(1.0 - normalizedTime);
                case EasingMode.EaseInOut:
                default:
                    // EaseInOut is a combination of EaseIn & EaseOut fit to the 0-1, 0-1 range.
                    return (normalizedTime < 0.5)
                        ? EaseInCore(normalizedTime * 2.0) * 0.5
                        : (1.0 - EaseInCore((1.0 - normalizedTime) * 2.0)) * 0.5 + 0.5;
            }
        }
    }

    public class CustomCircleEase : EaseFunctionBase
    {
        protected override double EaseInCore(double normalizedTime)
        {
            normalizedTime = Math.Max(0.0, Math.Min(1.0, normalizedTime));
            return 1.0 - Math.Sqrt(1.0 - normalizedTime * normalizedTime);
        }
    }
}