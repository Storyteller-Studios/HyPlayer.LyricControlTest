using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using HyPlayer.LyricRenderer.Converters;
using HyPlayer.LyricRenderer.RollingCalculators;
using ILyricLine = HyPlayer.LyricRenderer.Abstraction.ILyricLine;
using System.Diagnostics;
using System.Timers;
using Windows.UI.Popups;
using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Models;
using Lyricify.Lyrics.Searchers;
using Windows.UI.Xaml.Controls.Maps;
using Windows.Media;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace HyPlayer.LyricRenderer
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private List<ILyricLine> _lyricLines = new()
        {
            
        };

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".mp3");
            var sf = await picker.PickSingleFileAsync();
            var musicProp = await sf.Properties.GetMusicPropertiesAsync();

            var search = await Searchers.QQMusic.GetNewSearcher().SearchForResult(new TrackMetadata
            {
                Title = musicProp.Title,
                Artist = musicProp.Artist,
                Album = musicProp.Album,
            });
            if (search is not QQMusicSearchResult searchResult)
            {
                await new MessageDialog("未找到歌曲").ShowAsync();
                return;
            }
            var lyricData = await ProviderHelper.QQMusicApi.GetLyricsAsync(searchResult.Id.ToString());
            if (lyricData is null)
            {
                await new MessageDialog("未找到歌词").ShowAsync();
                return;
            }
            
            var lrcs = LrcConverter.Convert(ParseHelper.ParseLyrics(lyricData.Lyrics, LyricsRawTypes.Qrc));
            RenderView.RenderingLyricLines = lrcs;
            RenderView.LyricWidthRatio = 1;
            RenderView.LyricPaddingTopRatio = 0.1;
            RenderView.CurrentLyricTime = 0;
            RenderView.LineRollingEaseCalculator = new LyricifyRollingCalculator();
        }

        private MediaPlayer _player = new MediaPlayer();
        private async void BtnPlayClick(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".mp3");
            var sf = await picker.PickSingleFileAsync();
            _player.CommandManager.IsEnabled = false;
            var _systemMediaTransportControls = _player.SystemMediaTransportControls;
            _systemMediaTransportControls.IsEnabled = false; 
            _systemMediaTransportControls.IsPlayEnabled = false;
            _systemMediaTransportControls.IsPauseEnabled = false;
            _player.Source = MediaSource.CreateFromStorageFile(sf);
            _player.Play();
            RenderView.OnBeforeRender += (LyricRenderView v) => { v.CurrentLyricTime = (long)_player.PlaybackSession.Position.TotalMilliseconds; };
        }

        private async void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".qrc");
            picker.FileTypeFilter.Add(".lrc");
            picker.FileTypeFilter.Add(".yrc");
            picker.FileTypeFilter.Add(".alrc");
            var sf = await picker.PickSingleFileAsync();
            var qrc = await FileIO.ReadTextAsync(sf);
            var lyricType = sf.FileType switch
            {
                ".qrc" => LyricsRawTypes.Qrc,
                ".yrc" => LyricsRawTypes.Yrc,
                ".lrc" => LyricsRawTypes.Lrc,
                ".alrc" => LyricsRawTypes.ALRC,
                _ => LyricsRawTypes.Unknown
            };
            var lrcs = LrcConverter.Convert(ParseHelper.ParseLyrics(qrc, lyricType));
            RenderView.RenderingLyricLines = lrcs;
            RenderView.LyricWidthRatio = 1;
            RenderView.LyricPaddingTopRatio = 0.1;
            RenderView.CurrentLyricTime = 0;
            RenderView.LineRollingEaseCalculator = new LyricifyRollingCalculator();
        }

        private void Button_RightTapped_1(object sender, RightTappedRoutedEventArgs e)
        {
            _player.PlaybackSession.Position = TimeSpan.Zero;
            _player.Play();
            RenderView.ReflowTime(0);
        }
    }
}
