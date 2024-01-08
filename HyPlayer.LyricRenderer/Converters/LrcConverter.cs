using System;
using System.Collections.Generic;
using System.Linq;
using HyPlayer.LyricRenderer.Abstraction.Render;
using HyPlayer.LyricRenderer.LyricLineRenderers;
using Lyricify.Lyrics.Models;

namespace HyPlayer.LyricRenderer.Converters;

public static class LrcConverter
{
    public static List<RenderingLyricLine> Convert(LyricsData lyricsData)
    {
        var result = new List<RenderingLyricLine>();
        var lines = lyricsData.Lines ?? new();
        bool isSubLine = false;
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
        parseLine:
            if (line is FullSyllableLineInfo syllableLineInfo)
            {
                var syllables = syllableLineInfo.Syllables.Select(t => new RenderingSyllable
                {
                    Syllable = t.Text,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,                    
                }).ToList();
                if (!string.IsNullOrWhiteSpace(line.FullText) || syllables.Count > 0)
                {
                    result.Add(new SyllablesRenderingLyricLine
                    {
                        Id = index + (isSubLine ? lines.Count : 0),
                        HiddenOnBlur = isSubLine,
                        KeyFrames =
                        [
                            syllableLineInfo.StartTimeWithSubLine.Value,
                            syllableLineInfo.EndTimeWithSubLine.Value
                        ],
                        StartTime = syllableLineInfo.StartTimeWithSubLine.Value,
                        EndTime = syllableLineInfo.EndTimeWithSubLine.Value,
                        Syllables = syllables,
                        Transliteration = syllableLineInfo.Pronunciation,
                        Translation = syllableLineInfo.ChineseTranslation
                    });
                }
                else
                    result.Add(new BreathPointRenderingLyricLine
                    {
                        Id = index+ (isSubLine ? lines.Count : 0),
                        KeyFrames =
                        [
                            line.StartTimeWithSubLine.Value,
                            line.EndTimeWithSubLine.Value
                        ],
                        StartTime = line.StartTimeWithSubLine.Value,
                        EndTime = line.EndTimeWithSubLine.Value,
                        HiddenOnBlur = true,
                        BeatPerMinute = 160
                    });

                if (line.SubLine is not null)
                {
                    line = line.SubLine;
                    isSubLine = true;
                    goto parseLine;
                }

                isSubLine = false;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(line.FullText))
                result.Add(new TextRenderingLyricLine
                {
                    Id = index+ (isSubLine ? lines.Count : 0),
                    KeyFrames =
                    [
                        line.StartTimeWithSubLine.Value,
                        line.EndTimeWithSubLine.Value
                    ],
                    StartTime = line.StartTimeWithSubLine.Value,
                    EndTime = line.EndTimeWithSubLine.Value,
                    Text = line.FullText
                });
            else
                result.Add(new BreathPointRenderingLyricLine
                {
                    Id = index+ (isSubLine ? lines.Count : 0),
                    KeyFrames =
                    [
                        line.StartTimeWithSubLine.Value,
                        line.EndTimeWithSubLine.Value
                    ],
                    StartTime = line.StartTimeWithSubLine.Value,
                    EndTime = line.EndTimeWithSubLine.Value,
                    HiddenOnBlur = true,
                    BeatPerMinute = 160
                });
            if (line.SubLine is not null)
            {
                line = line.SubLine;
                isSubLine = true;
                goto parseLine;
            }

            isSubLine = false;
        }

        return result;
    }
}