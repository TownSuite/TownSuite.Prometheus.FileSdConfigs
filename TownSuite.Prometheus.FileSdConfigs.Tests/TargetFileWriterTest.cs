using System.Text;
using NUnit.Framework;

namespace TownSuite.Prometheus.FileSdConfigs.Tests;

public class TargetFileWriterTest
{
    private string _dir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"filesdconfigs_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private string Path_(string name) => Path.Combine(_dir, name);

    private static Func<Stream, Task> Writes(string content)
    {
        return async stream =>
        {
            await using var sw = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
            await sw.WriteAsync(content);
        };
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task UnconfiguredOutputPath_WritesNothing(string outputPath)
    {
        bool generatorRan = false;
        var writer = new TargetFileWriter(null);

        bool written = await writer.WriteAsync(outputPath, _ =>
        {
            generatorRan = true;
            return Task.CompletedTask;
        });

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.False, "an unconfigured output should report that it was skipped");
            Assert.That(generatorRan, Is.False, "service discovery should not run for a disabled output");
            Assert.That(Directory.GetFiles(_dir), Is.Empty);
        });
    }

    [Test]
    public async Task ConfiguredOutputPath_WritesTheFileAndRemovesTheTempFile()
    {
        string target = Path_("targets_open_telemetry_metrics.json");
        var writer = new TargetFileWriter(null);

        bool written = await writer.WriteAsync(target, Writes("[]"));

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.True);
            Assert.That(File.ReadAllText(target), Is.EqualTo("[]"));
            Assert.That(File.Exists($"{target}.tmp"), Is.False, "the temp file should have been moved, not copied");
        });
    }

    [Test]
    public async Task ExistingFile_IsReplaced()
    {
        string target = Path_("targets.json");
        await File.WriteAllTextAsync(target, "[{\"targets\":[\"stale\"]}]");
        var writer = new TargetFileWriter(null);

        await writer.WriteAsync(target, Writes("[{\"targets\":[\"fresh\"]}]"));

        Assert.That(File.ReadAllText(target), Is.EqualTo("[{\"targets\":[\"fresh\"]}]"));
    }

    [Test]
    public async Task GeneratorThrows_LeavesThePreviousFileIntact()
    {
        string target = Path_("targets.json");
        await File.WriteAllTextAsync(target, "[{\"targets\":[\"previous\"]}]");
        var writer = new TargetFileWriter(null);

        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await writer.WriteAsync(target, _ => throw new HttpRequestException("lookup failed")));

        Assert.That(File.ReadAllText(target), Is.EqualTo("[{\"targets\":[\"previous\"]}]"),
            "a failed pass must not replace the last good file prometheus is reading");
    }

    [Test]
    public void OutputPathInAMissingDirectory_Throws()
    {
        string target = Path.Combine(_dir, "does_not_exist", "targets.json");
        var writer = new TargetFileWriter(null);

        // Documents current behaviour: an unusable output path is fatal, and Program.cs
        // turns that into a LogCritical + exit so the service manager restarts it.
        Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await writer.WriteAsync(target, Writes("[]")));
    }

    [Test]
    public async Task EveryConfiguredOutput_IsWrittenIndependently()
    {
        var writer = new TargetFileWriter(null);
        string enabled = Path_("targets_v2.json");
        string alsoEnabled = Path_("targets_open_telemetry_metrics.json");

        await writer.WriteAsync(enabled, Writes("[1]"));
        await writer.WriteAsync(string.Empty, Writes("[2]"));
        await writer.WriteAsync(alsoEnabled, Writes("[3]"));

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllText(enabled), Is.EqualTo("[1]"));
            Assert.That(File.ReadAllText(alsoEnabled), Is.EqualTo("[3]"));
            Assert.That(Directory.GetFiles(_dir).Length, Is.EqualTo(2));
        });
    }
}
