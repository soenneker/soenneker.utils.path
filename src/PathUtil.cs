using Soenneker.Utils.Path.Abstract;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Nito.AsyncEx;

namespace Soenneker.Utils.Path;

/// <inheritdoc cref="IPathUtil"/>
public class PathUtil: IPathUtil
{
    private readonly Lazy<AsyncLock> _asyncLock = new();

    public PathUtil()
    {
    }

    /// <summary>
    /// Retrieves the last segment of a file or directory path. OS agnostic.
    /// </summary>
    /// <param name="path">The path to process.</param>
    /// <returns>The last segment of the path, or null if the path is empty.</returns>
    public static string? GetLastPathSegment(string path)
    {
        string? lastPathSegment = path
            .Split(new[] { System.IO.Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        return lastPathSegment;
    }

    public async ValueTask<string> GetThreadSafeUniqueFilePath(string directory, string uri, CancellationToken cancellationToken)
    {
        string fileName = System.IO.Path.GetFileName(new Uri(uri).AbsolutePath);
        string filePath = System.IO.Path.Combine(directory, fileName);
        int count = 1;
        string fileExtension = System.IO.Path.GetExtension(filePath);
        string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(filePath);

        while (true)
        {
            string tempFilePath = filePath;

            using (await _asyncLock.Value.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!System.IO.File.Exists(tempFilePath))
                {
                    return tempFilePath;
                }

                string tempFileName = $"{fileNameWithoutExtension}({count++}){fileExtension}";
                filePath = System.IO.Path.Combine(directory, tempFileName);
            }
        }
    }
}
