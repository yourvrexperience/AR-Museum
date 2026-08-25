using System;
using System.Runtime.CompilerServices;

namespace SoftWx.Match {
    public class Levenshtein : IDistance, ISimilarity {
        private int[] baseChar1Costs;

        public Levenshtein() {
            this.baseChar1Costs = new int[0];
        }

        public Levenshtein(int expectedMaxStringLength) {
            this.baseChar1Costs = new int[expectedMaxStringLength];
        }

        public double Distance(string string1, string string2) {
            if (string1 == null) return (string2 ?? "").Length;
            if (string2 == null) return string1.Length;

            if (string1.Length > string2.Length) { var t = string1; string1 = string2; string2 = t; }

            int len1, len2, start;
            Helpers.PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return len2;

            return Distance(string1, string2, len1, len2, start,
                (this.baseChar1Costs = (len2 <= this.baseChar1Costs.Length) ? this.baseChar1Costs : new int[len2]));
        }

        public double Distance(string string1, string string2, double maxDistance) {
            if (string1 == null || string2 == null) return Helpers.NullDistanceResults(string1, string2, maxDistance);
            if (maxDistance <= 0) return (string1 == string2) ? 0 : -1;
            maxDistance = Math.Ceiling(maxDistance);
            int iMaxDistance = (maxDistance <= int.MaxValue) ? (int)maxDistance : int.MaxValue;

            if (string1.Length > string2.Length) { var t = string1; string1 = string2; string2 = t; }
            if (string2.Length - string1.Length > iMaxDistance) return -1;

            int len1, len2, start;
            Helpers.PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return (len2 <= iMaxDistance) ? len2 : -1;

            if (iMaxDistance < len2) {
                return Distance(string1, string2, len1, len2, start, iMaxDistance,
                    (this.baseChar1Costs = (len2 <= this.baseChar1Costs.Length) ? this.baseChar1Costs : new int[len2]));
            }
            return Distance(string1, string2, len1, len2, start,
                (this.baseChar1Costs = (len2 <= this.baseChar1Costs.Length) ? this.baseChar1Costs : new int[len2]));
        }

        public double Similarity(string string1, string string2) {
            if (string1 == null) return (string2 == null) ? 1 : 0;
            if (string2 == null) return 0;

            if (string1.Length > string2.Length) { var t = string1; string1 = string2; string2 = t; }

            int len1, len2, start;
            Helpers.PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return 1.0;

            return Distance(string1, string2, len1, len2, start,
                    (this.baseChar1Costs = (len2 <= this.baseChar1Costs.Length) ? this.baseChar1Costs : new int[len2]))
                    .ToSimilarity(string2.Length);
        }

