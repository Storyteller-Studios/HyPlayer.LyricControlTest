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
using HyPlayer.LyricRenderer.RollingCalculators;
using System.Diagnostics;
using System.Timers;
using Windows.UI.Popups;
using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Models;
using Lyricify.Lyrics.Searchers;
using Windows.UI.Xaml.Controls.Maps;
using Windows.Media;
using ALRC.Converters;
using HyPlayer.LyricRenderer.Abstraction.Render;
using LrcConverter = HyPlayer.LyricRenderer.Converters.LrcConverter;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace HyPlayer.LyricRenderer
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public List<LineRollingCalculator> rollingCalculators = new List<LineRollingCalculator>()
        {
            new SinRollingCalculator(),
            new ElasticEaseRollingCalculator(),
            new LyricifyRollingCalculator(),
        };

        public MainPage()
        {
            this.InitializeComponent();
            RenderView.Context.LyricWidthRatio = 1;
            RenderView.Context.LyricPaddingTopRatio = 0.1f;
            RenderView.Context.CurrentLyricTime = 0;
            RenderView.Context.LineRollingEaseCalculator = new LyricifyRollingCalculator();
            RenderView.ChangeRenderFontSize(64, 32, 16);
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".qrc");
            picker.FileTypeFilter.Add(".lrc");
            picker.FileTypeFilter.Add(".yrc");
            picker.FileTypeFilter.Add(".alrc");
            var sf = await picker.PickSingleFileAsync();
            var qrc = await FileIO.ReadTextAsync(sf);
            ILyricConverter<string> converter = sf.FileType switch
            {
                ".qrc" => new QQLyricConverter(),
                ".yrc" => new NeteaseYrcConverter(),
                ".lrc" => new ALRC.Converters.LrcConverter(),
                ".alrc" => new ALRCConverter(),
                ".ttml" => new AppleSyllableConverter(),
                _ => new LyricifySyllableConverter()
            };
            var lrcs = LrcConverter.Convert(converter.Convert(qrc));
            RenderView.SetLyricLines(lrcs);
           
        }

        private MediaPlayer _player = new MediaPlayer();
        private async void BtnPlayClick(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".mp3");
            var sf = await picker.PickSingleFileAsync();
            _player.Source = MediaSource.CreateFromStorageFile(sf);
            _player.Play();
            RenderView.OnBeforeRender += (LyricRenderView v) => { v.Context.CurrentLyricTime = (long)_player.PlaybackSession.Position.TotalMilliseconds; };
        }

        private async void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
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

            var lrcs = LrcConverter.Convert(new QQLyricConverter().Convert(lyricData.Lyrics!));
            RenderView.SetLyricLines(lrcs);
        }

        private void Button_RightTapped_1(object sender, RightTappedRoutedEventArgs e)
        {
            _player.PlaybackSession.Position = TimeSpan.Zero;
            _player.Play();
            RenderView.ReflowTime(0);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RenderView.Context.LineRollingEaseCalculator = e.AddedItems[0] as LineRollingCalculator;
        }
    }
}
