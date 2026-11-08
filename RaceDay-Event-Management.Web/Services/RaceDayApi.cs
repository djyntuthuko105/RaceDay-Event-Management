using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
namespace RaceDay.Web.Services;

public class ApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public class RaceDayApi(HttpClient client, IHttpContextAccessor accessor)
{
    public const string CookieKey = "ApiSessionCookie";
    public async Task<T> GetAsync<T>(string path) => (await SendAsync<T>(HttpMethod.Get, path))!;
    public async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var content = body is null ? null : JsonContent.Create(body);
        return await SendContentAsync<T>(method, path, content);
    }
    public async Task<T?> UploadAsync<T>(string path, IFormFile file)
    {
        using var form = new MultipartFormDataContent();
        var stream = new StreamContent(file.OpenReadStream());
        stream.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
        form.Add(stream, "File", Path.GetFileName(file.FileName));
        return await SendContentAsync<T>(HttpMethod.Post, path, form);
    }
    private async Task<T?> SendContentAsync<T>(HttpMethod method, string path, HttpContent? content)
    {
        var context = accessor.HttpContext!;
        using var request = new HttpRequestMessage(method, path) { Content = content };
        // A shared HttpClient cookie jar would leak one user's API session to another.
        var cookie = context.Session.GetString(CookieKey);
        if (cookie is not null) request.Headers.Add("Cookie", cookie);
        using var response = await client.SendAsync(request, context.RequestAborted);
        if (response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            var sessionCookie = values.FirstOrDefault(value => value.StartsWith("RaceDay.Session=", StringComparison.Ordinal));
            if (sessionCookie is not null) context.Session.SetString(CookieKey, sessionCookie.Split(';')[0]);
        }
        if (!response.IsSuccessStatusCode)
        {
            var message = "The service could not complete your request. Please try again.";
            if ((int)response.StatusCode < 500)
            {
                try
                {
                    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    if (json.RootElement.TryGetProperty("message", out var value)) message = value.GetString() ?? message;
                    else if (json.RootElement.TryGetProperty("errors", out _)) message = "Please check the entered values and try again.";
                }
                catch (JsonException) { }
            }
            throw new ApiException(response.StatusCode, message);
        }
        if (response.StatusCode == HttpStatusCode.NoContent) return default;
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: context.RequestAborted);
    }
}
