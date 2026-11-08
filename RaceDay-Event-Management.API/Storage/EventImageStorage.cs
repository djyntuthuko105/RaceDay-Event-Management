using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
namespace RaceDay_Event_Management.API.Storage;

public class StorageUnavailableException : Exception
{
    public StorageUnavailableException() : base("Image storage is unavailable. Please try again later.") { }
}

public class EventImageStorage(IWebHostEnvironment environment, IConfiguration configuration)
{
    public Task<(string Url, string ImageType)> SaveAsync(int eventId, IFormFile file, CancellationToken cancellationToken)
        => SaveImageAsync($"events/{eventId}", file, cancellationToken);

    public async Task<(string Url, string ImageType)> SaveImageAsync(string folder, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > 5 * 1024 * 1024)
            throw new InvalidOperationException("Choose an image between 1 byte and 5 MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var contentType = extension switch { ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", ".gif" => "image/gif", ".webp" => "image/webp", _ => "" };
        if (contentType == "") throw new InvalidOperationException("Use a JPG, PNG, GIF or WebP image.");
        await using var source = file.OpenReadStream();
        using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        if (!MatchesImage(bytes, contentType)) throw new InvalidOperationException("The file content is not a supported image.");
        memory.Position = 0;
        var name = $"{folder}/{Guid.NewGuid():N}{extension}";
        if (UseAzure)
        {
            try
            {
                var container = Container();
                await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
                var blob = container.GetBlobClient(name);
                await blob.UploadAsync(memory, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } }, cancellationToken);
                return (blob.Uri.AbsoluteUri, contentType);
            }
            catch (Exception ex) when (ex is RequestFailedException or FormatException or ArgumentException)
            { throw new StorageUnavailableException(); }
        }
        if (configuration["Storage:Provider"] is not (null or "Local")) throw new StorageUnavailableException();
        var path = Path.Combine(WebRoot, "event-images", name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
        return ($"/event-images/{name}", contentType);
    }

    public async Task DeleteAsync(string url, CancellationToken cancellationToken)
    {
        if (url.StartsWith("/event-images/", StringComparison.Ordinal))
        {
            var path = LocalPath(url);
            if (path is not null && File.Exists(path)) File.Delete(path);
            return;
        }
        var blob = OwnedBlob(url);
        if (blob is not null) await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<Stream?> OpenAsync(string url, CancellationToken cancellationToken)
    {
        if (url.StartsWith("/event-images/", StringComparison.Ordinal))
        {
            var path = LocalPath(url);
            return path is not null && File.Exists(path) ? File.OpenRead(path) : null;
        }
        var blob = OwnedBlob(url);
        if (blob is null) return null;
        try { return (await blob.DownloadStreamingAsync(cancellationToken: cancellationToken)).Value.Content; }
        catch (RequestFailedException ex) when (ex.Status == 404) { return null; }
    }
    private bool UseAzure => configuration["Storage:Provider"] == "AzureBlob";
    private string WebRoot => environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
    private BlobContainerClient Container()
    {
        var connection = configuration["Storage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connection)) throw new StorageUnavailableException();
        var options = new BlobClientOptions();
        options.Retry.MaxRetries = 1;
        options.Retry.NetworkTimeout = TimeSpan.FromSeconds(10);
        return new BlobContainerClient(connection, configuration["Storage:Container"] ?? "raceday-images", options);
    }
    private BlobClient? OwnedBlob(string url)
    {
        if (!UseAzure) return null;
        var container = Container();
        var prefix = container.Uri.AbsoluteUri.TrimEnd('/') + "/";
        // Only read or delete objects belonging to our configured container.
        return url.StartsWith(prefix, StringComparison.Ordinal) ? container.GetBlobClient(Uri.UnescapeDataString(url[prefix.Length..])) : null;
    }
    private string? LocalPath(string url)
    {
        var root = Path.GetFullPath(Path.Combine(WebRoot, "event-images")) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(WebRoot, url.TrimStart('/')));
        return path.StartsWith(root, StringComparison.Ordinal) ? path : null;
    }
    private static bool MatchesImage(byte[] data, string type) => type switch
    {
        "image/png" => data.Length >= 8 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/jpeg" => data.Length >= 3 && data[0] == 255 && data[1] == 216 && data[2] == 255,
        "image/gif" => data.Length >= 6 && (System.Text.Encoding.ASCII.GetString(data, 0, 6) is "GIF87a" or "GIF89a"),
        "image/webp" => data.Length >= 12 && System.Text.Encoding.ASCII.GetString(data, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(data, 8, 4) == "WEBP",
        _ => false
    };
}
