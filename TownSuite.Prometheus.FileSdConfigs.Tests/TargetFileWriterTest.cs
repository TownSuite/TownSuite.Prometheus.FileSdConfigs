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
    public async Task TryWriteAsync_GeneratorThrows_ReturnsFalseAndLeavesThePreviousFileIntact()
    {
        // A source returning 503 for a few minutes became an exception inside a pass, the
        // process exited, and a mis-parsed RestartSec kept it down long after the source recovered.
        // The main loop goes through this entry point so a failed pass ends at the log.
        string target = Path_("targets.json");
        await File.WriteAllTextAsync(target, "[{\"targets\":[\"previous\"]}]");
        var writer = new TargetFileWriter(null);

        bool written = await writer.TryWriteAsync(target,
            _ => throw new NullReferenceException("source answered 503, lookup came back null"));

        Assert.That(written, Is.False);
        Assert.That(File.ReadAllText(target), Is.EqualTo("[{\"targets\":[\"previous\"]}]"));
        Assert.That(File.Exists(target + ".tmp"), Is.False, "the half-written temp file must be cleaned up");
    }

    [Test]
    public void OutputPathInAMissingDirectory_Throws()
    {
        string target = Path.Combine(_dir, "does_not_exist", "targets.json");
        var writer = new TargetFileWriter(null);

        // WriteAsync still throws on an unusable output path. Program.cs reaches it through
        // TryWriteAsync, so the error is logged every cycle until the path is fixed instead
        // of ending the process.
        Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await writer.WriteAsync(target, Writes("[]")));
    }

    [Test]
    public async Task IncompletePass_KeepsThePreviousFile()
    {
        string target = Path_("targets_v2.json");
        var writer = new TargetFileWriter(null);
        await writer.WriteAsync(target, Writes("[{\"targets\":[\"https://a/healthz\"]}]"));

        // A lookup failed, so the list generated this pass is missing endpoints.
        bool written = await writer.WriteAsync(target, async stream =>
        {
            await Writes("[]")(stream);
            return false;
        });

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.False);
            Assert.That(File.ReadAllText(target), Is.EqualTo("[{\"targets\":[\"https://a/healthz\"]}]"),
                "a temporary lookup failure must not empty the file prometheus is reading");
            Assert.That(File.Exists($"{target}.tmp"), Is.False);
        });
    }

    [Test]
    public async Task IncompletePass_WithNoPreviousFile_WritesWhatItHas()
    {
        string target = Path_("targets_v2.json");
        var writer = new TargetFileWriter(null);

        bool written = await writer.WriteAsync(target, async stream =>
        {
            await Writes("[{\"targets\":[\"https://a/healthz\"]}]")(stream);
            return false;
        });

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.True, "there is no previous file to fall back to");
            Assert.That(File.ReadAllText(target), Does.Contain("https://a/healthz"));
        });
    }

    [Test]
    public async Task RecoveredPass_PublishesAgain()
    {
        string target = Path_("targets_v2.json");
        var writer = new TargetFileWriter(null);
        await writer.WriteAsync(target, Writes("[\"first\"]"));
        await writer.WriteAsync(target, async stream => { await Writes("[]")(stream); return false; });

        bool written = await writer.WriteAsync(target, Writes("[\"third\"]"));

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.True);
            Assert.That(File.ReadAllText(target), Is.EqualTo("[\"third\"]"));
        });
    }

    [Test]
    public async Task MaxStaleCycles_EventuallyPublishesTheIncompleteList()
    {
        string target = Path_("targets_v2.json");
        var writer = new TargetFileWriter(null, maxStaleCycles: 2);
        await writer.WriteAsync(target, Writes("[\"good\"]"));

        Func<Stream, Task<bool>> degraded = async stream =>
        {
            await Writes("[\"degraded\"]")(stream);
            return false;
        };

        Assert.Multiple(() =>
        {
            Assert.That(writer.WriteAsync(target, degraded).Result, Is.False, "pass 1 holds");
            Assert.That(writer.WriteAsync(target, degraded).Result, Is.False, "pass 2 holds");
            Assert.That(writer.WriteAsync(target, degraded).Result, Is.True, "pass 3 is past the budget");
            Assert.That(File.ReadAllText(target), Is.EqualTo("[\"degraded\"]"));
        });
    }

    [Test]
    public async Task MaxStaleCycles_CounterResetsAfterAGoodPass()
    {
        string target = Path_("targets_v2.json");
        var writer = new TargetFileWriter(null, maxStaleCycles: 1);
        await writer.WriteAsync(target, Writes("[\"good\"]"));

        Func<Stream, Task<bool>> degraded = async stream =>
        {
            await Writes("[\"degraded\"]")(stream);
            return false;
        };

        await writer.WriteAsync(target, degraded);
        await writer.WriteAsync(target, Writes("[\"good again\"]"));
        bool written = await writer.WriteAsync(target, degraded);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.False, "the budget starts over after a good pass");
            Assert.That(File.ReadAllText(target), Is.EqualTo("[\"good again\"]"));
        });
    }

    [Test]
    public async Task GeneratorThrows_RemovesTheTempFile()
    {
        string target = Path_("targets.json");
        var writer = new TargetFileWriter(null);

        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await writer.WriteAsync(target, _ => throw new HttpRequestException("lookup failed")));

        Assert.That(File.Exists($"{target}.tmp"), Is.False, "a failed pass should not leave temp files behind");
        await Task.CompletedTask;
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
