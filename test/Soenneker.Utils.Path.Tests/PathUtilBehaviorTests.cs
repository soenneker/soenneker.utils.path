using Soenneker.Utils.MemoryStream;
using Microsoft.Extensions.Logging.Abstractions;
using Soenneker.Utils.File.Abstract;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.Path.Tests;

public class PathUtilBehaviorTests
{
    private readonly IFileUtil _fileUtil = new Soenneker.Utils.File.FileUtil(NullLogger<Soenneker.Utils.File.FileUtil>.Instance, new MemoryStreamUtil());

    [Test]
    public void LastSegmentPreservesWholeInputAndPlatformSeparators()
    {
        string input = new(['f', 'i', 'l', 'e']);
        Check(ReferenceEquals(input, PathUtil.GetLastPathSegment(input)));
        Check(PathUtil.GetLastPathSegment("one/two///") == "two");
        Check(PathUtil.GetLastPathSegment("///") is null);
        Check(PathUtil.GetLastPathSegment(null!) is null);
        Check(PathUtil.GetLastPathSegment(@"one\two\") == (OperatingSystem.IsWindows() ? "two" : @"one\two\"));
    }

    [Test]
    public async Task RandomPathsNormalizePortableExtensionsWithoutReserving()
    {
        var util = new PathUtil();
        foreach ((string? extension, string expected) in new (string?, string)[]
                 { (null, ".tmp"), ("", ".tmp"), ("txt", ".txt"), (".txt", ".txt"), (@"folder\txt", ".txt"), ("folder/txt", ".txt"), ("folder/", ".tmp") })
        {
            string path = await util.GetRandomUniqueFilePath(PathUtil.GetTempDirectory(), extension!);
            string name = System.IO.Path.GetFileName(path);
            Check(name.Length == 32 + expected.Length && name.EndsWith(expected, StringComparison.Ordinal));
            Check(Guid.TryParseExact(name[..32], "N", out _));
            Check(!(await _fileUtil.Exists(path)));
        }
    }

    [Test]
    public async Task ReservationsHandleConcurrentCollisionsAndEscapedUris()
    {
        string root = Directory.CreateTempSubdirectory("path-tests-").FullName;
        try
        {
            var util = new PathUtil();
            await _fileUtil.Write(System.IO.Path.Combine(root, "a b.txt"), "keep");
            await _fileUtil.Write(System.IO.Path.Combine(root, "a b(1).txt"), "keep");
            string[] paths = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(async () =>
                await util.GetUniqueFilePathFromUri(root, "https://example.com/a%20b.txt?ignored=true"))));
            Check(paths.Distinct().Count() == 16);
            foreach (string path in paths)
                Check(await _fileUtil.Exists(path));
            Check(paths.Contains(System.IO.Path.Combine(root, "a b(2).txt")));
            Check((await _fileUtil.Read(System.IO.Path.Combine(root, "a b(1).txt"))) == "keep");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public async Task TempDirectoriesNormalizePrefixesAndHonorCreate()
    {
        var util = new PathUtil();
        string uncreated = await util.GetUniqueTempDirectory(@"parent\child/", false);
        Check(System.IO.Path.GetFileName(uncreated).StartsWith("child_", StringComparison.Ordinal));
        Check(!Directory.Exists(uncreated));
        string created = await util.GetUniqueTempDirectory("parent/child/", true);
        try
        {
            Check(Directory.Exists(created));
            Check(System.IO.Path.GetFileName(created).StartsWith("child_", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(created);
        }
    }

    [Test]
    public async Task CanceledOperationsDoNotCreatePaths()
    {
        var util = new PathUtil();
        var token = new CancellationToken(true);
        foreach (Func<ValueTask<string>> operation in new Func<ValueTask<string>>[]
                 {
                     () => util.GetRandomUniqueFilePath("", ".txt", token),
                     () => util.GetRandomTempFilePath(".txt", token),
                     () => util.GetUniqueTempDirectory(cancellationToken: token),
                     () => util.GetUniqueFilePathFromUri("", "file.txt", token)
                 })
        {
            try
            {
                await operation();
                throw new InvalidOperationException("Expected cancellation.");
            }
            catch (OperationCanceledException exception)
            {
                Check(exception.CancellationToken == token);
            }
        }
    }

    [Test]
    public async Task ReservationsCompleteWithASynchronizationContext()
    {
        string root = Directory.CreateTempSubdirectory("path-context-tests-").FullName;
        try
        {
            await _fileUtil.Write(System.IO.Path.Combine(root, "file.txt"), "keep");
            ValueTask<string> pending;
            SynchronizationContext? original = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                pending = new PathUtil().GetUniqueFilePathFromUri(root, "file.txt");
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(original);
            }
            string path = await pending;
            Check((await _fileUtil.Exists(path)) && System.IO.Path.GetFileName(path) == "file(1).txt");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public async Task MissingDirectoryErrorsPropagate()
    {
        string root = System.IO.Path.Combine(PathUtil.GetTempDirectory(), Guid.NewGuid().ToString("N"), "missing");
        try
        {
            await new PathUtil().GetUniqueFilePathFromUri(root, "file.txt");
            throw new InvalidOperationException("Expected a missing-directory error.");
        }
        catch (DirectoryNotFoundException)
        {
            Check(!Directory.Exists(root));
        }
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("Path behavior assertion failed.");
    }
}
