using HyPlayer.LyricControlTest.Lyric;
using HyPlayer.LyricControlTest.Lyric.Base;
using HyPlayer.LyricControlTest.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Timers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace HyPlayer.LyricControlTest;

/// <summary>
/// 可用于自身或导航至 Frame 内部的空白页。
/// </summary>
public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; set; } = new();
    public MainPage()
    {
        this.InitializeComponent();
    }

    private async void MusicBtn_Click(object sender, RoutedEventArgs e)
    {
        FileOpenPicker picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".mp3");
        picker.FileTypeFilter.Add(".flac");
        var sf = await picker.PickSingleFileAsync();
        if (sf is null) return;

        var folder = await sf.GetParentAsync();
        if (await folder.TryGetItemAsync(sf.DisplayName+".lrc") is IStorageFile lyricFile)
        {
            GetLyric(lyricFile);
        }

        Player.Source = MediaSource.CreateFromStorageFile(sf);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.01) };
        timer.Start();
        timer.Tick += (_, _) =>
        {
            LyricView.CurrentTime = Player.MediaPlayer.PlaybackSession.Position;
            LyricView.GetCurrentLyric();
        };
    }

    private async void LyricBtn_Click(object sender, RoutedEventArgs e)
    {
        FileOpenPicker picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".lrc");
        var sf = await picker.PickSingleFileAsync();
        if (sf is null) return;
        GetLyric(sf);

    }
    public async void GetLyric(IStorageFile file)
    {
        var lines = await FileIO.ReadLinesAsync(file);
        var list = new List<LyricLineBase>();
        for (var i = 0; i < lines.Count; i++)
        {
            var regex = new Regex(@"\[([0-9.:]*)\]+(.*)", RegexOptions.Compiled);
            var startTime = TimeSpan.Parse("00:" + regex.Matches(lines[i])[0].Groups[1].Value);
            var word = lines[i].Substring(lines[i].LastIndexOf("]") + 1);
            string? translation = null;
            if (lines[i].Contains("「") && lines[i].Contains("」"))
            {
                translation = lines[i].Substring(lines[i].IndexOf("「") + 1, lines[i].LastIndexOf("」") - lines[i].IndexOf("「") - 1);
                word = word.Remove(word.IndexOf("「"));
            }
            if (i + 1 == lines.Count)
            {
                if (word is not "")
                    list.Add(new TextLyricLine(startTime, TimeSpan.MaxValue, word, translation));
                else
                    list.Add(new TextLyricLine(startTime, TimeSpan.MaxValue, word, translation));
                continue;
            }
            var endTime = TimeSpan.Parse("00:" + regex.Matches(lines[i + 1])[0].Groups[1].Value);


            if (word is not "")
            {
                list.Add(new TextLyricLine(startTime, endTime, word, translation));
                continue;
            }
            if (endTime - startTime > TimeSpan.FromSeconds(5))
                list.Add(new EmptyLyricLine(startTime, endTime));
        }
        LyricView.LyricLines = list;
    }
}