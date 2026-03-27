using NUnit.Framework;
using SalmonEgg.Domain.Models.Protocol;

namespace SalmonEgg.Domain.Tests.Protocol;

[TestFixture]
public sealed class SessionListRuntimeTypesTests
{
    [Test]
    public void SessionListParams_Should_ExposeCursorProperty()
    {
        Assert.That(typeof(SessionListParams).GetProperty("Cursor"), Is.Not.Null);
    }

    [Test]
    public void SessionListResponse_Should_ExposeNextCursorProperty()
    {
        Assert.That(typeof(SessionListResponse).GetProperty("NextCursor"), Is.Not.Null);
    }
}
