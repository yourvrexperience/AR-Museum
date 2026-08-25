using System;
using System.Runtime.CompilerServices;

namespace SoftWx.Match {
    public class DamerauOSA : IDistance {
        private int[] baseChar1Costs;
        private int[] basePrevChar1Costs;

        public DamerauOSA() {
            this.baseChar1Costs = new int[0];
            this.basePrevChar1Costs = new int[0];
        }

        public DamerauOSA(int expectedMaxStringLength) {
            this.baseChar1Costs = new int[expectedMaxStringLength];
            this.basePrevChar1Costs = new int[expectedMaxStringLength];
        }

        public double Distance(string string1, string string2) {
            if (string1 == null) return (string2 ?? "").Length;
            if (string2 == null) return string1.Length;

            if (string1.Length > string2.Length) { var t = string1; string1 = string2; string2 = t; }

            int len1, len2, start;
            Helpers.PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return len2;

            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            return Distance(string1, string2, len1, len2, start, this.baseChar1Costs, this.basePrevChar1Costs);
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

            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            if (iMaxDistance < len2) {
                return Distance(string1, string2, len1, len2, start, iMaxDistance, this.baseChar1Costs, this.basePrevChar1Costs);
            }
            return Distance(string1, string2, len1, len2, start, this.baseChar1Costs, this.basePrevChar1Costs);
        }

       public double Similarity(string string1, string string2) {
            if (string1 == null) return (string2 == null) ? 1 : 0;
            if (string2 == null) return 0;

            if (string1.Length > string2.Length) { var t = string1; string1 = string2; string2 = t; }

            int len1, len2, start;
            Helpers.PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return 1.0;

            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            return Distance(string1, string2, len1, len2, start, this.baseChar1Costs, this.basePrevChar1Costs)
                .ToSimilarity(string2.Length);
        }

        public double Similarity(string string1, string string2, double minSimilarity) {
            if (minSimilarity < 0 || minSimilarity > 1) throw new ArgumentException("minSimilarity must be in range 0 to 1.0");
            if (string1 == null || string2 == null) return Helpers.NullSimilarityResults(string1, string2, minSimilarity);

            if (string1.Length > string2.Length) { var t = string1; string1 = string2; string2 = t; }

            int iMaxDistance = minSimilarity.ToDistance(string2.Length);
            if (string2.Length - string1.Length > iMaxDistance) return -1;
            if (iMaxDistance <= 0) return (string1 == string2) ? 1 : -1;

            int len1, len2, start;
            Helpers.PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return 1.0;

            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            if (iMaxDistance < len2) {
                return Distance(string1, string2, len1, len2, start, iMaxDistance, this.baseChar1Costs, this.basePrevChar1Costs)
                    .ToSimilarity(string2.Length);
            }
            return Distance(string1, string2, len1, len2, start, this.baseChar1Costs, this.basePrevChar1Costs)
                .ToSimilarity(string2.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Distance(string string1, string string2, int len1, int len2, int start, int[] char1Costs, int[] prevChar1Costs) {
            int j;
            for (j = 0; j < len2;) char1Costs[j] = ++j;
            char char1 = ' ';
            int currentCost = 0;
            for (int i = 0; i < len1; ++i) {
                char prevChar1 = char1;
                char1 = string1[start + i];
                char char2 = ' ';
                int leftCharCost, aboveCharCost;
                leftCharCost = aboveCharCost = i;
                int nextTransCost = 0;
                for (j = 0; j < len2; ++j) {
                    int thisTransCost = nextTransCost;
                    nextTransCost = prevChar1Costs[j];
                    prevChar1Costs[j] = currentCost = leftCharCost;
                    leftCharCost = char1Costs[j];
                    char prevChar2 = char2;
                    char2 = string2[start + j];
                    if (char1 != char2) {
                        if (aboveCharCost < currentCost) currentCost = aboveCharCost;
                        if (leftCharCost < currentCost) currentCost = leftCharCost;
                        ++currentCost;
                        if ((i != 0) && (j != 0)
                            && (char1 == prevChar2)
                            && (prevChar1 == char2)
                            && (thisTransCost + 1 < currentCost)) { 
                            currentCost = thisTransCost + 1;
                        }
                    }
                    char1Costs[j] = aboveCharCost = currentCost;
                }
            }
            return currentCost;
        }

        internal static int Distance(string string1, string string2, int len1, int len2, int start, int maxDistance, int[] char1Costs, int[] prevChar1Costs) {
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
            char char1 = ' ';
            int currentCost = 0;
            for (i = 0; i < len1; ++i) {
                char prevChar1 = char1;
                char1 = string1[start + i];
                char char2 = ' ';
                int leftCharCost, aboveCharCost;
                leftCharCost = aboveCharCost = i;
                int nextTransCost = 0;
                jStart += (i > jStartOffset) ? 1 : 0;
                jEnd += (jEnd < len2) ? 1 : 0;
                for (j = jStart; j < jEnd; ++j) {
                    int thisTransCost = nextTransCost;
                    nextTransCost = prevChar1Costs[j];
                    prevChar1Costs[j] = currentCost = leftCharCost;
                    leftCharCost = char1Costs[j];
                    char prevChar2 = char2;
                    char2 = string2[start + j];
                    if (char1 != char2) {
                        if (aboveCharCost < currentCost) currentCost = aboveCharCost;
                        if (leftCharCost < currentCost) currentCost = leftCharCost;
                        ++currentCost;
                        if ((i != 0) && (j != 0)
                            && (char1 == prevChar2)
                            && (prevChar1 == char2)
                            && (thisTransCost + 1 < currentCost)) {
                            currentCost = thisTransCost + 1;
                        }
                    }
                    char1Costs[j] = aboveCharCost = currentCost;
                }
                if (char1Costs[i + lenDiff] > maxDistance) return -1;
            }
            return (currentCost <= maxDistance) ? currentCost : -1;
        }
    }
}
