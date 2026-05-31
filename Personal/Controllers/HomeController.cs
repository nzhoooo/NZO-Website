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
    private static readonly string[] FallbackLandingCarouselImages =
    [
        "/images/lumina/hero.jpg",
        "/images/lumina/architecture.jpg",
        "/images/lumina/nature.jpg",
        "/images/lumina/portraiture.jpg"
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
            LandingCarouselImages = GetLandingCarouselImages()
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Admin()
    {
        return View(CreateAdminViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImages(List<IFormFile> images, bool useAsLandingBackground)
    {
        if (images.Count == 0)
        {
            TempData["AdminMessage"] = "Choose at least one image to upload.";
            return RedirectToAction(nameof(Admin));
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
                return RedirectToAction(nameof(Admin));
            }
        }

        foreach (var image in imagesToUpload)
        {
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

            uploadedImages.Add(await UploadImage(image, extension));
        }

        if (uploadedImages.Count == 0)
        {
            TempData["AdminMessage"] = "No image files were uploaded.";
            return RedirectToAction(nameof(Admin));
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

        return RedirectToAction(nameof(Admin));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetLandingBackground(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !IsKnownImageUrl(imageUrl))
        {
            TempData["AdminMessage"] = "Choose an uploaded image before setting the landing background.";
            return RedirectToAction(nameof(Admin));
        }

        var settings = GetSettings();
        settings.LandingBackground = imageUrl;
        SetCarouselSlot(settings, 0, imageUrl);
        SaveSettings(settings);

        TempData["AdminMessage"] = "Landing background updated.";

        return RedirectToAction(nameof(Admin));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetCarouselSlide(int slideIndex, string imageUrl)
    {
        if (slideIndex is < 0 or > 3 || string.IsNullOrWhiteSpace(imageUrl) || !IsUsableCarouselImage(imageUrl))
        {
            TempData["AdminMessage"] = "Choose a valid carousel slide image.";
            return RedirectToAction(nameof(Admin));
        }

        var settings = GetSettings();
        SetCarouselSlot(settings, slideIndex, imageUrl);

        if (slideIndex == 0)
        {
            settings.LandingBackground = imageUrl;
        }

        SaveSettings(settings);

        TempData["AdminMessage"] = $"Slide {slideIndex + 1} updated.";

        return RedirectToAction(nameof(Admin));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private AdminViewModel CreateAdminViewModel()
    {
        var settings = GetSettings();

        return new AdminViewModel
        {
            CurrentLandingBackground = settings.LandingBackground,
            LandingCarouselImages = GetLandingCarouselImages(settings),
            CarouselImageOptions = GetCarouselImageOptions(settings),
            UploadedImages = GetUploadedImages(settings)
        };
    }

    private IReadOnlyList<string> GetLandingCarouselImages()
    {
        return GetLandingCarouselImages(GetSettings());
    }

    private IReadOnlyList<string> GetLandingCarouselImages(SiteSettings settings)
    {
        var fallbackPool = new[] { settings.LandingBackground }
            .Concat(GetUploadedImages(settings).Select(image => image.Url))
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

    private IReadOnlyList<CarouselImageOption> GetCarouselImageOptions(SiteSettings settings)
    {
        var uploadedOptions = GetUploadedImages(settings).Select(image => new CarouselImageOption
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

    private IReadOnlyList<UploadedImageViewModel> GetUploadedImages(SiteSettings settings)
    {
        var savedImages = settings.UploadedImages
            .Where(image => !string.IsNullOrWhiteSpace(image.Url))
            .Select(image => new UploadedImageViewModel
            {
                FileName = image.FileName,
                Url = image.Url,
                DisplayName = string.IsNullOrWhiteSpace(image.DisplayName) ? image.FileName : image.DisplayName,
                CreatedAt = image.CreatedAt
            });

        var uploadDirectory = GetUploadDirectory();
        if (!Directory.Exists(uploadDirectory))
        {
            return savedImages.OrderByDescending(image => image.CreatedAt).ToList();
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
                    CreatedAt = System.IO.File.GetCreationTimeUtc(file)
                };
            })
            .Where(image => settings.UploadedImages.All(saved => saved.Url != image.Url));

        return savedImages
            .Concat(localImages)
            .OrderByDescending(image => image.CreatedAt)
            .ToList();
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

    private bool IsKnownImageUrl(string imageUrl)
    {
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
        {
            return GetSettings().UploadedImages.Any(image => image.Url == imageUrl);
        }

        if (!imageUrl.StartsWith("/uploads/admin/", StringComparison.Ordinal))
        {
            return false;
        }

        var fileName = Path.GetFileName(imageUrl);
        var filePath = Path.Combine(GetUploadDirectory(), fileName);

        return System.IO.File.Exists(filePath);
    }

    private async Task<UploadedImageRecord> UploadImage(IFormFile image, string extension)
    {
        var cloudinary = CreateCloudinaryClient();

        if (cloudinary is not null)
        {
            await using var imageStream = image.OpenReadStream();
            var uploadResult = await cloudinary.UploadAsync(new ImageUploadParams
            {
                File = new FileDescription(image.FileName, imageStream),
                Folder = "nzo-website",
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

    private string GetUploadDirectory()
    {
        return Path.Combine(_environment.WebRootPath, "uploads", "admin");
    }

    private string GetSettingsDirectory()
    {
        return Path.Combine(_environment.ContentRootPath, "App_Data");
    }

    private string GetSettingsPath()
    {
        return Path.Combine(GetSettingsDirectory(), "site-settings.json");
    }
}
