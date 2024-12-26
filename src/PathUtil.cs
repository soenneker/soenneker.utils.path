using Soenneker.Utils.Path.Abstract;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Nito.AsyncEx;
using System.IO;
using System.Diagnostics.Contracts;

namespace Soenneker.Utils.Path;

/// <inheritdoc cref="IPathUtil"/>
public class PathUtil : IPathUtil
{
    private readonly Lazy<AsyncLock> _asyncLock = new();

    /// <summary>
    /// Retrieves the last segment of a file or directory path. OS-agnostic.
    /// </summary>
    /// <param name="path">The path to process.</param>
    /// <returns>The last segment of the path, or null if the path is empty.</returns>
    [Pure]
    public static string? GetLastPathSegment(string path)
    {
        string? lastPathSegment = path
            .Split([System.IO.Path.DirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        return lastPathSegment;
    }

    public async ValueTask<string> GetThreadSafeUniqueFilePath(string directory, string uri, CancellationToken cancellationToken = default)
    {
        string fileName = System.IO.Path.GetFileName(new Uri(uri).AbsolutePath);
        string filePath = System.IO.Path.Combine(directory, fileName);
        var count = 1;
        string fileExtension = System.IO.Path.GetExtension(filePath);
        string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(filePath);

        while (true)
        {
            string tempFilePath = filePath;

            using (await _asyncLock.Value.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!File.Exists(tempFilePath))
                    return tempFilePath;

                string tempFileName = $"{fileNameWithoutExtension}({count++}){fileExtension}";
                filePath = System.IO.Path.Combine(directory, tempFileName);
            }
        }
    }

    public async ValueTask<string> GetThreadSafeRandomUniqueFilePath(string directory, string fileExtension, CancellationToken cancellationToken = default)
    {
        if (!fileExtension.StartsWith('.'))
            fileExtension = '.' + fileExtension;

        while (true)
        {
            var randomFileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = System.IO.Path.Combine(directory, randomFileName);

            using (await _asyncLock.Value.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                // Check if the file path already exists
                if (!File.Exists(filePath))
                    return filePath;
            }
        }
    }

    public async ValueTask<string> GetThreadSafeTempUniqueFilePath(string fileExtension, CancellationToken cancellationToken = default)
    {
        if (!fileExtension.StartsWith('.'))
            fileExtension = '.' + fileExtension;

        string tempDirectory = System.IO.Path.GetTempPath();

        while (true)
        {
            var randomFileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = System.IO.Path.Combine(tempDirectory, randomFileName);

            using (await _asyncLock.Value.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!File.Exists(filePath))
                    return filePath;
            }
        }
    }
}