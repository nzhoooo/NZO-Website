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
    private const string LocalLibrarySource = "local";
    private const string CloudinaryLibrarySource = "cloudinary";
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

    public IActionResult Help()
    {
        return View();
    }

    public IActionResult Admin(string librarySource = LocalLibrarySource, string cloudinaryFolder = "")
    {
        return View(CreateAdminViewModel(librarySource, cloudinaryFolder));
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

    private AdminViewModel CreateAdminViewModel(string librarySource, string cloudinaryFolder)
    {
        var settings = GetSettings();
        var normalizedLibrarySource = NormalizeLibrarySource(librarySource);
        var normalizedCloudinaryFolder = normalizedLibrarySource == CloudinaryLibrarySource
            ? NormalizeCloudinaryFolder(cloudinaryFolder)
            : string.Empty;
        var uploadedImages = GetUploadedImages(settings, includeCloudinaryLibrary: true, cloudinaryFolder: normalizedCloudinaryFolder);

        return new AdminViewModel
        {
            CurrentLandingBackground = settings.LandingBackground,
            LandingCarouselImages = GetLandingCarouselImages(settings, includeCloudinaryLibrary: true),
            CarouselImageOptions = GetCarouselImageOptions(settings, includeCloudinaryLibrary: true),
            UploadedImages = uploadedImages
                .Where(image => image.Source == normalizedLibrarySource)
                .ToList(),
            CurrentLibrarySource = normalizedLibrarySource,
            CurrentCloudinaryFolder = normalizedCloudinaryFolder,
            CloudinaryFolders = GetCloudinaryFolders()
        };
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
            while (!string.IsNullOrWhiteSpace(nextCursor) && cloudinaryResources.Count < 500);

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
