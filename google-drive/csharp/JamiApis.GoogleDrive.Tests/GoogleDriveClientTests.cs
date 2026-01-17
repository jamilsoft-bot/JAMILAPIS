using Xunit;

namespace JamiApis.GoogleDrive.Tests;

public class GoogleDriveClientTests
{
    [Fact]
    public void ConstructorRequiresCredentials()
    {
        Assert.Throws<ArgumentException>(() => new GoogleDriveClient(credentialsPath: ""));
    }

    [Fact]
    public void MockingPlaceholder()
    {
        // Example: use Moq to mock IConfig or wrap DriveService via interface in future.
        Assert.True(true);
    }
}
