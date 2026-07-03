namespace Personal.Models;

public sealed class HomeViewModel
{
    public IReadOnlyList<string> LandingCarouselImages { get; init; } = [];
    public IReadOnlyList<FeaturedSeriesItem> FeaturedSeries { get; init; } = [];
}

public sealed record FeaturedSeriesItem(
    string Id,
    string Eyebrow,
    string Title,
    string Description,
    string ImageUrl,
    string ImageAlt,
    string Orientation,
    IReadOnlyList<string> PhotoUrls);

public sealed class AlbumsViewModel
{
    public IReadOnlyList<FeaturedSeriesItem> Albums { get; init; } = [];
}

public sealed class AlbumViewModel
{
    public required FeaturedSeriesItem Album { get; init; }
}
