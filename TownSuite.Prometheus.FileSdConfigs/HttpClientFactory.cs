namespace TownSuite.Prometheus.FileSdConfigs;

public static class HttpClientFactory
{
    /// <summary>
    /// Used when HttpTimeoutInSeconds is missing or not a positive value.  Matches
    /// HttpClient's own default rather than crashing on a zero timeout.
    /// </summary>
    public const int DefaultTimeoutInSeconds = 100;

    public static HttpClient Create(AppSettings appSettings)
    {
        var timeout = TimeSpan.FromSeconds(appSettings.HttpTimeoutInSeconds > 0
            ? appSettings.HttpTimeoutInSeconds
            : DefaultTimeoutInSeconds);

        if (appSettings.SkipCertificateValidation)
        {
            var handler = new HttpClientHandler
            {
                // Allow self-signed certificates when explicitly enabled.
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            return new HttpClient(handler)
            {
                Timeout = timeout
            };
        }

        return new HttpClient
        {
            Timeout = timeout
        };
    }
}
