using System;
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        // Skip logging for health checks
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var request = context.Request;

        // Log basic info
        _logger.LogInformation("🌐 {Method} {Path} - Content-Type: {ContentType}", 
            request.Method, request.Path, request.ContentType);

        // If it's FormData, try to log some info
        if (request.HasFormContentType)
        {
            try
            {
                var form = await request.ReadFormAsync();
                
                _logger.LogInformation("📝 FormData fields:");
                foreach (var key in form.Keys)
                {
                    if (key.Contains("Files", StringComparison.OrdinalIgnoreCase))
                    {
                        var files = form.Files.GetFiles(key);
                        _logger.LogInformation("  • {Key}: {Count} files", key, files?.Count ?? 0);
                    }
                    else
                    {
                        var value = form[key];
                        _logger.LogInformation("  • {Key}: '{Value}'", key, value.ToString().Truncate(100));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Cannot read FormData: {Message}", ex.Message);
            }
        }

        await _next(context);
    }
}

// Extension method untuk Program.cs
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}

// Extension method untuk string truncate
public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength, string truncationSuffix = "...")
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + truncationSuffix;
    }
}