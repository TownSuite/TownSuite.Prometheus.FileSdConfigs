using NUnit.Framework;

namespace TownSuite.Prometheus.FileSdConfigs.Tests;

public class HttpClientFactoryTest
{
    [Test]
    public void ConfiguredTimeout_IsApplied()
    {
        using var client = HttpClientFactory.Create(new AppSettings { HttpTimeoutInSeconds = 10 });

        Assert.That(client.Timeout, Is.EqualTo(TimeSpan.FromSeconds(10)));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void MissingOrNonPositiveTimeout_FallsBackToTheDefault(int configured)
    {
        // A zero timeout used to throw ArgumentOutOfRangeException before the loop
        // ever started, which killed the process on an appsettings.json that simply
        // omitted HttpTimeoutInSeconds.
        using var client = HttpClientFactory.Create(new AppSettings { HttpTimeoutInSeconds = configured });

        Assert.That(client.Timeout,
            Is.EqualTo(TimeSpan.FromSeconds(HttpClientFactory.DefaultTimeoutInSeconds)));
    }

    [Test]
    public void SkipCertificateValidation_StillBuildsAClient()
    {
        using var client = HttpClientFactory.Create(new AppSettings
        {
            HttpTimeoutInSeconds = 5,
            SkipCertificateValidation = true
        });

        Assert.That(client.Timeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
    }
}
