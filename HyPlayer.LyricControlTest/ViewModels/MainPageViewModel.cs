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
        //new EmptyLyricLine(TimeSpan.Zero,TimeSpan.FromSeconds(15.14)),
        //new TextLyricLine(TimeSpan.FromSeconds(15.14), TimeSpan.FromSeconds(21.03),"突然降る夕立 あぁ傘もないや嫌"),
        //new TextLyricLine(TimeSpan.FromSeconds(21.03), TimeSpan.FromSeconds(25.03),"空のご機嫌なんか知らない"),
        //new TextLyricLine(TimeSpan.FromSeconds(25.03), TimeSpan.FromSeconds(30.98),"季節の変わり目の服は 何着りゃいいんだろ"),
        //new TextLyricLine(TimeSpan.FromSeconds(30.98), TimeSpan.FromSeconds(34.78),"春と秋 どこいっちゃったんだよ"),
        //new TextLyricLine(TimeSpan.FromSeconds(34.97), TimeSpan.FromSeconds(39.82),"息も出来ない 情報の圧力"),
        //new TextLyricLine(TimeSpan.FromSeconds(39.82), TimeSpan.FromSeconds(44.65),"めまいの螺旋だ わたしはどこにいる"),
        //new TextLyricLine(TimeSpan.FromSeconds(44.65), TimeSpan.FromSeconds(50.78),"こんなに こんなに 息の音がするのに"),
        //new TextLyricLine(TimeSpan.FromSeconds(56.12), TimeSpan.Parse("01:00.36"),"めまいの螺旋だ わたしはどこにいる"),
        //new TextLyricLine(TimeSpan.Parse("01:00.36"), TimeSpan.Parse("01:05.34"),"変だね 世界の音がしない"),
    };

    [RelayCommand]
    public void Play()
    {

    }
}