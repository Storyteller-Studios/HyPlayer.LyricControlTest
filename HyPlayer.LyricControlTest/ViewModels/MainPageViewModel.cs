using System;
using CommunityToolkit.Mvvm.ComponentModel;
using HyPlayer.LyricControlTest.Lyric;
using System.Collections.Generic;
using HyPlayer.LyricControlTest.Lyric.Base;
using CommunityToolkit.Mvvm.Input;
using Windows.UI.Xaml;
using System.Threading;

namespace HyPlayer.LyricControlTest.ViewModels;

public partial class MainPageViewModel:ObservableObject
{
    [ObservableProperty]
    private List<LyricLineBase>? _lyricLines = new()
    {
        new TextLyricLine(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5),"突然降る夕立 あぁ傘もないや嫌"),
        new TextLyricLine(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10),"空のご機嫌なんか知らない"),
        new TextLyricLine(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20),"季節の変わり目の服は 何着りゃいいんだろ"),
        new TextLyricLine(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(30),"春と秋 どこいっちゃったんだよ"),
        new EmptyLyricLine(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(40)),
        new TextLyricLine(TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(50),"息も出来ない 情報の圧力"),
        new TextLyricLine(TimeSpan.FromSeconds(55), TimeSpan.FromSeconds(60),"めまいの螺旋だ わたしはどこにいる"),
        new TextLyricLine(TimeSpan.FromSeconds(65), TimeSpan.FromSeconds(70),"こんなに こんなに 息の音がするのに"),
        new TextLyricLine(TimeSpan.FromSeconds(75), TimeSpan.FromSeconds(80),"めまいの螺旋だ わたしはどこにいる"),
        new TextLyricLine(TimeSpan.FromSeconds(85), TimeSpan.FromSeconds(90),"変だね 世界の音がしない"),
    };

    [RelayCommand]
    public void Play()
    {

    }
}