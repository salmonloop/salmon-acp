using System;
using System.Globalization;

namespace SalmonEgg.Presentation.Core.Utilities;

internal static class AcpSessionTimestampPolicy
{
    public static DateTime? ParseUpdatedAtUtc(string? updatedAt)
    {
        if (string.IsNullOrWhiteSpace(updatedAt)
            || !DateTimeOffset.TryParse(
                updatedAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedUpdatedAt))
        {
            return null;
        }

        return parsedUpdatedAt.UtcDateTime;
    }

    public static DateTime? ResolveLatestUpdatedAtUtc(DateTime? existing, DateTime? incoming)
    {
        if (incoming is not DateTime incomingValue || incomingValue == default)
        {
            return existing;
        }

        if (existing is not DateTime existingValue || existingValue == default)
        {
            return incomingValue;
        }

        return incomingValue > existingValue ? incomingValue : existingValue;
    }
}
