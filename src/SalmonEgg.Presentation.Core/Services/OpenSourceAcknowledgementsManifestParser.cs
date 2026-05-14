using System;
using System.Collections.Generic;
using System.IO;

namespace SalmonEgg.Presentation.Core.Services;

public static class OpenSourceAcknowledgementsManifestParser
{
    public static IReadOnlyList<OpenSourceAcknowledgement> Parse(string? manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest))
        {
            return Array.Empty<OpenSourceAcknowledgement>();
        }

        var acknowledgements = new List<OpenSourceAcknowledgement>();
        using var reader = new StringReader(manifest);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = line.Split('\t');
            var name = GetColumn(columns, 0);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            acknowledgements.Add(new OpenSourceAcknowledgement(
                name,
                GetColumn(columns, 1),
                GetColumn(columns, 2),
                GetColumn(columns, 3)));
        }

        return acknowledgements;
    }

    private static string GetColumn(string[] columns, int index)
        => index < columns.Length ? columns[index].Trim() : string.Empty;
}
