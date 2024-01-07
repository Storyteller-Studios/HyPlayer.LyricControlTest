﻿using Lyricify.Lyrics.Helpers.General;
using System.Text;

namespace Lyricify.Lyrics.Models
{
    public class LineInfo : ILineInfo
    {
#pragma warning disable CS8618
        public LineInfo() { }
#pragma warning restore CS8618

        public LineInfo(string text)
        {
            Text = text;
        }

        public LineInfo(string text, int startTime)
        {
            Text = text;
            StartTime = startTime;
        }

        public LineInfo(string text, int startTime, int? endTime) : this(text, startTime)
        {
            EndTime = endTime;
        }

        public string Text { get; set; }

        public int? StartTime { get; set; }

        public int? EndTime { get; set; }

        public LyricsAlignment LyricsAlignment { get; set; } = LyricsAlignment.Unspecified;

        public ILineInfo? SubLine { get; set; }

        #region Common methods

        public int? Duration => EndTime - StartTime;

        public int? StartTimeWithSubLine => MathHelper.Min(StartTime, SubLine?.StartTime);

        public int? EndTimeWithSubLine => MathHelper.Max(EndTime, SubLine?.EndTime);

        public int? DurationWithSubLine => EndTimeWithSubLine - StartTimeWithSubLine;

        public string FullText
        {
            get
            {
                if (SubLine == null)
                {
                    return Text;
                }
                else
                {
                    var sb = new StringBuilder();
                    if (SubLine.StartTime < StartTime)
                    {
                        sb.Append('(');
                        sb.Append(SubLine.Text.RemoveFrontBackBrackets());
                        sb.Append(") ");
                        sb.Append(Text.Trim());
                    }
                    else
                    {
                        sb.Append(Text.Trim());
                        sb.Append(" (");
                        sb.Append(SubLine.Text.RemoveFrontBackBrackets());
                        sb.Append(')');
                    }
                    return sb.ToString();
                }
            }
        }

        public int CompareTo(object obj)
        {
            if (obj is ILineInfo line)
            {
                if (StartTime is null || line.StartTime is null) return 0;
                if (StartTime == line.StartTime) return 0;
                if (StartTime < line.StartTime) return -1;
                else return 1;
            }
            return 0;
        }

        #endregion
    }

    public class SyllableLineInfo : ILineInfo
    {
#pragma warning disable CS8618
        public SyllableLineInfo() { }
#pragma warning restore CS8618

        public SyllableLineInfo(IEnumerable<ISyllableInfo> syllables)
        {
            Syllables = syllables.ToList();
        }

        private string? _text = null;
        public string Text
        {
            get => _text ??= SyllableHelper.GetTextFromSyllableList(Syllables);
            init => _text = value;
        }

        private int? _startTime = null;
        public int? StartTime
        {
            get => _startTime ??= Syllables.First().StartTime;
            init => _startTime = value;
        }

        private int? _endTime = null;
        public int? EndTime
        {
            get => _endTime ??= Syllables.Last().EndTime;
            init => _endTime = value;
        }

        public LyricsAlignment LyricsAlignment { get; set; } = LyricsAlignment.Unspecified;

        public ILineInfo? SubLine { get; set; }

        public List<ISyllableInfo> Syllables { get; set; }

        public bool IsSyllable => Syllables is { Count: > 0 };

        #region Common methods

        public int? Duration => EndTime - StartTime;

        public int? StartTimeWithSubLine => MathHelper.Min(StartTime, SubLine?.StartTime);

        public int? EndTimeWithSubLine => MathHelper.Max(EndTime, SubLine?.EndTime);

        public int? DurationWithSubLine => EndTimeWithSubLine - StartTimeWithSubLine;

        public string FullText
        {
            get
            {
                if (SubLine == null)
                {
                    return Text;
                }
                else
                {
                    var sb = new StringBuilder();
                    if (SubLine.StartTime < StartTime)
                    {
                        sb.Append('(');
                        sb.Append(SubLine.Text.RemoveFrontBackBrackets());
                        sb.Append(") ");
                        sb.Append(Text.Trim());
                    }
                    else
                    {
                        sb.Append(Text.Trim());
                        sb.Append(" (");
                        sb.Append(SubLine.Text.RemoveFrontBackBrackets());
                        sb.Append(')');
                    }
                    return sb.ToString();
                }
            }
        }

        public int CompareTo(object obj)
        {
            if (obj is ILineInfo line)
            {
                if (StartTime is null || line.StartTime is null) return 0;
                if (StartTime == line.StartTime) return 0;
                if (StartTime < line.StartTime) return -1;
                else return 1;
            }
            return 0;
        }

        #endregion
    }

    public class FullLineInfo : LineInfo, IFullLineInfo
    {
        public FullLineInfo() { }

        public FullLineInfo(LineInfo lineInfo)
        {
            Text = lineInfo.Text;
            StartTime = lineInfo.StartTime;
            EndTime = lineInfo.EndTime;
            LyricsAlignment = lineInfo.LyricsAlignment;
            SubLine = lineInfo.SubLine;
        }

        public Dictionary<string, string?> Translations { get; set; } = new();

        public string? Pronunciation { get; set; }

        public string? ChineseTranslation
        {
            get => Translations.ContainsKey("zh") ? Translations["zh"] : null;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Translations.Remove("zh");
                }
                else
                {
                    Translations["zh"] = value!;
                }
            }
        }
    }

    public class FullSyllableLineInfo : SyllableLineInfo, IFullLineInfo
    {
        public FullSyllableLineInfo() { }

        public FullSyllableLineInfo(SyllableLineInfo lineInfo)
        {
            LyricsAlignment = lineInfo.LyricsAlignment;
            SubLine = lineInfo.SubLine;
            Syllables = lineInfo.Syllables;
        }

        public FullSyllableLineInfo(SyllableLineInfo lineInfo, string? chineseTranslation = null, string? pronunciation = null) : this(lineInfo)
        {
            if (!string.IsNullOrEmpty(chineseTranslation))
            {
                Translations["zh"] = chineseTranslation!;
            }

            if (!string.IsNullOrEmpty(pronunciation))
            {
                Pronunciation = pronunciation;
            }
        }

        public Dictionary<string, string?> Translations { get; set; } = new();

        public string? Pronunciation { get; set; }

        public string? ChineseTranslation
        {
            get => Translations.ContainsKey("zh") ? Translations["zh"] : null;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Translations.Remove("zh");
                }
                else
                {
                    Translations["zh"] = value!;
                }
            }
        }
    }

    public enum LyricsAlignment
    {
        Unspecified,
        Left,
        Center,
        Right
    }
}
