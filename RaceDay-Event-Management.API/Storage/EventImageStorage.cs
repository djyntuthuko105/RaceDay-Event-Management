namespace RaceDay_Event_Management.API.Storage;

public class EventImageStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private readonly IWebHostEnvironment _environment;

    public EventImageStorage(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<(string Url, string ImageType)> SaveAsync(int eventId, IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Use a jpg, png, gif, or webp image.");

        if (file.Length == 0 || file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("The image must be smaller than 5 MB.");

        var folder = Path.Combine(WebRoot(), "event-images", eventId.ToString());
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(folder, fileName);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, cancellationToken);

        var imageType = string.IsNullOrWhiteSpace(file.ContentType) ? "image" : file.ContentType;
        if (imageType.Length > 50)
            imageType = imageType[..50];

        return ($"/event-images/{eventId}/{fileName}", imageType);
    }

    public void Delete(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/event-images/", StringComparison.Ordinal))
            return;

        var webRoot = WebRoot();
        var imagesRoot = Path.GetFullPath(Path.Combine(webRoot, "event-images"));
        var relative = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, relative));

        // The url is stored by us, but still refuse anything that would leave the image folder.
        if (!fullPath.StartsWith(imagesRoot, StringComparison.OrdinalIgnoreCase))
            return;

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    private string WebRoot()
    {
        var root = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(root))
            root = Path.Combine(_environment.ContentRootPath, "wwwroot");

        Directory.CreateDirectory(root);
        return root;
    }
}
