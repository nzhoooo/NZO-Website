namespace Personal.Models;

public sealed class HomeViewModel
{
    public IReadOnlyList<string> LandingCarouselImages { get; init; } = [];
    public IReadOnlyList<FeaturedSeriesItem> FeaturedSeries { get; init; } = [];
}

public sealed record FeaturedSeriesItem(
    string Eyebrow,
    string Title,
    string Description,
    string ImageUrl,
    string ImageAlt,
    string Orientation);
