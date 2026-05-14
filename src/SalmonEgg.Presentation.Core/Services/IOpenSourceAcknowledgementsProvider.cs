using System.Collections.Generic;

namespace SalmonEgg.Presentation.Core.Services;

public interface IOpenSourceAcknowledgementsProvider
{
    IReadOnlyList<OpenSourceAcknowledgement> GetAcknowledgements();
}
