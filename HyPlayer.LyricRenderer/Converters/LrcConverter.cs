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
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            if (line is SyllableLineInfo syllableLineInfo)
            {
                var syllables = syllableLineInfo.Syllables.Select(t => new RenderingSyllable
                {
                    Syllable = t.Text,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime
                }).ToList();
                if (!string.IsNullOrWhiteSpace(line.FullText))
                {
                    if (line.FullText.StartsWith("(") && line.FullText.EndsWith(")"))
                    {
                        var start = Math.Min((lines[index - 1].StartTimeWithSubLine ?? lines[index - 1].StartTime).Value, line.StartTime.Value);
                        var end = Math.Max((lines[index - 1].EndTimeWithSubLine ?? lines[index - 1].EndTime).Value, line.EndTime.Value);
                        result.Last().StartTime = start;
                        result.Last().EndTime = end;
                        result.Add(new SyllablesRenderingLyricLine
                        {
                            Id = index,
                            HiddenOnBlur = true,
                            KeyFrames =
                            [
                                start,
                                end
                            ],
                            StartTime = start,
                            EndTime = end,
                            Syllables = syllables
                        });
                    }
                    else
                    {
                        result.Add(new SyllablesRenderingLyricLine
                        {
                            Id = index,
                            HiddenOnBlur = false,
                            KeyFrames =
                            [
                                (line.StartTimeWithSubLine ?? line.StartTime).Value,
                                (line.EndTimeWithSubLine ?? line.EndTime).Value
                            ],
                            StartTime = (line.StartTimeWithSubLine ?? line.StartTime).Value,
                            EndTime = (line.EndTimeWithSubLine ?? line.EndTime).Value,
                            Syllables = syllables
                        });
                    }
                }
                else
                    result.Add(new BreathPointRenderingLyricLine
                    {
                        Id = index,
                        KeyFrames =
                        [
                            (line.StartTimeWithSubLine ?? line.StartTime).Value,
                            (line.EndTimeWithSubLine ?? line.EndTime).Value
                        ],
                        StartTime = (line.StartTimeWithSubLine ?? line.StartTime).Value,
                        EndTime = (line.EndTimeWithSubLine ?? line.EndTime).Value,
                        HiddenOnBlur = true,
                        BeatPerMinute = 68
                    });
                
                continue;
            }
            if (!string.IsNullOrWhiteSpace(line.FullText))
                result.Add(new TextRenderingLyricLine
                {
                    Id = index,
                    KeyFrames =
                    [
                        (line.StartTimeWithSubLine ?? line.StartTime).Value,
                        (line.EndTimeWithSubLine ?? line.EndTime).Value
                    ],
                    StartTime = (line.StartTimeWithSubLine ?? line.StartTime).Value,
                    EndTime = (line.EndTimeWithSubLine ?? line.EndTime).Value,
                    Text = line.FullText
                });
            else
                result.Add(new BreathPointRenderingLyricLine
                {
                    Id = index,
                    KeyFrames =
                    [
                        (line.StartTimeWithSubLine ?? line.StartTime).Value,
                        (line.EndTimeWithSubLine ?? line.EndTime).Value
                    ],
                    StartTime = (line.StartTimeWithSubLine ?? line.StartTime).Value,
                    EndTime = (line.EndTimeWithSubLine ?? line.EndTime).Value,
                    HiddenOnBlur = false,
                    BeatPerMinute = 68
                });
        }

        return result;
    }
}