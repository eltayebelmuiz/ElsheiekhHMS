using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ElsheiekhHMS.Infrastructure.Observability;

public sealed class RequestObservabilityMiddleware(
    RequestDelegate next,
    ILogger<RequestObservabilityMiddleware> logger)
{
    private static readonly EventId RequestCompleted =
        new(9101, nameof(RequestCompleted));

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var started = Stopwatch.GetTimestamp();
        var activity = Activity.Current;
        var ownsActivity = activity is null;
        if (ownsActivity)
        {
            activity = new Activity("ElsheiekhHMS.HttpRequest")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
        }

        var correlationId = GetCorrelationId(activity);
        var scopeValues = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["CorrelationId"] = correlationId,
            ["HttpMethod"] = context.Request.Method,
            ["RequestPath"] = context.Request.Path.Value ?? "/"
        };

        var userId = GetStableUserId(context.User);
        if (userId is not null)
        {
            scopeValues["UserId"] = userId;
        }

        try
        {
            using (logger.BeginScope(scopeValues))
            {
                try
                {
                    await next(context);
                }
                finally
                {
                    var duration = Stopwatch.GetElapsedTime(started);
                    var statusCode = context.Response.StatusCode;
                    var level = statusCode >= StatusCodes.Status500InternalServerError
                        ? LogLevel.Error
                        : statusCode >= StatusCodes.Status400BadRequest
                            ? LogLevel.Warning
                            : LogLevel.Information;

                    logger.Log(
                        level,
                        RequestCompleted,
                        "HTTP request completed {HttpMethod} {RequestPath} with {StatusCode} in {DurationMilliseconds} ms ({CorrelationId})",
                        context.Request.Method,
                        context.Request.Path.Value ?? "/",
                        statusCode,
                        duration.TotalMilliseconds,
                        correlationId);
                }
            }
        }
        finally
        {
            if (ownsActivity)
            {
                activity?.Stop();
            }
        }
    }

    private static string GetCorrelationId(Activity? activity)
    {
        var traceId = activity?.TraceId.ToString();
        return string.IsNullOrWhiteSpace(traceId) ||
               traceId.All(character => character == '0')
            ? activity?.Id ?? Guid.NewGuid().ToString("N")
            : traceId;
    }

    private static string? GetStableUserId(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirst("sub")?.Value;
        return string.IsNullOrWhiteSpace(userId) ? null : userId;
    }
}
