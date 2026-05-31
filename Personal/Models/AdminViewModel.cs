namespace Personal.Models;

public sealed class AdminViewModel
{
    public string CurrentLandingBackground { get; init; } = "/images/lumina/hero.jpg";

    public IReadOnlyList<string> LandingCarouselImages { get; init; } = [];

    public IReadOnlyList<CarouselImageOption> CarouselImageOptions { get; init; } = [];

    public IReadOnlyList<UploadedImageViewModel> UploadedImages { get; init; } = [];

    public string CurrentLibrarySource { get; init; } = "local";

    public string CurrentCloudinaryFolder { get; init; } = string.Empty;

    public IReadOnlyList<CloudinaryFolderViewModel> CloudinaryFolders { get; init; } = [];
}

public sealed class UploadedImageViewModel
{
    public required string FileName { get; init; }

    public required string Url { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string Source { get; init; } = "local";

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CloudinaryFolderViewModel
{
    public required string Name { get; init; }

    public required string Folder { get; init; }

    public required string Path { get; init; }

    public bool IsRoot { get; init; }
}

public sealed class SiteSettings
{
    public string LandingBackground { get; set; } = "/images/lumina/hero.jpg";

    public List<string> LandingCarouselImages { get; set; } = [];

    public List<UploadedImageRecord> UploadedImages { get; set; } = [];
}

public sealed class CarouselImageOption
{
    public required string Url { get; init; }

    public required string DisplayName { get; init; }
}

public sealed class UploadedImageRecord
{
    public string FileName { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
