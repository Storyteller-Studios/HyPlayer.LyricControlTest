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
            long endTime = line.EndTimeWithSubLine.GetValueOrDefault(-1);
            if (endTime is -1) endTime = lines.Count > index + 1 ? lines[index + 1].StartTimeWithSubLine.GetValueOrDefault(0) : int.MaxValue;
            if (line is FullSyllableLineInfo syllableLineInfo)
            {
                var syllables = syllableLineInfo.Syllables.Select(t => new RenderingSyllable
                {
                    Syllable = t.Text,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                }).ToList();

                var transliterationSyllableInfo = syllableLineInfo.Syllables.Cast<SyllableInfo>().Select(t => new RenderingSyllable()
                {
                    Syllable = t.Transliteration + " ",
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                });
                if (!string.IsNullOrWhiteSpace(line.FullText) || syllables.Count > 0)
                {
                    result.Add(new SyllablesRenderingLyricLine
                    {
                        IsSyllable = true,
                        Id = index + (isSubLine ? lines.Count : 0),
                        HiddenOnBlur = isSubLine,
                        KeyFrames =
                        [
                            syllableLineInfo.StartTimeWithSubLine.GetValueOrDefault(0),
                            endTime
                        ],
                        StartTime = syllableLineInfo.StartTimeWithSubLine.GetValueOrDefault(0),
                        EndTime = endTime,
                        Syllables = syllables,
                        RomajiSyllables = transliterationSyllableInfo.ToList(),
                        Transliteration = syllableLineInfo.Pronunciation,
                        Translation = syllableLineInfo.ChineseTranslation
                    });
                }
                else
                    result.Add(new ProgressBarRenderingLyricLine
                    {
                        Id = index + (isSubLine ? lines.Count : 0),
                        KeyFrames =
                        [
                            line.StartTimeWithSubLine.GetValueOrDefault(0),
                            endTime
                        ],
                        StartTime = line.StartTimeWithSubLine.GetValueOrDefault(0),
                        EndTime = endTime,
                        HiddenOnBlur = true
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
            {
                
                result.Add(new SyllablesRenderingLyricLine()
                {
                    IsSyllable = false,
                    Id = index + (isSubLine ? lines.Count : 0),
                    KeyFrames =
                    [
                        line.StartTimeWithSubLine.GetValueOrDefault(0),
                        endTime
                    ],
                    StartTime = line.StartTimeWithSubLine.GetValueOrDefault(0),
                    EndTime = endTime,
                    Text = line.FullText
                });
            }
            else
                result.Add(new ProgressBarRenderingLyricLine
                {
                    Id = index + (isSubLine ? lines.Count : 0),
                    KeyFrames =
                    [
                        line.StartTimeWithSubLine.GetValueOrDefault(0),
                        endTime
                    ],
                    StartTime = line.StartTimeWithSubLine.GetValueOrDefault(0),
                    EndTime = endTime,
                    HiddenOnBlur = true,
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