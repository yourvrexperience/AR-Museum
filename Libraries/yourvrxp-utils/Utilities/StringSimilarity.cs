using System;
using System.Collections.Generic;
using System.Linq;

namespace yourvrexperience.Utils
{
    public class StringSimilarity
    {
        public static int LevenshteinDistance(string s1, string s2)
        {
            int[,] d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;
            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[s1.Length, s2.Length];
        }

        public static float CalculateSimilarityPercentage(string s1, string s2)
        {
            int maxLength = Math.Max(s1.Length, s2.Length);

            if (maxLength == 0)
            {
                return 100.0f;
            }

            int distance = LevenshteinDistance(s1, s2);
            return (1.0f - ((float)distance / maxLength)) * 100;
        }

        public static bool ContainsInList(string target, List<string> origin, int percentage)
        {
            if (target.Length == 0)
            {
                return false;
            }

            foreach (string item in origin)
            {
                int maxLength = Math.Max(target.Length, item.Length);

                if (maxLength > 0)
                {
                    int distance = LevenshteinDistance(target, item);
                    float result = (1.0f - ((float)distance / maxLength)) * 100;
                    if (result > percentage)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static int GetPositionInList(string target, List<string> origin, int percentage)
        {
            if (target.Length == 0)
            {
                return -1;
            }

            for (int i = 0; i < origin.Count; i++)
            {
                string item = origin[i];

                int maxLength = Math.Max(target.Length, item.Length);

                if (maxLength > 0)
                {
                    int distance = LevenshteinDistance(target, item);
                    float result = (1.0f - ((float)distance / maxLength)) * 100;
                    if (result > percentage)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public static string GetInList(string target, List<string> origin, int percentage)
        {
            if (target.Length == 0)
            {
                return null; // Both strings are empty
            }

            foreach (string item in origin)
            {
                int maxLength = Math.Max(target.Length, item.Length);

                if (maxLength > 0)
                {
                    int distance = LevenshteinDistance(target, item);
                    float result = (1.0f - ((float)distance / maxLength)) * 100;
                    if (result > percentage)
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        public static List<string> RemoveDuplicates(List<string> inputList)
        {
            return inputList.Distinct().ToList();
        }

    }
}