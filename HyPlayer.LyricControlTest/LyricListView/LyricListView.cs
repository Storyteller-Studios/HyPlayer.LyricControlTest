using HyPlayer.LyricControlTest.Lyric;
using HyPlayer.LyricControlTest.Lyric.Base;
using HyPlayer.LyricControlTest.LyricControl.Base;
using HyPlayer.LyricControlTest.LyricControl.Implements;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace HyPlayer.LyricControlTest.LyricListView;

public sealed class LyricListView : Control
{
    private ListView? _lyricContainer;

    public List<LyricLineBase> LyricLines
    {
        get => (List<LyricLineBase>)GetValue(LyricLinesProperty);
        set => SetValue(LyricLinesProperty, value);
    }

    public static readonly DependencyProperty LyricLinesProperty =
        DependencyProperty.Register(nameof(LyricLines), typeof(List<LyricLineBase>), typeof(LyricListView), new PropertyMetadata(default));


    public TimeSpan CurrentTime
    {
        get => (TimeSpan)GetValue(CurrentTimeProperty);
        set => SetValue(CurrentTimeProperty, value);
    }

    public static readonly DependencyProperty CurrentTimeProperty =
        DependencyProperty.Register(nameof(CurrentTime), typeof(TimeSpan), typeof(LyricListView), new PropertyMetadata(default));

    public LyricListView()
    {
        this.DefaultStyleKey = typeof(LyricListView);
    }
    public event RoutedEventHandler LyricChanged;
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _lyricContainer = (ListView)GetTemplateChild("LyricContainer");
        //ActiveLyrics();
    }
    //    public async void ActiveLyrics()
    //    {
    //#nullable disable
    //        await Task.Delay(2000);
    //        if (_lyricContainer!.Items is null) return;
    //        foreach (var i in _lyricContainer.Items)
    //        {
    //            var lyric = (LyricLineBase)i;
    //            var container = (ListViewItem)_lyricContainer.ContainerFromItem(i);
    //            var control = (LyricControlBase)container!.ContentTemplateRoot;
    //            control!.IsActive = true;
    //            await Task.Delay(lyric.EndTime - lyric.StartTime);
    //            control!.IsActive = false;
    //            await Task.Delay(100);
    //        }
    //    }
    
    public void GetCurrentLyric()
    {
        LyricLines.ForEach(p =>
        {
            var container = (ListViewItem?)_lyricContainer?.ContainerFromItem(p);
            if (container is null) return;
            var control = (LyricControlBase)container.ContentTemplateRoot;
            if (p.StartTime < CurrentTime && CurrentTime < p.EndTime)
            {
                if(!control.IsActive)
                {
                    control.IsActive = true;
                    if (p is not EmptyLyricLine)
                        _lyricContainer.SmoothScrollIntoViewWithItemAsync(p, ScrollItemPlacement.Center);

                }
                if (control is EmptyLyricControl emptyLyricControl)
                    emptyLyricControl.CurrentTime = CurrentTime;
            }
            else
                control.IsActive = false;
        });
    }
}