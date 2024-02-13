using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Models;
using Lyricify.Lyrics.Parsers.Models;
using Newtonsoft.Json;
using FileInfo = Lyricify.Lyrics.Models.FileInfo;

namespace Lyricify.Lyrics.Parsers
{
    public static class ALRCParser
    {
        public static LyricsData Parse(string alrcLyric)
        {
            var result = new LyricsData
            {
                File = new FileInfo
                {
                    Type = LyricsTypes.ALRC
                },
                TrackMetadata = new TrackMetadata(),
                Lines = new List<ILineInfo>(),
                Writers = new List<string>()
            };

            // 反序列化
            var alrcFile = JsonConvert.DeserializeObject<ALRCFile>(alrcLyric);
            if (alrcFile is null) throw new Exception("ALRC Parse Error");
            // 完善 LyricData 基本信息
            result.File.AdditionalInfo = new GeneralAdditionalInfo() { Attributes = alrcFile?.SongInfo?.ToList() };
            // 判断歌词类型
            var containsWord = alrcFile!.Lines.Any(t => t.Words is { Count: > 0 });
            var allWords = alrcFile.Lines.All(t => t.Words is { Count: > 0 });
            result.File.SyncTypes = containsWord switch
            {
                true when allWords => SyncTypes.SyllableSynced,
                true when !allWords => SyncTypes.MixedSynced,
                _ => SyncTypes.LineSynced
            };
            // 解析样式表
            var styles = alrcFile.Header?.Styles?.ToDictionary(t => t.Id, t => t) ??
                         new Dictionary<string, ALRCStyle>();


            // 解析歌词
            Dictionary<ALRCLine, ILineInfo> convertedDictionary = new();
            foreach (var line in alrcFile.Lines)
            {
                ILineInfo lineInfo = default;
                if (line is null) continue;
                if (line.Words is { Count: > 0 })
                {
                    var syllableLineInfo = new FullSyllableLineInfo()
                    {
                        Text = line.RawText ?? string.Empty,
                        StartTime = (int)(line.Start ?? 0),
                        EndTime = (int)(line.End ?? 0),
                        LyricsAlignment = styles.GetStyleById(line.LineStyle).Position switch
                        {
                            ALRCStylePosition.Left => LyricsAlignment.Left,
                            ALRCStylePosition.Center => LyricsAlignment.Center,
                            ALRCStylePosition.Right => LyricsAlignment.Right,
                            _ => LyricsAlignment.Unspecified
                        },
                        Syllables = new(),
                    };
                    syllableLineInfo.Translations = line.LineTranslations ?? new Dictionary<string, string?>();
                    syllableLineInfo.ChineseTranslation =
                        line.LineTranslations?.TryGetValue("zh-CN", out var translation) is true
                            ? translation
                            : null;
                    syllableLineInfo.Pronunciation = line.Transliteration;
                    if (syllableLineInfo.Pronunciation is null)
                    {
                        if (line.Words?.Any(t=>!string.IsNullOrWhiteSpace(t.Transliteration)) is true)
                        {
                            syllableLineInfo.Pronunciation = string.Join(" ", line.Words.Where(t => !string.IsNullOrWhiteSpace(t.Transliteration)).Select(t => t.Transliteration));
                        }
                    }
                    foreach (var word in line.Words ?? new List<ALRCWord>())
                    {
                        syllableLineInfo.Syllables.Add(new SyllableInfo()
                        {
                            StartTime = (int)word.Start,
                            EndTime = (int)word.End,
                            Text = word.Word,
                            Transliteration = word.Transliteration
                        });
                    }

                    lineInfo = syllableLineInfo;
                }
                else
                {
                    lineInfo = new FullLineInfo
                    {
                        StartTime = (int)(line.Start ?? 0),
                        EndTime = (int)(line.End ?? 0),
                        LyricsAlignment = styles.GetStyleById(line.LineStyle).Position switch
                        {
                            ALRCStylePosition.Left => LyricsAlignment.Left,
                            ALRCStylePosition.Center => LyricsAlignment.Center,
                            ALRCStylePosition.Right => LyricsAlignment.Right,
                            _ => LyricsAlignment.Unspecified
                        },
                        Translations = line.LineTranslations ?? new Dictionary<string, string?>(),
                        Pronunciation = line.Transliteration,
                        ChineseTranslation = line.LineTranslations?.TryGetValue("zh-CN", out var translation) is true
                            ? translation
                            : null,
                        Text = line.RawText ?? string.Empty
                    };
                }
                
                convertedDictionary.Add(line, lineInfo);
                if (line.ParentLineId is not null)
                {
                    var pline = alrcFile.Lines.FirstOrDefault(t => t.Id == line.ParentLineId);
                    if (convertedDictionary.TryGetValue(pline, out var value))
                        value.SubLine = lineInfo;
                }
                else
                {
                    result.Lines.Add(lineInfo);
                }
            }

            return result;
        }

        public static ALRCStyle GetStyleById(this Dictionary<string, ALRCStyle>? styles, string? id)
        {
            var defaultStyle = new ALRCStyle
            {
                Id = "def",
                Position = ALRCStylePosition.Undefined,
                Color = null,
                Type = ALRCStyleAccent.Normal
            };
            if (styles is null || id is null || !styles.ContainsKey(id))
            {
                return defaultStyle;
            }

            return styles[id];
        }

        /// <summary>
        /// 解析 QRC 歌词
        /// </summary>
        public static List<ILineInfo> ParseLyrics(List<string> lines, int? offset = null)
        {
            var list = new List<SyllableLineInfo>();

            foreach (var line in lines)
            {
                // 处理歌词行
                var item = ParseLyricsLine(line);
                if (item != null)
                {
                    list.Add(item);
                }
            }

            var returnList = list.Cast<ILineInfo>().ToList();
            if (offset.HasValue && offset.Value != 0)
            {
                OffsetHelper.AddOffset(returnList, offset.Value);
            }

            return returnList;
        }

        /// <summary>
        /// 解析 QRC 歌词行
        /// </summary>
        public static SyllableLineInfo? ParseLyricsLine(string line)
        {
            if (line.IndexOf(']') != -1)
            {
                line = line[(line.IndexOf("]") + 1)..];
            }

            List<SyllableInfo> lyricItems = new();
            MatchCollection matches = Regex.Matches(line, @"(.*?)\((\d+),(\d+)\)");

            foreach (Match match in matches.Cast<Match>())
            {
                if (match.Groups.Count == 4)
                {
                    string text = match.Groups[1].Value;
                    int startTime = int.Parse(match.Groups[2].Value);
                    int duration = int.Parse(match.Groups[3].Value);

                    int endTime = startTime + duration;

                    lyricItems.Add(new() { Text = text, StartTime = startTime, EndTime = endTime });
                }
            }

            return new(lyricItems);
        }
    }
}