using System.Collections;
using System.Collections.Generic;
using Windows.UI.Xaml;
using HyPlayer.LyricControlTest.Lyric.Interfaces;
using HyPlayer.LyricControlTest.Lyric.Base;

namespace HyPlayer.LyricControlTest.LyricControl.Interfaces;

public interface ILyricControl
{
    event RoutedEventHandler? OnActive;
    event RoutedEventHandler? OnInactive;
    bool IsActive { get; set; }
    LyricLineBase? LyricLine { get; }
}