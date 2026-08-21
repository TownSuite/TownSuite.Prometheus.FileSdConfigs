using Microsoft.Extensions.Logging;

namespace TownSuite.Prometheus.FileSdConfigs;

/// <summary>
/// Writes a single service discovery output file.  The content is generated into a
/// "[outputPath].tmp" file and then moved over the destination so prometheus never
/// reads a half written file.
/// </summary>
public class TargetFileWriter
{
    private readonly ILogger _logger;

    public TargetFileWriter(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates and writes one output file.  An output path that is not configured
    /// disables that output.
    /// </summary>
    /// <returns>False when the output is disabled and nothing was written.</returns>
    public async Task<bool> WriteAsync(string outputPath, Func<Stream, Task> generate)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return false;
        }

        string tempPath = $"{outputPath}.tmp";
        await using (var fs = new FileStream(tempPath, FileMode.Create))
        {
            await generate(fs);
        }

        File.Move(tempPath, outputPath, true);
        _logger?.LogDebug($"{outputPath} written");
        return true;
    }
}
