namespace SalmonEgg.Presentation.ViewModels.Settings;

public sealed record OpenSourceAcknowledgementViewModel(
    string Name,
    string Version,
    string License,
    string SourceUrl);
