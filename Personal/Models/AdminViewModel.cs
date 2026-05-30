namespace Personal.Models;

public sealed class AdminViewModel
{
    public string CurrentLandingBackground { get; init; } = "/images/lumina/hero.jpg";

    public IReadOnlyList<string> LandingCarouselImages { get; init; } = [];

    public IReadOnlyList<UploadedImageViewModel> UploadedImages { get; init; } = [];
}

public sealed class UploadedImageViewModel
{
    public required string FileName { get; init; }

    public required string Url { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class SiteSettings
{
    public string LandingBackground { get; set; } = "/images/lumina/hero.jpg";
}
