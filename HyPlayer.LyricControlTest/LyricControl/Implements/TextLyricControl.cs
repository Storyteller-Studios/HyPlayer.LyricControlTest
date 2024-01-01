using Windows.UI.Composition;
using System;
using HyPlayer.LyricControlTest.Lyric;
using HyPlayer.LyricControlTest.Lyric.Base;
using HyPlayer.LyricControlTest.LyricControl.Base;
using Microsoft.Toolkit.Uwp.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace HyPlayer.LyricControlTest.LyricControl.Implements;

public sealed class TextLyricControl : LyricControlBase
{
#nullable disable
    private TextBlock _lyricTextBlock;
    public TextLyricLine TextLyricLine
    {
        get => (TextLyricLine)GetValue(TextLyricLineProperty);
        set => SetValue(TextLyricLineProperty, value);
    }

    public static readonly DependencyProperty TextLyricLineProperty =
        DependencyProperty.Register(nameof(TextLyricLine), typeof(TextLyricLine), typeof(TextLyricControl), new PropertyMetadata(default));
    public override LyricLineBase LyricLine => TextLyricLine;
    public TextLyricControl()
    {
        this.DefaultStyleKey = typeof(TextLyricControl);
    }
    protected override void OnApplyTemplate()
    {
        _lyricTextBlock = (TextBlock)GetTemplateChild("LyricTextBlock");
        var target = (Border)GetTemplateChild("ShadowTarget");
        var shadow = (AttachedDropShadow)GetTemplateChild("LyricShadow");
        shadow.CastTo = target;
        base.OnApplyTemplate();
    }
}
