using System.Diagnostics;
using System.Security.Claims;
using ElsheiekhHMS.Infrastructure.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ElsheiekhHMS.Tests.Unit.Infrastructure.Observability;

public sealed class RequestObservabilityMiddlewareTests
{
    [Fact]
    public async Task Existing_activity_trace_id_is_scoped_and_completion_is_structured()
    {
        using var activity = new Activity("incoming-request")
            .SetIdFormat(ActivityIdFormat.W3C)
            .Start();
        var logger = new CapturingLogger();
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/health/live";
        context.Response.StatusCode = StatusCodes.Status204NoContent;

        await new RequestObservabilityMiddleware(
                _ => Task.CompletedTask,
                logger)
            .InvokeAsync(context);

        var scope = Assert.Single(logger.Scopes);
        Assert.Equal(activity.TraceId.ToString(), scope["CorrelationId"]);
        Assert.Equal("GET", scope["HttpMethod"]);
        Assert.Equal("/health/live", scope["RequestPath"]);

        var completion = Assert.Single(logger.Records);
        Assert.Equal(LogLevel.Information, completion.Level);
        Assert.Equal(StatusCodes.Status204NoContent, completion.Values["StatusCode"]);
        Assert.Equal(activity.TraceId.ToString(), completion.Values["CorrelationId"]);
        Assert.True((double)completion.Values["DurationMilliseconds"]! >= 0);
    }

    [Fact]
    public async Task Authenticated_request_scopes_stable_user_id_without_a_database_lookup()
    {
        var logger = new CapturingLogger();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "stable-user-09a")],
                "test"))
        };
        context.Request.Method = "POST";
        context.Request.Path = "/appointments";

        await new RequestObservabilityMiddleware(
                _ => Task.CompletedTask,
                logger)
            .InvokeAsync(context);

        var scope = Assert.Single(logger.Scopes);
        Assert.Equal("stable-user-09a", scope["UserId"]);
        Assert.DoesNotContain("UserName", scope.Keys);
    }

    [Fact]
    public async Task Anonymous_request_does_not_log_sensitive_headers_query_or_body_data()
    {
        var logger = new CapturingLogger();
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/patients";
        context.Request.QueryString = new QueryString("?name=private-patient");
        context.Request.Headers.Authorization = "Bearer secret-token";
        context.Request.Headers.Cookie = "session=secret-cookie";

        await new RequestObservabilityMiddleware(
                _ => Task.CompletedTask,
                logger)
            .InvokeAsync(context);

        var scope = Assert.Single(logger.Scopes);
        Assert.False(scope.ContainsKey("UserId"));
        var rendered = string.Join(" ", logger.Records.Select(record => record.RenderedMessage));
        Assert.DoesNotContain("private-patient", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-token", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-cookie", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Downstream_exception_is_not_swallowed_or_written_with_sensitive_details()
    {
        var logger = new CapturingLogger();
        var exception = new InvalidOperationException("internal database detail");
        var context = new DefaultHttpContext();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RequestObservabilityMiddleware(
                    _ => throw exception,
                    logger)
                .InvokeAsync(context));

        Assert.Same(exception, thrown);
        Assert.DoesNotContain(
            exception.Message,
            string.Join(" ", logger.Records.Select(record => record.RenderedMessage)),
            StringComparison.Ordinal);
    }

    private sealed class CapturingLogger : ILogger<RequestObservabilityMiddleware>
    {
        public List<IReadOnlyDictionary<string, object?>> Scopes { get; } = [];
        public List<LogRecord> Records { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> values)
            {
                Scopes.Add(values.ToDictionary(pair => pair.Key, pair => pair.Value));
            }

            return NoopDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var values = state is IEnumerable<KeyValuePair<string, object?>> pairs
                ? pairs.ToDictionary(pair => pair.Key, pair => pair.Value)
                : new Dictionary<string, object?>();
            Records.Add(new(logLevel, values, formatter(state, exception)));
        }
    }

    private sealed record LogRecord(
        LogLevel Level,
        IReadOnlyDictionary<string, object?> Values,
        string RenderedMessage);

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();
        public void Dispose() { }
    }
}
