using System;
using System.Collections.Generic;
using System.Text;
using SalmonEgg.Domain.Models;

namespace SalmonEgg.Presentation.Core.Services.Chat;

public readonly record struct AcpConnectionReuseKey(
    TransportType TransportType,
    string StdioCommand,
    string StdioArgsCanonical,
    string RemoteUrl)
{
    public static AcpConnectionReuseKey FromTransportConfiguration(IAcpTransportConfiguration transportConfiguration)
    {
        ArgumentNullException.ThrowIfNull(transportConfiguration);

        var normalizedCommand = (transportConfiguration.StdioCommand ?? string.Empty).Trim();
        var normalizedUrl = (transportConfiguration.RemoteUrl ?? string.Empty).Trim();
        var canonicalArgs = CanonicalizeArguments(transportConfiguration.StdioArgs);

        return new AcpConnectionReuseKey(
            transportConfiguration.SelectedTransportType,
            normalizedCommand,
            canonicalArgs,
            normalizedUrl);
    }

    private static string CanonicalizeArguments(string? args)
    {
        var tokens = ParseCommandLineArguments(args);
        if (tokens.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var token in tokens)
        {
            // Length-prefixed format avoids delimiter ambiguity and keeps stable equality.
            builder.Append(token.Length);
            builder.Append(':');
            builder.Append(token);
            builder.Append('|');
        }

        return builder.ToString();
    }

    private static string[] ParseCommandLineArguments(string? args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            return Array.Empty<string>();
        }

        var results = new List<string>();
        var current = new StringBuilder();
        char? activeQuote = null;

        foreach (var character in args)
        {
            if ((character == '"' || character == '\''))
            {
                if (activeQuote == character)
                {
                    activeQuote = null;
                    continue;
                }

                if (activeQuote == null)
                {
                    activeQuote = character;
                    continue;
                }
            }

            if (char.IsWhiteSpace(character) && activeQuote == null)
            {
                if (current.Length == 0)
                {
                    continue;
                }

                results.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        if (current.Length > 0)
        {
            results.Add(current.ToString());
        }

        return results.ToArray();
    }
}

