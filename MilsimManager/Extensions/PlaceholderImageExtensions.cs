namespace MilsimManager.Extensions;

public static class PlaceholderImageExtensions {
    public static string GetImageUrl(string seed, int width = 1000, int? height = null) => $"https://picsum.photos/seed/{seed}/{width}" + (height is null ? string.Empty : $"/{height}");
}
