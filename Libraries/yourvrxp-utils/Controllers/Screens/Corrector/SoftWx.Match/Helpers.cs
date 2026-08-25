using System.Runtime.CompilerServices;

namespace SoftWx.Match {
    internal static class Helpers {
        public static int NullDistanceResults(string string1, string string2, double maxDistance) {
            if (string1 == null) return (string2 == null) ? 0 : (string2.Length <= maxDistance) ? string2.Length : -1;
            return (string1.Length <= maxDistance) ? string1.Length : -1;
        }

        public static int NullSimilarityResults(string string1, string string2, double minSimilarity) {
            return (string1 == null && string2 == null) ? 1 : (0 <= minSimilarity) ? 0 : -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrefixSuffixPrep(string string1, string string2, out int len1, out int len2, out int start) {
            len2 = string2.Length;
            len1 = string1.Length;

            while (len1 != 0 && string1[len1 - 1] == string2[len2 - 1]) {
                len1 = len1 - 1; len2 = len2 - 1;
            }
            start = 0;
            while (start != len1 && string1[start] == string2[start]) start++;
            if (start != 0) {
                len2 -= start;
                len1 -= start;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ToSimilarity(this int distance, int length) {
            return (distance < 0) ? -1 : 1 - (distance / (double)length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToDistance(this double similarity, int length) {
            return (int)((length * (1 - similarity)) + .0000000001);
        }
    }
}
