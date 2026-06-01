using System;
using System.Reflection;
using SalmonEgg.Domain.Services;

namespace SalmonEgg.Infrastructure.Services;

public sealed class AppSupportInfoService : IAppSupportInfoService
{
    internal const string ReportInappropriateAiContentEmailMetadataKey = "SalmonEgg.ReportInappropriateAiContentEmail";

    private readonly string _reportInappropriateAiContentEmail;

    public AppSupportInfoService(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _reportInappropriateAiContentEmail = ResolveAssemblyMetadataValue(
            assembly,
            ReportInappropriateAiContentEmailMetadataKey);
    }

    public string ReportInappropriateAiContentEmail => _reportInappropriateAiContentEmail;

    private static string ResolveAssemblyMetadataValue(Assembly assembly, string key)
    {
        foreach (var attribute in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (!string.Equals(attribute.Key, key, StringComparison.Ordinal))
            {
                continue;
            }

            return attribute.Value?.Trim() ?? string.Empty;
        }

        return string.Empty;
    }
}
