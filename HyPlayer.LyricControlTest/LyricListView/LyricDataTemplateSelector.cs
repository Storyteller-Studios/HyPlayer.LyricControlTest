using HyPlayer.LyricControlTest.Lyric;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace HyPlayer.LyricControlTest.LyricListView;

public class LyricDataTemplateSelector:DataTemplateSelector
{
    public DataTemplate? Empty { get; set; }
    public DataTemplate? Text { get; set; }
    public DataTemplate? Karaoke { get; set; }
    protected override DataTemplate SelectTemplateCore(object item)
    {
        switch (item)
        {
            case EmptyLyricLine:
                return Empty;
            case TextLyricLine:
                return Text;
            case KaraokeLyricLine:
                return Karaoke;
        }
        return Empty;
    }
}