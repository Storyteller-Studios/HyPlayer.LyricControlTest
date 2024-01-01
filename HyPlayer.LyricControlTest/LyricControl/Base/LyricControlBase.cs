using System.Collections.Generic;
using HyPlayer.LyricControlTest.LyricControl.Interfaces;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using HyPlayer.LyricControlTest.Lyric.Interfaces;
using HyPlayer.LyricControlTest.Lyric.Base;

namespace HyPlayer.LyricControlTest.LyricControl.Base;

public abstract class LyricControlBase : Control , ILyricControl
{
    internal const string NormalState = "Normal";
    internal const string ActiveState = "Active";
    public virtual bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set
        {
            if (value)
            {
                OnActive?.Invoke(this, new RoutedEventArgs());
                VisualStateManager.GoToState(this, ActiveState, true);
                Active();
            }
            else
            {
                OnInactive?.Invoke(this, new RoutedEventArgs());
                VisualStateManager.GoToState(this, NormalState, true);
                Inactive();
            }
            SetValue(IsActiveProperty, value);
        }
    }
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(LyricControlBase), new PropertyMetadata(false));  
    public abstract LyricLineBase LyricLine { get; }

    public event RoutedEventHandler? OnActive;

    public event RoutedEventHandler? OnInactive;
    protected virtual void Active() { }
    protected virtual void Inactive() { }
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        VisualStateManager.GoToState(this, NormalState, true);
    }
}