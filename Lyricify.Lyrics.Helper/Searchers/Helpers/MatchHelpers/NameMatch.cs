﻿using Lyricify.Lyrics.Helpers.General;

namespace Lyricify.Lyrics.Searchers.Helpers
{
    public static partial class CompareHelper
    {
        /// <summary>
        /// 比较曲目名或专辑名匹配程度
        /// </summary>
        /// <param name="name1">原曲目名</param>
        /// <param name="name2">搜索得到的曲目名</param>
        /// <returns>名称匹配程度</returns>
        public static NameMatchType? CompareName(string? name1, string? name2)
        {
            if (name1 == null || name2 == null) return null;

            name1 = name1.ToSC(true).ToLower().Trim();
            name2 = name2.ToSC(true).ToLower().Trim();

            if (name1 == name2) return NameMatchType.Perfect;

            name1 = name1.Replace('’', '\'').Replace('，', ',');
            name2 = name2.Replace('’', '\'').Replace('，', ',');
            name1 = name1.Replace("acoustic version", "acoustic");
            name2 = name2.Replace("acoustic version", "acoustic");

            if ((name1.Replace(" - ", " (").Trim() + ")").Remove(" ")
                == (name2.Replace(" - ", " (").Trim() + ")").Remove(" "))
                return NameMatchType.VeryHigh;

            static bool specialCompare(string str1, string str2, string special)
            {
                special = "(" + special;
                bool c1 = str1.Contains(special);
                bool c2 = str2.Contains(special);
                if (c1 && !c2 && str1[..(str1.IndexOf(special) - 1)].Trim() == str2) return true;
                if (c2 && !c1 && str2[..(str2.IndexOf(special) - 1)].Trim() == str1) return true;
                return false;
            }

            if (specialCompare(name1, name2, "deluxe")) return NameMatchType.High;
            if (specialCompare(name1, name2, "explicit")) return NameMatchType.High;

            // 在同长度的情况下，判断解决异体字的问题
            int count = 0;
            if (name1.Length == name2.Length)
            {
                for (int i = 0; i < name1.Length; i++)
                    if (name1[i] == name2[i]) count++;

                if ((double)count / name1.Length >= 0.8 && name1.Length >= 4
                    || (double)count / name1.Length >= 0.5 && name1.Length >= 2 && name1.Length <= 3)
                    return NameMatchType.High;
            }

            if (StringHelper.ComputeTextSame(name1, name2, true) > 66) return NameMatchType.Low;

            return NameMatchType.NoMatch;
        }

        public static int GetMatchScore(this NameMatchType? matchType)
        {
            return matchType switch
            {
                NameMatchType.Perfect => 7,
                NameMatchType.VeryHigh => 6,
                NameMatchType.High => 5,
                NameMatchType.Low => 2,
                NameMatchType.NoMatch => 0,
                _ => 0,
            };
        }

        /// <summary>
        /// 名称匹配程度
        /// </summary>
        public enum NameMatchType
        {
            Perfect,
            VeryHigh,
            High,
            Low,
            NoMatch = -1,
        }
    }
}
