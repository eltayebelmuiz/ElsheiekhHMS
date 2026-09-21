using System.Diagnostics;
using System.Text.Json;
using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Infrastructure.Auditing.Entities;
using ElsheiekhHMS.Infrastructure.Persistence;

namespace ElsheiekhHMS.Infrastructure.Auditing;

public sealed class AuditEventWriter(
    ElsheiekhHmsDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IAuditEventWriter
{
    private const int MaxCategoryLength = 32;
    private const int MaxActionLength = 64;
    private const int MaxTargetTypeLength = 128;
    private const int MaxTargetIdLength = 200;
    private const int MaxReasonLength = 1000;
    private const int MaxMetadataLength = 4000;
    private const int MaxMetadataKeyLength = 64;
    private const int MaxMetadataValueLength = 512;
    private const int MaxCorrelationIdLength = 128;

    private static readonly string[] ForbiddenMetadataFragments =
    [
        "password",
        "hash",
        "token",
        "cookie",
        "ticket",
        "securitystamp",
        "connectionstring",
        "secret",
        "encryptionkey",
        "apikey"
    ];

    public Task RecordAsync(
        AuditEventRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRequest(request);

        var actorUserId = currentUser.IsAuthenticated && !string.IsNullOrWhiteSpace(currentUser.UserId)
            ? currentUser.UserId
            : null;
        var actorKind = actorUserId is not null
            ? AuditActorKinds.Human
            : string.Equals(currentUser.UserId, "system", StringComparison.OrdinalIgnoreCase)
                ? AuditActorKinds.System
                : AuditActorKinds.Anonymous;
        var actorUserName = actorKind == AuditActorKinds.Human
            ? NormalizeOptional(currentUser.UserName, 200)
            : null;
        var metadataJson = SerializeMetadata(request.Metadata);
        var correlationId = NormalizeOptional(Activity.Current?.Id, MaxCorrelationIdLength);

        dbContext.AuditLogs.Add(AuditLog.Create(
            timeProvider.GetUtcNow(),
            actorKind,
            actorUserId,
            actorUserName,
            request.Category,
            request.Action,
            request.TargetType,
            request.TargetId,
            request.Reason,
            metadataJson,
            correlationId));

        return Task.CompletedTask;
    }

    private static void ValidateRequest(AuditEventRequest request)
    {
        if (!AuditCategories.All.Contains(request.Category, StringComparer.Ordinal))
        {
            throw new ArgumentException("The audit category is not approved.", nameof(request));
        }

        ValidateIdentifier(request.Action, MaxActionLength, nameof(request.Action));
        ValidateOptionalLength(request.TargetType, MaxTargetTypeLength, nameof(request.TargetType));
        ValidateOptionalLength(request.TargetId, MaxTargetIdLength, nameof(request.TargetId));
        ValidateOptionalLength(request.Reason, MaxReasonLength, nameof(request.Reason));

        if ((request.TargetType is null) != (request.TargetId is null))
        {
            throw new ArgumentException(
                "TargetType and TargetId must either both be supplied or both be null.",
                nameof(request));
        }
    }

    private static void ValidateIdentifier(string value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength ||
            value.Any(character => !(character is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_')))
        {
            throw new ArgumentException("The audit identifier is invalid or exceeds its bound.", parameterName);
        }
    }

    private static void ValidateOptionalLength(string? value, int maxLength, string parameterName)
    {
        if (value is not null && (string.IsNullOrWhiteSpace(value) || value.Length > maxLength))
        {
            throw new ArgumentException("The audit value is invalid or exceeds its bound.", parameterName);
        }
    }

    private static string? SerializeMetadata(IReadOnlyDictionary<string, string?>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        foreach (var pair in metadata)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Key.Length > MaxMetadataKeyLength ||
                ForbiddenMetadataFragments.Any(fragment =>
                    pair.Key.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Metadata contains a prohibited or unbounded key.", nameof(metadata));
            }

            if (pair.Value is not null && pair.Value.Length > MaxMetadataValueLength)
            {
                throw new ArgumentException("Metadata contains an unbounded value.", nameof(metadata));
            }
        }

        var json = JsonSerializer.Serialize(metadata);
        if (json.Length > MaxMetadataLength)
        {
            throw new ArgumentException("Metadata exceeds the approved bound.", nameof(metadata));
        }

        return json;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Length > maxLength)
        {
            throw new InvalidOperationException("Server-supplied audit context exceeds its approved bound.");
        }

        return value;
    }
}