        public double Similarity(string string1, string string2, double minSimilarity) {
            if (minSimilarity < 0 || minSimilarity > 1) throw new ArgumentException("minSimilarity must be in range 0 to 1.0");
            if (string1 == null || string2 == null) return Helpers.NullSimilarityResults(string1, string2, minSimilarity);

            if (string1.Length > string2.Length) { var t = string1; string1 = string2; string2 = t; }

            int iMaxDistance = minSimilarity.ToDistance(string2.Length);
            if (string2.Length - string1.Length > iMaxDistance) return -1;
            if (iMaxDistance == 0) return (string1 == string2) ? 1 : -1;

            int len1, len2, start;
            Helpers.PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return 1.0;

            if (iMaxDistance < len2) {
                return Distance(string1, string2, len1, len2, start, iMaxDistance,
                        (this.baseChar1Costs = (len2 <= this.baseChar1Costs.Length) ? this.baseChar1Costs : new int[len2]))
                        .ToSimilarity(string2.Length);
            }
            return Distance(string1, string2, len1, len2, start,
                    (this.baseChar1Costs = (len2 <= this.baseChar1Costs.Length) ? this.baseChar1Costs : new int[len2]))
                    .ToSimilarity(string2.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Distance(string string1, string string2, int len1, int len2, int start, int[] char1Costs) {
            for (int j = 0; j < len2;) char1Costs[j] = ++j;
            int currentCharCost = 0;
            if (start == 0) {
                for (int i = 0; i < len1; ++i) {
                    int leftCharCost, aboveCharCost;
                    leftCharCost = aboveCharCost = i;
                    char char1 = string1[i];
                    for (int j = 0; j < len2; ++j) {
                        currentCharCost = leftCharCost;
                        leftCharCost = char1Costs[j];
                        if (string2[j] != char1) {
                            if (aboveCharCost < currentCharCost) currentCharCost = aboveCharCost;
                            if (leftCharCost < currentCharCost) currentCharCost = leftCharCost;
                            ++currentCharCost;
                        }
                        char1Costs[j] = aboveCharCost = currentCharCost;
                    }
                }
            } else {
                for (int i = 0; i < len1; ++i) {
                    int leftCharCost, aboveCharCost;
                    leftCharCost = aboveCharCost = i;
                    char char1 = string1[start + i];
                    for (int j = 0; j < len2; ++j) {
                        currentCharCost = leftCharCost;
                        leftCharCost = char1Costs[j];
                        if (string2[start + j] != char1) {
                            if (aboveCharCost < currentCharCost) currentCharCost = aboveCharCost;
                            if (leftCharCost < currentCharCost) currentCharCost = leftCharCost;
                            ++currentCharCost;
                        }
                        char1Costs[j] = aboveCharCost = currentCharCost;
                    }
                }
            }
            return currentCharCost;
        }

        internal static int Distance(string string1, string string2, int len1, int len2, int start, int maxDistance, int[] char1Costs) {
#if DEBUG
                    if (len2 < maxDistance) throw new ArgumentException();
                    if (len2-len1 > maxDistance) throw new ArgumentException();
#endif
            int i, j;
            for (j = 0; j < maxDistance;) char1Costs[j] = ++j;
            for (; j < len2;) char1Costs[j++] = maxDistance + 1;
            int lenDiff = len2 - len1;
            int jStartOffset = maxDistance - lenDiff;
            int jStart = 0;
            int jEnd = maxDistance;
            int currentCost = 0;
            if (start == 0) {
                for (i = 0; i < len1; ++i) {
                    char char1 = string1[i];
                    int prevChar1Cost, aboveCharCost;
                    prevChar1Cost = aboveCharCost = i;
                    jStart += (i > jStartOffset) ? 1 : 0;
                    jEnd += (jEnd < len2) ? 1 : 0;
                    for (j = jStart; j < jEnd; ++j) {
                        currentCost = prevChar1Cost;
                        prevChar1Cost = char1Costs[j];
                        if (string2[j] != char1) {
                            if (aboveCharCost < currentCost) currentCost = aboveCharCost;
                            if (prevChar1Cost < currentCost) currentCost = prevChar1Cost;
                            ++currentCost;
                        }
                        char1Costs[j] = aboveCharCost = currentCost;
                    }
                    if (char1Costs[i + lenDiff] > maxDistance) return -1;
                }
            } else {
                for (i = 0; i < len1; ++i) {
                    char char1 = string1[start + i];
                    int prevChar1Cost, aboveCharCost;
                    prevChar1Cost = aboveCharCost = i;
                    jStart += (i > jStartOffset) ? 1 : 0;
                    jEnd += (jEnd < len2) ? 1 : 0;
                    for (j = jStart; j < jEnd; ++j) {
                        currentCost = prevChar1Cost;
                        prevChar1Cost = char1Costs[j];
                        if (string2[start + j] != char1) {
                            if (aboveCharCost < currentCost) currentCost = aboveCharCost;
                            if (prevChar1Cost < currentCost) currentCost = prevChar1Cost;
                            ++currentCost;
                        }
                        char1Costs[j] = aboveCharCost = currentCost;
                    }
                    if (char1Costs[i + lenDiff] > maxDistance) return -1;
                }
            }
            return (currentCost <= maxDistance) ? currentCost : -1;
        }
    }
}
