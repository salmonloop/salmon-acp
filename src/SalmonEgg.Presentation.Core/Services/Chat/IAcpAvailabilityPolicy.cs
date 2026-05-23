namespace SalmonEgg.Presentation.Core.Services.Chat;

public interface IAcpAvailabilityPolicy
{
    bool IsAcpEnabled { get; }
}

public sealed class AlwaysEnabledAcpAvailabilityPolicy : IAcpAvailabilityPolicy
{
    public static AlwaysEnabledAcpAvailabilityPolicy Instance { get; } = new();

    private AlwaysEnabledAcpAvailabilityPolicy()
    {
    }

    public bool IsAcpEnabled => true;
}
