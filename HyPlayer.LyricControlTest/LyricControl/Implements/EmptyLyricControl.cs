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

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace HyPlayer.LyricControlTest.LyricControl.Implements;

public sealed class EmptyLyricControl : LyricControlBase
{
    public EmptyLyricControl()
    {
        this.DefaultStyleKey = typeof(EmptyLyricControl);
    }

    public EmptyLyricLine EmptyLyricLine
    {
        get => (EmptyLyricLine)GetValue(TextLyricLineProperty);
        set => SetValue(TextLyricLineProperty, value);
    }

    public static readonly DependencyProperty TextLyricLineProperty =
        DependencyProperty.Register(nameof(EmptyLyricLine), typeof(EmptyLyricLine), typeof(EmptyLyricControl), new PropertyMetadata(default));
    public override LyricLineBase LyricLine => EmptyLyricLine;

}