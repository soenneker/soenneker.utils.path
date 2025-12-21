using Soenneker.Utils.Path.Abstract;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Diagnostics.Contracts;
using Soenneker.Asyncs.Locks;
using Soenneker.Extensions.ValueTask;

namespace Soenneker.Utils.Path;

/// <inheritdoc cref="IPathUtil"/>
public sealed class PathUtil : IPathUtil
{
    private readonly Lazy<AsyncLock> _asyncLock = new();

    /// <summary>
    /// Convenience method to get the temp directory for the current OS. (Path.GetTempPath())
    /// </summary>
    /// <returns></returns>
    public static string GetTempDirectory()
    {
        return System.IO.Path.GetTempPath();
    }

    /// <summary>
    /// Retrieves the last segment of a file or directory path. OS-agnostic.
    /// </summary>
    /// <param name="path">The path to process.</param>
    /// <returns>The last segment of the path, or null if the path is empty.</returns>
    [Pure]
    public static string? GetLastPathSegment(string path)
    {
        return path.Split([System.IO.Path.DirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
    }

    public async ValueTask<string> GetUniqueFilePathFromUri(string directory, string uri, CancellationToken cancellationToken = default)
    {
        string fileName = System.IO.Path.GetFileName(new Uri(uri).AbsolutePath);
        string filePath = System.IO.Path.Combine(directory, fileName);
        var count = 1;
        string fileExtension = System.IO.Path.GetExtension(filePath);
        string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(filePath);

        while (true)
        {
            string tempFilePath = filePath;

            using (await _asyncLock.Value.Lock(cancellationToken).NoSync())
            {
                if (!File.Exists(tempFilePath))
                    return tempFilePath;

                var tempFileName = $"{fileNameWithoutExtension}({count++}){fileExtension}";
                filePath = System.IO.Path.Combine(directory, tempFileName);
            }
        }
    }

    public async ValueTask<string> GetRandomUniqueFilePath(string directory, string fileExtension, CancellationToken cancellationToken = default)
    {
        if (!fileExtension.StartsWith('.'))
            fileExtension = '.' + fileExtension;

        while (true)
        {
            var randomFileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = System.IO.Path.Combine(directory, randomFileName);

            using (await _asyncLock.Value.Lock(cancellationToken).NoSync())
            {
                // Check if the file path already exists
                if (!File.Exists(filePath))
                    return filePath;
            }
        }
    }

    public async ValueTask<string> GetRandomTempFilePath(string fileExtension, CancellationToken cancellationToken = default)
    {
        if (!fileExtension.StartsWith('.'))
            fileExtension = '.' + fileExtension;

        string tempDirectory = GetTempDirectory();

        while (true)
        {
            var randomFileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = System.IO.Path.Combine(tempDirectory, randomFileName);

            using (await _asyncLock.Value.Lock(cancellationToken).NoSync())
            {
                if (!File.Exists(filePath))
                    return filePath;
            }
        }
    }

    public async ValueTask<string> GetUniqueTempDirectory(string? prefix = null, bool create = true, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var dirName = $"{prefix ?? "temp"}_{Guid.NewGuid()}";
            string fullPath = System.IO.Path.Combine(GetTempDirectory(), dirName);

            using (await _asyncLock.Value.Lock(cancellationToken).NoSync())
            {
                if (Directory.Exists(fullPath)) 
                    continue;

                if (create)
                    Directory.CreateDirectory(fullPath);

                return fullPath;
            }
        }
    }
}