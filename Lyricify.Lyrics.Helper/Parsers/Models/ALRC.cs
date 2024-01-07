using Newtonsoft.Json;

namespace Lyricify.Lyrics.Parsers.Models;

public class ALRCFile
{
    [JsonProperty("$schema")] public string Schema { get; set; }

    [JsonProperty("li")] public ALRCLyricInfo? LyricInfo { get; set; }

    [JsonProperty("si")] public Dictionary<string, string>? SongInfo { get; set; }

    [JsonProperty("h")] public ALRCHeader? Header { get; set; }

    [JsonProperty("l")] public List<ALRCLine> Lines { get; set; }
}

public class ALRCHeader
{
    [JsonProperty("s")] public List<ALRCStyle>? Styles { get; set; }
}

public class ALRCStyle
{
    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("p")] public ALRCStylePosition? Position { get; set; } = ALRCStylePosition.Undefined;

    [JsonProperty("c")] public string? Color { get; set; }

    [JsonProperty("t")] public ALRCStyleAccent? Type { get; set; } = ALRCStyleAccent.Normal;
}

public enum ALRCStylePosition
{
    Undefined = 0,
    Left = 1,
    Center = 2,
    Right = 3
}

public enum ALRCStyleAccent
{
    Normal = 0,
    Background = 1,
    Whisper = 2,
    Emphasise = 3
}

public class ALRCLine
{
    [JsonProperty("id")] public string? Id { get; set; }

    [JsonProperty("p")] public string? ParentLineId { get; set; }

    [JsonProperty("f")] public long? Start { get; set; }

    [JsonProperty("t")] public long? End { get; set; }

    [JsonProperty("s")] public string? LineStyle { get; set; }


    [JsonProperty("cm")] public string? Comment { get; set; }

    [JsonProperty("tx")] public string? RawText { get; set; }

    [JsonProperty("lt")] public string? Transliteration { get; set; }
    [JsonProperty("tr")] public Dictionary<string, string?>? LineTranslations { get; set; }

    [JsonProperty("w")] public List<ALRCWord>? Words { get; set; }
}

public class ALRCLyricInfo
{
    [JsonProperty("lng")] public string? Language { get; set; }

    [JsonProperty("author")] public string? Author { get; set; }

    [JsonProperty("translation")] public string? Translation { get; set; }

    [JsonProperty("timeline")] public string? Timeline { get; set; }

    [JsonProperty("transliteration")] public string? Transliteration { get; set; }

    [JsonProperty("proofread")] public string? Proofread { get; set; }

    [JsonProperty("offset")] public int? Offset { get; set; }

    [JsonProperty("duration")] public long? Duration { get; set; }
}

public class ALRCWord
{
    [JsonProperty("f")] public long Start { get; set; }

    [JsonProperty("t")] public long End { get; set; }

    [JsonProperty("w")] public string Word { get; set; }

    [JsonProperty("s")] public string? WordStyle { get; set; }

    [JsonProperty("l")] public string? Transliteration { get; set; }
}