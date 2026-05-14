namespace SalmonEgg.Presentation.Core.Services;

public sealed record OpenSourceAcknowledgement(
    string Name,
    string Version,
    string License,
    string SourceUrl);
