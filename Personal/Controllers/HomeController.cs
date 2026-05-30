using System.Diagnostics;
using System.Text.Json;
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
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly IWebHostEnvironment _environment;

    public HomeController(IWebHostEnvironment environment)
    {
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

        var uploadedUrls = new List<string>();
        var uploadDirectory = GetUploadDirectory();
        Directory.CreateDirectory(uploadDirectory);

        foreach (var image in images.Where(image => image.Length > 0))
        {
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
            {
                TempData["AdminMessage"] = "Only JPG, PNG, WEBP, and GIF images can be uploaded.";
                return RedirectToAction(nameof(Admin));
            }

            var fileName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadDirectory, fileName);

            await using var stream = System.IO.File.Create(filePath);
            await image.CopyToAsync(stream);

            uploadedUrls.Add($"/uploads/admin/{fileName}");
        }

        if (uploadedUrls.Count == 0)
        {
            TempData["AdminMessage"] = "No image files were uploaded.";
            return RedirectToAction(nameof(Admin));
        }

        if (useAsLandingBackground)
        {
            SaveSettings(new SiteSettings { LandingBackground = uploadedUrls[0] });
        }

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

        SaveSettings(new SiteSettings { LandingBackground = imageUrl });
        TempData["AdminMessage"] = "Landing background updated.";

        return RedirectToAction(nameof(Admin));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private AdminViewModel CreateAdminViewModel()
    {
        return new AdminViewModel
        {
            CurrentLandingBackground = GetSettings().LandingBackground,
            LandingCarouselImages = GetLandingCarouselImages(),
            UploadedImages = GetUploadedImages()
        };
    }

    private IReadOnlyList<string> GetLandingCarouselImages()
    {
        var selectedBackground = GetSettings().LandingBackground;
        var uploadedImages = GetUploadedImages().Select(image => image.Url);

        return new[] { selectedBackground }
            .Concat(uploadedImages)
            .Concat(FallbackLandingCarouselImages)
            .Where(IsUsableCarouselImage)
            .Distinct(StringComparer.Ordinal)
            .Take(4)
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

    private IReadOnlyList<UploadedImageViewModel> GetUploadedImages()
    {
        var uploadDirectory = GetUploadDirectory();
        if (!Directory.Exists(uploadDirectory))
        {
            return [];
        }

        return Directory
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

    private bool IsKnownImageUrl(string imageUrl)
    {
        if (!imageUrl.StartsWith("/uploads/admin/", StringComparison.Ordinal))
        {
            return false;
        }

        var fileName = Path.GetFileName(imageUrl);
        var filePath = Path.Combine(GetUploadDirectory(), fileName);

        return System.IO.File.Exists(filePath);
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
