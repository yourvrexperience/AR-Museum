namespace SoftWx.Match {
    public interface IDistance {
        double Distance(string string1, string string2);

        double Distance(string string1, string string2, double maxDistance);
    }
}
