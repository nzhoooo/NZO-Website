using System.Diagnostics;
using System.Text.Json;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Personal.Models;

namespace Personal.Controllers;

public class HomeController : Controller
{
    private const string DefaultLandingBackground = "/images/lumina/hero.jpg";
    private const string CloudinaryUploadFolder = "nzo-website";
    private const string AdminUploadsPathConfigurationKey = "ADMIN_UPLOADS_PATH";
    private const string SiteSettingsPathConfigurationKey = "SITE_SETTINGS_PATH";
    private const string RailwayDataDirectory = "/data";
    private const string DashboardAdminPage = "dashboard";
    private const string SeriesAdminPage = "series";
    private const string LandingAdminPage = "landing";
    private const string LibraryAdminPage = "library";
    private const string LocalLibrarySource = "local";
    private const string CloudinaryLibrarySource = "cloudinary";
    private static readonly string[] FallbackLandingCarouselImages =
    [
        "/images/lumina/hero.jpg",
        "/images/lumina/architecture.jpg",
        "/images/lumina/nature.jpg",
        "/images/lumina/portraiture.jpg"
    ];
    private static readonly FeaturedSeriesRecord[] DefaultFeaturedSeries =
    [
        CreateDefaultSeries("nature", "Series 01", "Nature", "Unveiling the quiet drama found within the world's untamed spaces.", "/images/lumina/nature.jpg", "landscape"),
        CreateDefaultSeries("celebrations", "Series 02", "Celebrations", "Soft, luminous coverage for intimate milestones and ceremony details.", "/uploads/admin/20260530105406-c140f6906f04444e9e77bc59b9d7039e.jpg", "landscape"),
        CreateDefaultSeries("studio-notes", "Series 03", "Studio Notes", "Controlled compositions for campaigns, keepsakes, and visual archives.", "/uploads/admin/20260530104554-3602dcdd3c444ebaadc0e1f31fb86347.jpg", "landscape"),
        CreateDefaultSeries("architecture", "Series 04", "Architecture", "Exploring the mathematical beauty of the modern urban landscape.", "/images/lumina/architecture.jpg", "portrait"),
        CreateDefaultSeries("portraiture", "Series 05", "Portraiture", "Documenting the human condition through honest, light-filled moments.", "/images/lumina/portraiture.jpg", "portrait"),
        CreateDefaultSeries("editorial-light", "Series 06", "Editorial Light", "Portrait sessions shaped around movement, texture, and natural direction.", "/uploads/20260529155029-7b52b42126a34a7284fdcd1700eaf1d3.jpeg", "portrait"),
        CreateDefaultSeries("archive", "Series 07", "Archive", "Selected work-in-progress images from the evolving NZO visual library.", "/uploads/admin/20260530105406-8c6771734f214da2a27467e6ecc33fe6.jpg", "landscape"),
        CreateDefaultSeries("golden-hour", "Series 08", "Golden Hour", "Warm outdoor frames built around color, atmosphere, and honest expressions.", "/uploads/admin/20260530105406-197e8079fc8f41d79e86b4ec155d16ec.jpg", "portrait")
    ];
    private static readonly Dictionary<string, string> FallbackImageNames = new(StringComparer.Ordinal)
    {
        ["/images/lumina/hero.jpg"] = "Default hero",
        ["/images/lumina/architecture.jpg"] = "Default architecture",
        ["/images/lumina/nature.jpg"] = "Default nature",
        ["/images/lumina/portraiture.jpg"] = "Default portraiture"
    };
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public HomeController(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public IActionResult Index()
    {
        return View(new HomeViewModel
        {
            LandingCarouselImages = GetLandingCarouselImages(),
            FeaturedSeries = GetFeaturedSeries(GetSettings())
        });
    }

    public IActionResult Albums()
    {
        return View(new AlbumsViewModel
        {
            Albums = GetFeaturedSeries(GetSettings())
        });
    }

    [HttpGet("Home/Albums/{id}")]
    [HttpGet("Home/Album/{id}")]
    public IActionResult Album(string id)
    {
        var album = GetFeaturedSeries(GetSettings())
            .FirstOrDefault(series => string.Equals(series.Id, id, StringComparison.Ordinal));

        if (album is null)
        {
            return NotFound();
        }

        return View(new AlbumViewModel
        {
            Album = album
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Help()
    {
        return View();
    }

    public IActionResult Admin(
        string adminPage = DashboardAdminPage,
        string librarySource = LocalLibrarySource,
        string cloudinaryFolder = "")
    {
        return View(CreateAdminViewModel(adminPage, librarySource, cloudinaryFolder));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateFeaturedSeries(
        string title,
        string description,
        string orientation = "portrait",
        string coverImageUrl = "",
        string librarySource = LocalLibrarySource,
        string cloudinaryFolder = "")
    {
        librarySource = NormalizeLibrarySource(librarySource);
        cloudinaryFolder = NormalizeCloudinaryFolder(cloudinaryFolder);
        if (librarySource == LocalLibrarySource)
        {
            cloudinaryFolder = string.Empty;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            if (IsFetchRequest())
            {
                return BadRequest(new { message = "Enter a series title." });
            }

            TempData["AdminMessage"] = "Enter a series title.";
            return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
        }

        var settings = GetSettings();
        EnsureFeaturedSeries(settings);
        var normalizedOrientation = NormalizeSeriesOrientation(orientation);
        var normalizedCover = IsKnownSeriesImageUrl(coverImageUrl) ? coverImageUrl : string.Empty;
        var nextSeriesNumber = settings.FeaturedSeries.Count + 1;
        var record = new FeaturedSeriesRecord
        {
            Id = CreateSeriesId(title),
            Eyebrow = $"Series {nextSeriesNumber:00}",
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Orientation = normalizedOrientation,
            CoverImageUrl = normalizedCover,
            PhotoUrls = string.IsNullOrWhiteSpace(normalizedCover) ? [] : [normalizedCover]
        };

        settings.FeaturedSeries.Add(record);
        SaveSettings(settings);
        var message = "Featured series added.";
        if (IsFetchRequest())
        {
            return SeriesJson(record, message);
        }

        TempData["AdminMessage"] = message;

        return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateFeaturedSeries(
        string seriesId,
        string eyebrow,
        string title,
        string description,
        string orientation,
        string librarySource = LocalLibrarySource,
        string cloudinaryFolder = "")
    {
        librarySource = NormalizeLibrarySource(librarySource);
        cloudinaryFolder = NormalizeCloudinaryFolder(cloudinaryFolder);
        if (librarySource == LocalLibrarySource)
        {
            cloudinaryFolder = string.Empty;
        }

        var settings = GetSettings();
        var series = FindFeaturedSeries(settings, seriesId);
        if (series is null || string.IsNullOrWhiteSpace(title))
        {
            if (IsFetchRequest())
            {
                return BadRequest(new { message = "Choose a valid series to edit." });
            }

            TempData["AdminMessage"] = "Choose a valid series to edit.";
            return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
        }

        series.Eyebrow = string.IsNullOrWhiteSpace(eyebrow) ? series.Eyebrow : eyebrow.Trim();
        series.Title = title.Trim();
        series.Description = description?.Trim() ?? string.Empty;
        series.Orientation = NormalizeSeriesOrientation(orientation);
        SaveSettings(settings);
        var message = "Featured series updated.";
        if (IsFetchRequest())
        {
            return SeriesJson(series, message);
        }

        TempData["AdminMessage"] = message;

        return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddFeaturedSeriesPhoto(
        string seriesId,
        string[] imageUrl,
        string librarySource = LocalLibrarySource,
        string cloudinaryFolder = "")
    {
        librarySource = NormalizeLibrarySource(librarySource);
        cloudinaryFolder = NormalizeCloudinaryFolder(cloudinaryFolder);
        if (librarySource == LocalLibrarySource)
        {
            cloudinaryFolder = string.Empty;
        }

        var validImageUrls = (imageUrl ?? [])
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.Ordinal)
            .Where(IsKnownSeriesImageUrl)
            .ToList();

        if (validImageUrls.Count == 0)
        {
            if (IsFetchRequest())
            {
                return BadRequest(new { message = "Choose at least one valid image for the series." });
            }

            TempData["AdminMessage"] = "Choose at least one valid image for the series.";
            return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
        }

        var settings = GetSettings();
        var series = FindFeaturedSeries(settings, seriesId);
        if (series is null)
        {
            if (IsFetchRequest())
            {
                return BadRequest(new { message = "Choose a valid series." });
            }

            TempData["AdminMessage"] = "Choose a valid series.";
            return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
        }

        foreach (var validImageUrl in validImageUrls)
        {
            if (!series.PhotoUrls.Contains(validImageUrl, StringComparer.Ordinal))
            {
                series.PhotoUrls.Add(validImageUrl);
            }
        }

        if (string.IsNullOrWhiteSpace(series.CoverImageUrl))
        {
            series.CoverImageUrl = validImageUrls[0];
        }

        SaveSettings(settings);
        var message = validImageUrls.Count == 1 ? "Photo added to series." : "Photos added to series.";
        if (IsFetchRequest())
        {
            return SeriesJson(series, message);
        }

        TempData["AdminMessage"] = message;

        return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetFeaturedSeriesCover(
        string seriesId,
        string imageUrl,
        string librarySource = LocalLibrarySource,
        string cloudinaryFolder = "")
    {
        librarySource = NormalizeLibrarySource(librarySource);
        cloudinaryFolder = NormalizeCloudinaryFolder(cloudinaryFolder);
        if (librarySource == LocalLibrarySource)
        {
            cloudinaryFolder = string.Empty;
        }

        var settings = GetSettings();
        var series = FindFeaturedSeries(settings, seriesId);
        if (series is null || !IsKnownSeriesImageUrl(imageUrl))
        {
            if (IsFetchRequest())
            {
                return BadRequest(new { message = "Choose a valid series cover." });
            }

            TempData["AdminMessage"] = "Choose a valid series cover.";
            return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
        }

        if (!series.PhotoUrls.Contains(imageUrl, StringComparer.Ordinal))
        {
            series.PhotoUrls.Add(imageUrl);
        }

        series.CoverImageUrl = imageUrl;
        SaveSettings(settings);
        var message = "Series cover updated.";
        if (IsFetchRequest())
        {
            return SeriesJson(series, message);
        }

        TempData["AdminMessage"] = message;

        return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveFeaturedSeriesPhoto(
        string seriesId,
        string imageUrl,
        string librarySource = LocalLibrarySource,
        string cloudinaryFolder = "")
    {
        librarySource = NormalizeLibrarySource(librarySource);
        cloudinaryFolder = NormalizeCloudinaryFolder(cloudinaryFolder);
        if (librarySource == LocalLibrarySource)
        {
            cloudinaryFolder = string.Empty;
        }

        var settings = GetSettings();
        var series = FindFeaturedSeries(settings, seriesId);
        if (series is null)
        {
            if (IsFetchRequest())
            {
                return BadRequest(new { message = "Choose a valid series." });
            }

            TempData["AdminMessage"] = "Choose a valid series.";
            return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
        }

        series.PhotoUrls.RemoveAll(photoUrl => photoUrl == imageUrl);
        if (series.CoverImageUrl == imageUrl)
        {
            series.CoverImageUrl = series.PhotoUrls.FirstOrDefault() ?? string.Empty;
        }

        SaveSettings(settings);
        var message = "Series photo removed.";
        if (IsFetchRequest())
        {
            return SeriesJson(series, message);
        }

        TempData["AdminMessage"] = message;

        return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveFeaturedSeries(
        string seriesId,
        string librarySource = LocalLibrarySource,
        string cloudinaryFolder = "")
    {
        librarySource = NormalizeLibrarySource(librarySource);
        cloudinaryFolder = NormalizeCloudinaryFolder(cloudinaryFolder);
        if (librarySource == LocalLibrarySource)
        {
            cloudinaryFolder = string.Empty;
        }

        var settings = GetSettings();
        EnsureFeaturedSeries(settings);
        var removedCount = settings.FeaturedSeries.RemoveAll(series => series.Id == seriesId);
        if (removedCount == 0)
        {
            if (IsFetchRequest())
            {
                return BadRequest(new { message = "Choose a valid series to remove." });
            }

            TempData["AdminMessage"] = "Choose a valid series to remove.";
            return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
        }

        SaveSettings(settings);
        var message = "Featured series removed.";
        if (IsFetchRequest())
        {
            return Json(new { message, removedSeriesId = seriesId });
        }

        TempData["AdminMessage"] = message;

        return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImages(
        List<IFormFile> images,
        bool useAsLandingBackground,
        string librarySource = LocalLibrarySource,
        string cloudinaryFolder = "")
    {
        librarySource = NormalizeLibrarySource(librarySource);
        cloudinaryFolder = NormalizeCloudinaryFolder(cloudinaryFolder);
        if (librarySource == LocalLibrarySource)
        {
            cloudinaryFolder = string.Empty;
        }

        if (images.Count == 0)
        {
            TempData["AdminMessage"] = "Choose at least one image to upload.";
            return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
        }

        if (librarySource == CloudinaryLibrarySource && CreateCloudinaryClient() is null)
        {
            TempData["AdminMessage"] = "Cloudinary is not configured yet.";
            return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
        }

        var settings = GetSettings();
        var uploadedImages = new List<UploadedImageRecord>();
        var imagesToUpload = images.Where(image => image.Length > 0).ToList();

        foreach (var image in imagesToUpload)
        {
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
            {
                TempData["AdminMessage"] = "Only JPG, PNG, WEBP, and GIF images can be uploaded.";
                return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
            }
        }

        foreach (var image in imagesToUpload)
        {
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

            uploadedImages.Add(await UploadImage(image, extension, librarySource, cloudinaryFolder));
        }

        if (uploadedImages.Count == 0)
        {
            TempData["AdminMessage"] = "No image files were uploaded.";
            return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
        }

        settings.UploadedImages.InsertRange(0, uploadedImages);

        if (useAsLandingBackground)
        {
            settings.LandingBackground = uploadedImages[0].Url;
            SetCarouselSlot(settings, 0, uploadedImages[0].Url);
        }

        SaveSettings(settings);

        TempData["AdminMessage"] = useAsLandingBackground
            ? "Images uploaded. Landing background updated."
            : "Images uploaded.";

        return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateCloudinaryFolder(string folderName)
    {
        var normalizedFolder = NormalizeCloudinaryFolder(folderName);
        if (string.IsNullOrWhiteSpace(normalizedFolder))
        {
            TempData["AdminMessage"] = "Enter a folder name first.";
            return RedirectToAction(nameof(Admin), new { librarySource = CloudinaryLibrarySource });
        }

        var cloudinary = CreateCloudinaryClient();
        if (cloudinary is null)
        {
            TempData["AdminMessage"] = "Cloudinary is not configured yet.";
            return RedirectToAction(nameof(Admin), new { librarySource = CloudinaryLibrarySource });
        }

        try
        {
            TryCreateCloudinaryFolder(cloudinary, CloudinaryUploadFolder);
            cloudinary.CreateFolder(GetCloudinaryFolderPath(normalizedFolder));
            TempData["AdminMessage"] = $"Cloudinary folder \"{normalizedFolder}\" is ready.";
        }
        catch
        {
            TempData["AdminMessage"] = "Cloudinary folder could not be created.";
        }

        return RedirectToAction(nameof(Admin), new { librarySource = CloudinaryLibrarySource, cloudinaryFolder = normalizedFolder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetLandingBackground(string imageUrl, string librarySource = LocalLibrarySource, string cloudinaryFolder = "")
    {
        librarySource = NormalizeLibrarySource(librarySource);
        cloudinaryFolder = NormalizeCloudinaryFolder(cloudinaryFolder);
        if (librarySource == LocalLibrarySource)
        {
            cloudinaryFolder = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(imageUrl) || !IsKnownImageUrl(imageUrl))
        {
            TempData["AdminMessage"] = "Choose an uploaded image before setting the landing background.";
            return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
        }

        var settings = GetSettings();
        settings.LandingBackground = imageUrl;
        SetCarouselSlot(settings, 0, imageUrl);
        SaveSettings(settings);

        TempData["AdminMessage"] = "Landing background updated.";

        return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetCarouselSlide(int slideIndex, string imageUrl, string librarySource = LocalLibrarySource, string cloudinaryFolder = "")
    {
        librarySource = NormalizeLibrarySource(librarySource);
        cloudinaryFolder = NormalizeCloudinaryFolder(cloudinaryFolder);
        if (librarySource == LocalLibrarySource)
        {
            cloudinaryFolder = string.Empty;
        }
        if (slideIndex is < 0 or > 3 || string.IsNullOrWhiteSpace(imageUrl) || !IsUsableCarouselImage(imageUrl))
        {
            TempData["AdminMessage"] = "Choose a valid carousel slide image.";
            return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
        }

        var settings = GetSettings();
        SetCarouselSlot(settings, slideIndex, imageUrl);

        if (slideIndex == 0)
        {
            settings.LandingBackground = imageUrl;
        }

        SaveSettings(settings);

        TempData["AdminMessage"] = $"Slide {slideIndex + 1} updated.";

        return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveUploadedImage(string imageUrl, string fileName, string librarySource = LocalLibrarySource, string cloudinaryFolder = "")
    {
        librarySource = NormalizeLibrarySource(librarySource);
        cloudinaryFolder = NormalizeCloudinaryFolder(cloudinaryFolder);
        if (librarySource == LocalLibrarySource)
        {
            cloudinaryFolder = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            TempData["AdminMessage"] = "Choose an image to remove.";
            return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
        }

        var settings = GetSettings();
        var removed = false;

        if (IsCloudinaryImageUrl(imageUrl))
        {
            removed = RemoveCloudinaryImage(fileName);
        }
        else if (imageUrl.StartsWith("/uploads/admin/", StringComparison.Ordinal))
        {
            var localFileName = Path.GetFileName(imageUrl);
            var localFilePath = Path.Combine(GetUploadDirectory(), localFileName);
            if (System.IO.File.Exists(localFilePath))
            {
                System.IO.File.Delete(localFilePath);
                removed = true;
            }
        }

        var removedSavedRecords = settings.UploadedImages.RemoveAll(image => image.Url == imageUrl || image.FileName == fileName);
        if (removed || removedSavedRecords > 0)
        {
            ClearImageReferences(settings, imageUrl);
            SaveSettings(settings);
            TempData["AdminMessage"] = "Image removed.";
        }
        else
        {
            TempData["AdminMessage"] = "Image could not be removed.";
        }

        return RedirectToAction(nameof(Admin), new { librarySource, cloudinaryFolder });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private AdminViewModel CreateAdminViewModel(string adminPage, string librarySource, string cloudinaryFolder)
    {
        var settings = GetSettings();
        var normalizedAdminPage = NormalizeAdminPage(adminPage);
        var normalizedLibrarySource = NormalizeLibrarySource(librarySource);
        var normalizedCloudinaryFolder = normalizedLibrarySource == CloudinaryLibrarySource
            ? NormalizeCloudinaryFolder(cloudinaryFolder)
            : string.Empty;
        var uploadedImages = GetUploadedImages(settings, includeCloudinaryLibrary: true, cloudinaryFolder: normalizedCloudinaryFolder);

        return new AdminViewModel
        {
            CurrentAdminPage = normalizedAdminPage,
            CurrentLandingBackground = settings.LandingBackground,
            LandingCarouselImages = GetLandingCarouselImages(settings, includeCloudinaryLibrary: true),
            CarouselImageOptions = GetCarouselImageOptions(settings, includeCloudinaryLibrary: true),
            FeaturedSeries = GetAdminFeaturedSeries(settings),
            SeriesImageOptions = GetSeriesImageOptions(settings, includeCloudinaryLibrary: true),
            UploadedImages = uploadedImages
                .Where(image => image.Source == normalizedLibrarySource)
                .ToList(),
            CurrentLibrarySource = normalizedLibrarySource,
            CurrentCloudinaryFolder = normalizedCloudinaryFolder,
            CloudinaryFolders = GetCloudinaryFolders()
        };
    }

    private IReadOnlyList<FeaturedSeriesItem> GetFeaturedSeries(SiteSettings settings)
    {
        EnsureFeaturedSeries(settings);

        return settings.FeaturedSeries
            .Where(series => !string.IsNullOrWhiteSpace(series.Title))
            .Select(series =>
            {
                var coverImageUrl = GetSeriesCoverImageUrl(series);
                return new FeaturedSeriesItem(
                    series.Id,
                    series.Eyebrow,
                    series.Title,
                    series.Description,
                    coverImageUrl,
                    $"{series.Title} series cover",
                    NormalizeSeriesOrientation(series.Orientation),
                    series.PhotoUrls
                        .Where(photoUrl => !string.IsNullOrWhiteSpace(photoUrl))
                        .Distinct(StringComparer.Ordinal)
                        .ToList());
            })
            .ToList();
    }

    private static IReadOnlyList<AdminFeaturedSeriesViewModel> GetAdminFeaturedSeries(SiteSettings settings)
    {
        EnsureFeaturedSeries(settings);

        return settings.FeaturedSeries
            .Select(series => new AdminFeaturedSeriesViewModel
            {
                Id = series.Id,
                Eyebrow = series.Eyebrow,
                Title = series.Title,
                Description = series.Description,
                Orientation = NormalizeSeriesOrientation(series.Orientation),
                CoverImageUrl = GetSeriesCoverImageUrl(series),
                PhotoUrls = series.PhotoUrls
                    .Where(photoUrl => !string.IsNullOrWhiteSpace(photoUrl))
                    .Distinct(StringComparer.Ordinal)
                    .ToList()
            })
            .ToList();
    }

    private IActionResult SeriesJson(FeaturedSeriesRecord series, string message)
    {
        series.Orientation = NormalizeSeriesOrientation(series.Orientation);
        series.PhotoUrls = series.PhotoUrls
            .Where(photoUrl => !string.IsNullOrWhiteSpace(photoUrl))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return Json(new
        {
            message,
            series = new
            {
                series.Id,
                series.Eyebrow,
                series.Title,
                series.Description,
                Orientation = series.Orientation,
                CoverImageUrl = GetSeriesCoverImageUrl(series),
                PhotoUrls = series.PhotoUrls
            }
        });
    }

    private bool IsFetchRequest()
    {
        return string.Equals(Request.Headers["X-Requested-With"].ToString(), "fetch", StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyList<CarouselImageOption> GetSeriesImageOptions(SiteSettings settings, bool includeCloudinaryLibrary = false)
    {
        return GetCarouselImageOptions(settings, includeCloudinaryLibrary)
            .Concat(DefaultFeaturedSeries.Select(series => new CarouselImageOption
            {
                Url = GetSeriesCoverImageUrl(series),
                DisplayName = series.Title
            }))
            .Where(option => !string.IsNullOrWhiteSpace(option.Url))
            .GroupBy(option => option.Url, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    private IReadOnlyList<string> GetLandingCarouselImages()
    {
        return GetLandingCarouselImages(GetSettings());
    }

    private IReadOnlyList<string> GetLandingCarouselImages(SiteSettings settings, bool includeCloudinaryLibrary = false)
    {
        var fallbackPool = new[] { settings.LandingBackground }
            .Concat(GetUploadedImages(settings, includeCloudinaryLibrary).Select(image => image.Url))
            .Concat(FallbackLandingCarouselImages)
            .Where(IsUsableCarouselImage)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var carouselImages = new List<string>();
        for (var index = 0; index < 4; index++)
        {
            var configuredImage = settings.LandingCarouselImages.Count > index
                ? settings.LandingCarouselImages[index]
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(configuredImage) && IsUsableCarouselImage(configuredImage))
            {
                carouselImages.Add(configuredImage);
                continue;
            }

            var fallbackImage = fallbackPool.FirstOrDefault(image => !carouselImages.Contains(image, StringComparer.Ordinal));
            if (fallbackImage is not null)
            {
                carouselImages.Add(fallbackImage);
            }
        }

        return carouselImages;
    }

    private IReadOnlyList<CarouselImageOption> GetCarouselImageOptions(SiteSettings settings, bool includeCloudinaryLibrary = false)
    {
        var uploadedOptions = GetUploadedImages(settings, includeCloudinaryLibrary).Select(image => new CarouselImageOption
        {
            Url = image.Url,
            DisplayName = image.DisplayName
        });

        var fallbackOptions = FallbackLandingCarouselImages.Select(imageUrl => new CarouselImageOption
        {
            Url = imageUrl,
            DisplayName = FallbackImageNames[imageUrl]
        });

        return uploadedOptions
            .Concat(fallbackOptions)
            .GroupBy(option => option.Url, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    private bool IsUsableCarouselImage(string imageUrl)
    {
        if (FallbackLandingCarouselImages.Contains(imageUrl, StringComparer.Ordinal))
        {
            return true;
        }

        return IsKnownImageUrl(imageUrl);
    }

    private IReadOnlyList<UploadedImageViewModel> GetUploadedImages(
        SiteSettings settings,
        bool includeCloudinaryLibrary = false,
        string cloudinaryFolder = "")
    {
        var normalizedCloudinaryFolder = NormalizeCloudinaryFolder(cloudinaryFolder);
        var savedImages = settings.UploadedImages
            .Where(image => !string.IsNullOrWhiteSpace(image.Url))
            .Select(image => new UploadedImageViewModel
            {
                FileName = image.FileName,
                Url = image.Url,
                DisplayName = string.IsNullOrWhiteSpace(image.DisplayName) ? image.FileName : image.DisplayName,
                Source = IsCloudinaryImageUrl(image.Url) ? CloudinaryLibrarySource : LocalLibrarySource,
                CreatedAt = image.CreatedAt
            })
            .Where(image => image.Source != CloudinaryLibrarySource || IsCloudinaryAssetInFolder(image.FileName, normalizedCloudinaryFolder));
        IReadOnlyList<UploadedImageViewModel> cloudinaryImages = includeCloudinaryLibrary
            ? GetCloudinaryUploadedImages(settings, normalizedCloudinaryFolder)
            : [];

        var uploadDirectory = GetUploadDirectory();
        if (!Directory.Exists(uploadDirectory))
        {
            return savedImages
                .Concat(cloudinaryImages)
                .DistinctBy(image => image.Url)
                .OrderByDescending(image => image.CreatedAt)
                .ToList();
        }

        var localImages = Directory
            .EnumerateFiles(uploadDirectory)
            .Where(file => AllowedImageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
            .Select(file =>
            {
                var fileName = Path.GetFileName(file);
                return new UploadedImageViewModel
                {
                    FileName = fileName,
                    Url = $"/uploads/admin/{fileName}",
                    DisplayName = fileName,
                    Source = LocalLibrarySource,
                    CreatedAt = System.IO.File.GetCreationTimeUtc(file)
                };
            })
            .Where(image => settings.UploadedImages.All(saved => saved.Url != image.Url));

        return savedImages
            .Concat(cloudinaryImages)
            .Concat(localImages)
            .DistinctBy(image => image.Url)
            .OrderByDescending(image => image.CreatedAt)
            .ToList();
    }

    private IReadOnlyList<UploadedImageViewModel> GetCloudinaryUploadedImages(SiteSettings settings, string cloudinaryFolder = "")
    {
        var cloudinary = CreateCloudinaryClient();
        if (cloudinary is null)
        {
            return [];
        }

        try
        {
            var cloudinaryResources = new List<Resource>();
            string? nextCursor = null;
            var folderPath = GetCloudinaryFolderPath(NormalizeCloudinaryFolder(cloudinaryFolder));

            do
            {
                var listedResources = cloudinary.ListResourcesByPrefix(folderPath, "upload", nextCursor);
                cloudinaryResources.AddRange(listedResources.Resources);
                nextCursor = listedResources.NextCursor;
            }
            while (!string.IsNullOrWhiteSpace(nextCursor) && cloudinaryResources.Count < 1000);

            return cloudinaryResources
                .Where(resource => !string.IsNullOrWhiteSpace(resource.SecureUrl?.ToString() ?? resource.Url?.ToString()))
                .Select(resource =>
                {
                    var resourceUrl = resource.SecureUrl?.ToString() ?? resource.Url?.ToString() ?? string.Empty;
                    var publicId = resource.PublicId ?? resourceUrl;
                    var displayName = settings.UploadedImages
                        .FirstOrDefault(image => image.FileName == publicId || image.Url == resourceUrl)?
                        .DisplayName;

                    return new UploadedImageViewModel
                    {
                        FileName = publicId,
                        Url = resourceUrl,
                        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Path.GetFileName(publicId) : displayName,
                        Source = CloudinaryLibrarySource,
                        CreatedAt = ParseCloudinaryCreatedAt(resource.CreatedAt)
                    };
                })
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private IReadOnlyList<CloudinaryFolderViewModel> GetCloudinaryFolders()
    {
        var folders = new List<CloudinaryFolderViewModel>
        {
            new()
            {
                Name = "Root",
                Folder = string.Empty,
                Path = CloudinaryUploadFolder,
                IsRoot = true
            }
        };

        var cloudinary = CreateCloudinaryClient();
        if (cloudinary is null)
        {
            return folders;
        }

        try
        {
            var listedFolders = cloudinary.SubFolders(CloudinaryUploadFolder, new GetFoldersParams
            {
                MaxResults = 100
            });

            folders.AddRange(listedFolders.Folders
                .Where(folder => !string.IsNullOrWhiteSpace(folder.Name))
                .Select(folder =>
                {
                    var normalizedFolder = NormalizeCloudinaryFolder(folder.Name);
                    return new CloudinaryFolderViewModel
                    {
                        Name = folder.Name,
                        Folder = normalizedFolder,
                        Path = folder.Path,
                        IsRoot = false
                    };
                })
                .Where(folder => !string.IsNullOrWhiteSpace(folder.Folder))
                .OrderBy(folder => folder.Name, StringComparer.OrdinalIgnoreCase));
        }
        catch
        {
            return folders;
        }

        return folders;
    }

    private SiteSettings GetSettings()
    {
        var settingsPath = GetSettingsPath();
        if (!System.IO.File.Exists(settingsPath))
        {
            return new SiteSettings();
        }

        try
        {
            var json = System.IO.File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<SiteSettings>(json) ?? new SiteSettings();
        }
        catch (JsonException)
        {
            return new SiteSettings();
        }
    }

    private void SaveSettings(SiteSettings settings)
    {
        Directory.CreateDirectory(GetSettingsDirectory());
        System.IO.File.WriteAllText(GetSettingsPath(), JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static void SetCarouselSlot(SiteSettings settings, int slideIndex, string imageUrl)
    {
        while (settings.LandingCarouselImages.Count < 4)
        {
            settings.LandingCarouselImages.Add(string.Empty);
        }

        settings.LandingCarouselImages[slideIndex] = imageUrl;
    }

    private static void ClearImageReferences(SiteSettings settings, string imageUrl)
    {
        if (settings.LandingBackground == imageUrl)
        {
            settings.LandingBackground = DefaultLandingBackground;
        }

        for (var index = 0; index < settings.LandingCarouselImages.Count; index++)
        {
            if (settings.LandingCarouselImages[index] == imageUrl)
            {
                settings.LandingCarouselImages[index] = string.Empty;
            }
        }

        foreach (var series in settings.FeaturedSeries)
        {
            series.PhotoUrls.RemoveAll(photoUrl => photoUrl == imageUrl);
            if (series.CoverImageUrl == imageUrl)
            {
                series.CoverImageUrl = series.PhotoUrls.FirstOrDefault() ?? string.Empty;
            }
        }
    }

    private static FeaturedSeriesRecord CreateDefaultSeries(
        string id,
        string eyebrow,
        string title,
        string description,
        string coverImageUrl,
        string orientation)
    {
        return new FeaturedSeriesRecord
        {
            Id = id,
            Eyebrow = eyebrow,
            Title = title,
            Description = description,
            Orientation = NormalizeSeriesOrientation(orientation),
            CoverImageUrl = coverImageUrl,
            PhotoUrls = [coverImageUrl]
        };
    }

    private static void EnsureFeaturedSeries(SiteSettings settings)
    {
        if (settings.FeaturedSeries.Count == 0)
        {
            settings.FeaturedSeries = DefaultFeaturedSeries
                .Select(CloneFeaturedSeries)
                .ToList();
            return;
        }

        foreach (var series in settings.FeaturedSeries)
        {
            if (string.IsNullOrWhiteSpace(series.Id))
            {
                series.Id = CreateSeriesId(series.Title);
            }

            series.Orientation = NormalizeSeriesOrientation(series.Orientation);
            series.PhotoUrls = series.PhotoUrls
                .Where(photoUrl => !string.IsNullOrWhiteSpace(photoUrl))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (!string.IsNullOrWhiteSpace(series.CoverImageUrl) &&
                !series.PhotoUrls.Contains(series.CoverImageUrl, StringComparer.Ordinal))
            {
                series.PhotoUrls.Insert(0, series.CoverImageUrl);
            }
        }
    }

    private static FeaturedSeriesRecord CloneFeaturedSeries(FeaturedSeriesRecord series)
    {
        return new FeaturedSeriesRecord
        {
            Id = series.Id,
            Eyebrow = series.Eyebrow,
            Title = series.Title,
            Description = series.Description,
            Orientation = NormalizeSeriesOrientation(series.Orientation),
            CoverImageUrl = series.CoverImageUrl,
            PhotoUrls = series.PhotoUrls.ToList()
        };
    }

    private static FeaturedSeriesRecord? FindFeaturedSeries(SiteSettings settings, string seriesId)
    {
        EnsureFeaturedSeries(settings);
        return settings.FeaturedSeries.FirstOrDefault(series => series.Id == seriesId);
    }

    private static string NormalizeSeriesOrientation(string? orientation)
    {
        return string.Equals(orientation, "landscape", StringComparison.OrdinalIgnoreCase)
            ? "landscape"
            : "portrait";
    }

    private static string GetSeriesCoverImageUrl(FeaturedSeriesRecord series)
    {
        if (!string.IsNullOrWhiteSpace(series.CoverImageUrl))
        {
            return series.CoverImageUrl;
        }

        return series.PhotoUrls.FirstOrDefault(photoUrl => !string.IsNullOrWhiteSpace(photoUrl)) ?? FallbackLandingCarouselImages[0];
    }

    private static string CreateSeriesId(string title)
    {
        var slug = NormalizeCloudinaryFolder(title);
        return string.IsNullOrWhiteSpace(slug)
            ? Guid.NewGuid().ToString("N")
            : $"{slug}-{Guid.NewGuid():N}"[..Math.Min(slug.Length + 9, 40)];
    }

    private bool RemoveCloudinaryImage(string publicId)
    {
        var cloudinary = CreateCloudinaryClient();
        if (cloudinary is null || string.IsNullOrWhiteSpace(publicId))
        {
            return false;
        }

        try
        {
            var result = cloudinary.Destroy(new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image,
                Invalidate = true
            });

            return string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(result.Result, "not found", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static void TryCreateCloudinaryFolder(Cloudinary cloudinary, string folderPath)
    {
        try
        {
            cloudinary.CreateFolder(folderPath);
        }
        catch
        {
            // The parent folder may already exist, which is fine for nested uploads.
        }
    }

    private static DateTimeOffset ParseCloudinaryCreatedAt(string? createdAt)
    {
        return DateTimeOffset.TryParse(createdAt, out var parsed)
            ? parsed
            : DateTimeOffset.UtcNow;
    }

    private static string NormalizeLibrarySource(string? librarySource)
    {
        return string.Equals(librarySource, CloudinaryLibrarySource, StringComparison.OrdinalIgnoreCase)
            ? CloudinaryLibrarySource
            : LocalLibrarySource;
    }

    private static string NormalizeAdminPage(string? adminPage)
    {
        return adminPage?.ToLowerInvariant() switch
        {
            SeriesAdminPage => SeriesAdminPage,
            LandingAdminPage => LandingAdminPage,
            LibraryAdminPage => LibraryAdminPage,
            _ => DashboardAdminPage
        };
    }

    private static string NormalizeCloudinaryFolder(string? folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return string.Empty;
        }

        var normalized = new string(folderName
            .Trim()
            .Select(character =>
            {
                if (char.IsLetterOrDigit(character))
                {
                    return char.ToLowerInvariant(character);
                }

                return character is '-' or '_' or ' ' ? '-' : '\0';
            })
            .Where(character => character != '\0')
            .ToArray());

        return string.Join("-", normalized.Split('-', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string GetCloudinaryFolderPath(string cloudinaryFolder)
    {
        return string.IsNullOrWhiteSpace(cloudinaryFolder)
            ? CloudinaryUploadFolder
            : $"{CloudinaryUploadFolder}/{cloudinaryFolder}";
    }

    private static bool IsCloudinaryAssetInFolder(string publicId, string cloudinaryFolder)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return false;
        }

        var folderPath = GetCloudinaryFolderPath(cloudinaryFolder);
        return publicId.StartsWith($"{folderPath}/", StringComparison.Ordinal);
    }

    private bool IsKnownImageUrl(string imageUrl)
    {
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
        {
            return GetSettings().UploadedImages.Any(image => image.Url == imageUrl) || IsCloudinaryDeliveryUrl(uri);
        }

        if (!imageUrl.StartsWith("/uploads/admin/", StringComparison.Ordinal))
        {
            return false;
        }

        var fileName = Path.GetFileName(imageUrl);
        var filePath = Path.Combine(GetUploadDirectory(), fileName);

        return System.IO.File.Exists(filePath);
    }

    private bool IsKnownSeriesImageUrl(string imageUrl)
    {
        if (FallbackLandingCarouselImages.Contains(imageUrl, StringComparer.Ordinal) ||
            DefaultFeaturedSeries.Any(series => series.CoverImageUrl == imageUrl || series.PhotoUrls.Contains(imageUrl, StringComparer.Ordinal)))
        {
            return true;
        }

        if (imageUrl.StartsWith("/uploads/", StringComparison.Ordinal) && !imageUrl.StartsWith("/uploads/admin/", StringComparison.Ordinal))
        {
            var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            return System.IO.File.Exists(Path.Combine(_environment.WebRootPath, relativePath));
        }

        return IsKnownImageUrl(imageUrl);
    }

    private bool IsCloudinaryDeliveryUrl(Uri uri)
    {
        var cloudName = GetConfiguredCloudName();
        return !string.IsNullOrWhiteSpace(cloudName) &&
               string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase) &&
               uri.AbsolutePath.Contains($"/{cloudName}/image/upload/", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCloudinaryImageUrl(string imageUrl)
    {
        return Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) && IsCloudinaryDeliveryUrl(uri);
    }

    private async Task<UploadedImageRecord> UploadImage(
        IFormFile image,
        string extension,
        string librarySource,
        string cloudinaryFolder)
    {
        var cloudinary = librarySource == CloudinaryLibrarySource ? CreateCloudinaryClient() : null;

        if (cloudinary is not null)
        {
            await using var imageStream = image.OpenReadStream();
            var uploadResult = await cloudinary.UploadAsync(new ImageUploadParams
            {
                File = new FileDescription(image.FileName, imageStream),
                Folder = GetCloudinaryFolderPath(cloudinaryFolder),
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            });

            if (uploadResult.Error is not null)
            {
                throw new InvalidOperationException(uploadResult.Error.Message);
            }

            return new UploadedImageRecord
            {
                FileName = uploadResult.PublicId,
                Url = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url.ToString(),
                DisplayName = image.FileName,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        var uploadDirectory = GetUploadDirectory();
        Directory.CreateDirectory(uploadDirectory);

        var fileName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadDirectory, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await image.CopyToAsync(stream);

        return new UploadedImageRecord
        {
            FileName = fileName,
            Url = $"/uploads/admin/{fileName}",
            DisplayName = image.FileName,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private Cloudinary? CreateCloudinaryClient()
    {
        var cloudinaryUrl = _configuration["CLOUDINARY_URL"];
        if (!string.IsNullOrWhiteSpace(cloudinaryUrl))
        {
            var cloudinary = new Cloudinary(cloudinaryUrl);
            cloudinary.Api.Secure = true;
            return cloudinary;
        }

        var cloudName = _configuration["CLOUDINARY_CLOUD_NAME"];
        var apiKey = _configuration["CLOUDINARY_API_KEY"];
        var apiSecret = _configuration["CLOUDINARY_API_SECRET"];

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            return null;
        }

        return new Cloudinary(new Account(cloudName, apiKey, apiSecret))
        {
            Api = { Secure = true }
        };
    }

    private string? GetConfiguredCloudName()
    {
        var cloudName = _configuration["CLOUDINARY_CLOUD_NAME"];
        if (!string.IsNullOrWhiteSpace(cloudName))
        {
            return cloudName;
        }

        var cloudinaryUrl = _configuration["CLOUDINARY_URL"];
        return Uri.TryCreate(cloudinaryUrl, UriKind.Absolute, out var uri)
            ? uri.Host
            : null;
    }

    private string GetUploadDirectory()
    {
        var configuredPath = GetConfiguredStoragePath(AdminUploadsPathConfigurationKey);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        if (ShouldUseRailwayDataDirectory())
        {
            return Path.Combine(RailwayDataDirectory, "uploads", "admin");
        }

        return Path.Combine(_environment.WebRootPath, "uploads", "admin");
    }

    private string GetSettingsDirectory()
    {
        var configuredPath = GetConfiguredStoragePath(SiteSettingsPathConfigurationKey);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetDirectoryName(configuredPath) ?? _environment.ContentRootPath;
        }

        if (ShouldUseRailwayDataDirectory())
        {
            return RailwayDataDirectory;
        }

        return Path.Combine(_environment.ContentRootPath, "App_Data");
    }

    private string GetSettingsPath()
    {
        var configuredPath = GetConfiguredStoragePath(SiteSettingsPathConfigurationKey);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        return Path.Combine(GetSettingsDirectory(), "site-settings.json");
    }

    private string GetConfiguredStoragePath(string configurationKey)
    {
        var configuredPath = _configuration[configurationKey];
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return string.Empty;
        }

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(_environment.ContentRootPath, configuredPath);
    }

    private bool ShouldUseRailwayDataDirectory()
    {
        return !string.IsNullOrWhiteSpace(_configuration["RAILWAY_ENVIRONMENT"]) ||
               !string.IsNullOrWhiteSpace(_configuration["RAILWAY_SERVICE_ID"]) ||
               _environment.IsProduction();
    }
}
