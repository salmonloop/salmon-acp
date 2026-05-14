using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SalmonEgg.Presentation.Core.Services;

namespace SalmonEgg.Presentation.Services;

public sealed class GeneratedOpenSourceAcknowledgementsProvider : IOpenSourceAcknowledgementsProvider
{
    private const string ManifestResourceName = "SalmonEgg.Presentation.Services.OpenSourceAcknowledgements.tsv";

    private readonly Lazy<IReadOnlyList<OpenSourceAcknowledgement>> _acknowledgements = new(LoadAcknowledgements);

    public IReadOnlyList<OpenSourceAcknowledgement> GetAcknowledgements()
        => _acknowledgements.Value;

    private static IReadOnlyList<OpenSourceAcknowledgement> LoadAcknowledgements()
    {
        var assembly = typeof(GeneratedOpenSourceAcknowledgementsProvider).GetTypeInfo().Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => string.Equals(name, ManifestResourceName, StringComparison.Ordinal));

        if (resourceName is null)
        {
            return Array.Empty<OpenSourceAcknowledgement>();
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return Array.Empty<OpenSourceAcknowledgement>();
        }

        using var reader = new StreamReader(stream);
        return OpenSourceAcknowledgementsManifestParser.Parse(reader.ReadToEnd());
    }
}
