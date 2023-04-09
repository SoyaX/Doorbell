namespace Doorbell; 

public static class Extend {
    public static bool IsHttpUrl(this string str) {
        return str.StartsWith("http://") || str.StartsWith("https://");
    }
}
