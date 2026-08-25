using System;
using SoftWx.Match;

public class EditDistance {
    public enum DistanceAlgorithm {
        Levenshtein,
        DamerauOSA
    }
    private DistanceAlgorithm algorithm;
    private IDistance distanceComparer;
    
    public EditDistance(DistanceAlgorithm algorithm) {
        this.algorithm = algorithm;
        switch (algorithm) {
            case DistanceAlgorithm.DamerauOSA: this.distanceComparer = new DamerauOSA(); break;
            case DistanceAlgorithm.Levenshtein: this.distanceComparer = new Levenshtein(); break;
            default: throw new ArgumentException("Unknown distance algorithm.");
        }
    }

    public int Compare(string string1, string string2, int maxDistance) {
        return (int)this.distanceComparer.Distance(string1, string2, maxDistance);
    }
}
