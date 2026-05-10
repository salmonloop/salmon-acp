using SalmonEgg.Infrastructure.Services.Security;

namespace SalmonEgg.Infrastructure.Tests.Services.Security;

public sealed class PathValidatorTests
{
    [Theory]
    [InlineData("/home/user/project")]
    [InlineData(@"C:\Users\user\project")]
    [InlineData(@"\\server\share\project")]
    public void IsAbsolutePath_AcceptsProtocolAbsolutePaths(string path)
    {
        var validator = new PathValidator();

        Assert.True(validator.IsAbsolutePath(path));
    }

    [Theory]
    [InlineData("relative-path")]
    [InlineData(@"folder\child")]
    [InlineData("C:folder")]
    [InlineData(@"\drive-relative")]
    public void IsAbsolutePath_RejectsRelativePaths(string path)
    {
        var validator = new PathValidator();

        Assert.False(validator.IsAbsolutePath(path));
    }
}
