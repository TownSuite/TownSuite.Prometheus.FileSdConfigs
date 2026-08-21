using Microsoft.Extensions.Logging;

namespace TownSuite.Prometheus.FileSdConfigs;

/// <summary>
/// Writes a single service discovery output file.  The content is generated into a
/// "[outputPath].tmp" file and then moved over the destination so prometheus never
/// reads a half written file.
///
/// A generator that reports an incomplete pass does not overwrite the destination.
/// Losing a lookup means we do not know what the targets are, not that there are
/// none, and a file rewritten as "[]" would silently stop prometheus probing
/// everything it names.
/// </summary>
public class TargetFileWriter
{
    private readonly ILogger _logger;
    private readonly int _maxStaleCycles;

    /// <summary>Consecutive incomplete passes per output path.</summary>
    private readonly Dictionary<string, int> _degradedPasses = new();

    /// <param name="maxStaleCycles">
    /// How many consecutive incomplete passes may keep serving the previous file before
    /// the incomplete list is written anyway.  Zero or less holds the last good file for
    /// as long as the failure lasts.
    /// </param>
    public TargetFileWriter(ILogger logger, int maxStaleCycles = 0)
    {
        _logger = logger;
        _maxStaleCycles = maxStaleCycles;
    }

    public Task<bool> WriteAsync(string outputPath, Func<Stream, Task> generate)
    {
        return WriteAsync(outputPath, async stream =>
        {
            await generate(stream);
            return true;
        });
    }

    /// <summary>
    /// Generates and writes one output file.  An output path that is not configured
    /// disables that output.
    /// </summary>
    /// <param name="generate">
    /// Writes the target list to the stream and returns false when lookups failed and
    /// the list it produced is incomplete.
    /// </param>
    /// <returns>False when nothing was written, either because the output is disabled
    /// or because the previous file was kept instead.</returns>
    public async Task<bool> WriteAsync(string outputPath, Func<Stream, Task<bool>> generate)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return false;
        }

        string tempPath = $"{outputPath}.tmp";
        bool complete;
        try
        {
            await using (var fs = new FileStream(tempPath, FileMode.Create))
            {
                complete = await generate(fs);
            }
        }
        catch (Exception)
        {
            // Leave the destination alone.  A half generated list is never better than
            // the last good one.
            TryDelete(tempPath);
            throw;
        }

        if (complete)
        {
            _degradedPasses.Remove(outputPath);
        }
        else if (!ShouldWriteDegraded(outputPath))
        {
            TryDelete(tempPath);
            return false;
        }

        File.Move(tempPath, outputPath, true);
        _logger?.LogDebug($"{outputPath} written");
        return true;
    }

    private bool ShouldWriteDegraded(string outputPath)
    {
        int consecutive = _degradedPasses.TryGetValue(outputPath, out int previous) ? previous + 1 : 1;
        _degradedPasses[outputPath] = consecutive;

        if (!File.Exists(outputPath))
        {
            _logger?.LogWarning(
                $"{outputPath} pass was incomplete and there is no previous file to keep, writing what was discovered");
            return true;
        }

        if (_maxStaleCycles > 0 && consecutive > _maxStaleCycles)
        {
            _logger?.LogWarning(
                $"{outputPath} pass has been incomplete {consecutive} times, past MaxStaleCycles of {_maxStaleCycles}, writing what was discovered");
            return true;
        }

        _logger?.LogWarning(
            $"{outputPath} pass was incomplete, keeping the previous file so the endpoints stay in prometheus (incomplete pass {consecutive})");
        return false;
    }

    private void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, $"{path} could not be removed");
        }
    }
}
