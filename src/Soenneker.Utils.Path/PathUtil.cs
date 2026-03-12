using Soenneker.Utils.Path.Abstract;
using Soenneker.Utils.ExecutionContexts;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.String;

namespace Soenneker.Utils.Path;

/// <inheritdoc cref="IPathUtil"/>
public sealed class PathUtil : IPathUtil
{
    // Temp path is effectively stable for the process lifetime.
    private static readonly string _tempDirectory = System.IO.Path.GetTempPath();

    /// <summary>
    /// Convenience method to get the temp directory for the current OS. (Path.GetTempPath())
    /// </summary>
    [Pure]
    public static string GetTempDirectory() => _tempDirectory;

    /// <summary>
    /// Extracts the last segment of a file system path, excluding any trailing directory separators.
    /// </summary>
    /// <remarks>Trailing directory separators are ignored when determining the last segment. The method does
    /// not validate the existence of the path or its segments.</remarks>
    /// <param name="path">The file system path from which to retrieve the last segment. Can be absolute or relative. Cannot be null or
    /// empty.</param>
    /// <returns>The last segment of the specified path as a string, or null if the path is null, empty, or consists only of
    /// separators.</returns>
    [Pure]
    public static string? GetLastPathSegment(string path)
    {
        if (path.IsNullOrEmpty())
            return null;

        ReadOnlySpan<char> span = path.AsSpan();

        // Trim trailing separators
        int end = span.Length - 1;
        while (end >= 0)
        {
            char c = span[end];
            if (c != System.IO.Path.DirectorySeparatorChar && c != System.IO.Path.AltDirectorySeparatorChar)
                break;

            end--;
        }

        if (end < 0)
            return null;

        span = span[..(end + 1)];

        // Find last separator (either kind)
        int lastSep = -1;
        for (int i = span.Length - 1; i >= 0; i--)
        {
            char c = span[i];
            if (c == System.IO.Path.DirectorySeparatorChar || c == System.IO.Path.AltDirectorySeparatorChar)
            {
                lastSep = i;
                break;
            }
        }

        ReadOnlySpan<char> segment = lastSep >= 0 ? span[(lastSep + 1)..] : span;

        if (segment.IsEmpty)
            return null;

        return segment.ToString();
    }

    /// <summary>
    /// Generates a unique file path within the specified directory based on the provided URI, ensuring that the file
    /// does not already exist.
    /// </summary>
    /// <remarks>This method atomically reserves the file path by creating the file, preventing race
    /// conditions across threads and processes. If the derived file name already exists, numeric suffixes are appended
    /// until a unique name is found. The caller is responsible for handling or overwriting the reserved file as needed.
    /// Throws a DirectoryNotFoundException if the specified directory does not exist.</remarks>
    /// <param name="directory">The directory in which to create the unique file. Must be a valid, existing directory path.</param>
    /// <param name="uri">The URI or file name used to derive the base name for the file. Can be an absolute URI or a simple file name.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A string containing the full path to a newly reserved, unique file within the specified directory. The file is
    /// created empty and can be safely overwritten or written to by the caller.</returns>
    [Pure]
    public async ValueTask<string> GetUniqueFilePathFromUri(string directory, string uri, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fileName;

        if (Uri.TryCreate(uri, UriKind.Absolute, out Uri? parsed))
            fileName = System.IO.Path.GetFileName(parsed.LocalPath);
        else
            fileName = System.IO.Path.GetFileName(uri);

        if (string.IsNullOrEmpty(fileName))
            fileName = "file";

        string extension = System.IO.Path.GetExtension(fileName);
        string baseName = System.IO.Path.GetFileNameWithoutExtension(fileName);

        // Try base name first, then add (n) suffixes. We "reserve" atomically by creating the file.
        // This removes races across threads and processes.
        for (var count = 0;; count++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string candidateName = count == 0 ? fileName : string.Concat(baseName, "(", count.ToString(), ")", extension);

            string candidatePath = System.IO.Path.Combine(directory, candidateName);

            try
            {
                // Reserve the path. Caller can overwrite later with FileMode.Create if desired.
                // Use FileShare.None to avoid others opening it while reserved.
                await ExecutionContextUtil.RunInlineOrOffload(static s =>
                {
                    var path = (string)s!;
                    using (new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.None))
                    {
                    }
                }, candidatePath, cancellationToken);

                return candidatePath;
            }
            catch (DirectoryNotFoundException)
            {
                // Mirror prior behavior: let this bubble as it's a usage/config issue.
                throw;
            }
            catch (IOException)
            {
                // Exists / collision / race - retry with next suffix
            }
        }
    }

    /// <summary>
    /// Generates a random, unique file path within the specified directory using a GUID-based file name and the given
    /// file extension.
    /// </summary>
    /// <remarks>The generated file path is guaranteed not to exist at the time of creation. This method is
    /// thread-safe and suitable for scenarios where a temporary or unique file name is required. GUID collisions are
    /// extremely unlikely, and an existence check is performed as an additional safeguard.</remarks>
    /// <param name="directory">The directory in which to generate the unique file path. Must be a valid, existing directory path.</param>
    /// <param name="fileExtension">The file extension to use for the generated file name. If null or empty, ".tmp" is used. If the extension does
    /// not start with a period (.), one is automatically prepended.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation before completion.</param>
    /// <returns>A task that represents the asynchronous operation. The value of the task contains a string with the full path to
    /// a non-existent file in the specified directory.</returns>
    [Pure]
    public ValueTask<string> GetRandomUniqueFilePath(string directory, string fileExtension, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (fileExtension.IsNullOrEmpty())
            fileExtension = ".tmp";
        else if (fileExtension[0] != '.')
            fileExtension = "." + fileExtension;

        return ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            (string dir, string ext, CancellationToken ct) = ((string, string, CancellationToken))s!;

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                string fileName = string.Concat(Guid.NewGuid().ToString("N"), ext);
                string filePath = System.IO.Path.Combine(dir, fileName);
                if (!File.Exists(filePath))
                    return filePath;
            }
        }, (directory, fileExtension, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Generates a unique file path in the temporary directory with the specified file extension, ensuring that the
    /// file does not already exist.
    /// </summary>
    /// <remarks>The returned file path is guaranteed not to exist at the time of generation, but the file is
    /// not created by this method. Callers are responsible for creating or reserving the file if necessary. This method
    /// is thread-safe and can be safely called concurrently.</remarks>
    /// <param name="fileExtension">The file extension to use for the generated file path. If null or empty, ".tmp" is used by default. The
    /// extension should include the leading period; if omitted, one will be added automatically.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation before completion.</param>
    /// <returns>A task that represents the asynchronous operation. The value of the task contains the full path to a
    /// non-existent temporary file with the specified extension.</returns>
    [Pure]
    public ValueTask<string> GetRandomTempFilePath(string fileExtension, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(fileExtension))
            fileExtension = ".tmp";
        else if (fileExtension[0] != '.')
            fileExtension = "." + fileExtension;

        return ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            (string tempDir, string ext, CancellationToken ct) = ((string, string, CancellationToken))s!;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                string fileName = string.Concat(Guid.NewGuid().ToString("N"), ext);
                string filePath = System.IO.Path.Combine(tempDir, fileName);

                if (!File.Exists(filePath))
                    return filePath;
            }
        }, (_tempDirectory, fileExtension, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Generates a unique temporary directory path, optionally creating the directory on disk.
    /// </summary>
    /// <remarks>The generated directory name is guaranteed to be unique within the temporary directory. If
    /// <paramref name="create"/> is <see langword="true"/>, the method ensures the directory exists before returning.
    /// This method is thread-safe and can be safely called concurrently.</remarks>
    /// <param name="prefix">An optional prefix for the directory name. If null or empty, "temp" is used as the default prefix.</param>
    /// <param name="create">If <see langword="true"/>, the directory is created on disk before returning the path; otherwise, only a unique
    /// path is generated without creating the directory.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation before completion.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the full path to the unique temporary
    /// directory.</returns>
    [Pure]
    public ValueTask<string> GetUniqueTempDirectory(string? prefix = null, bool create = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        prefix = string.IsNullOrEmpty(prefix) ? "temp" : prefix;

        return ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            (string tempDir, string pfx, bool doCreate, CancellationToken ct) = ((string, string, bool, CancellationToken))s!;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                string dirName = string.Concat(pfx, "_", Guid.NewGuid().ToString("N"));
                string fullPath = System.IO.Path.Combine(tempDir, dirName);

                if (!doCreate)
                {
                    if (!Directory.Exists(fullPath))
                        return fullPath;
                    continue;
                }

                try
                {
                    DirectoryInfo info = Directory.CreateDirectory(fullPath);
                    if (string.Equals(info.FullName, fullPath, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(fullPath))
                        return fullPath;
                }
                catch (IOException)
                {
                    // Collision / transient - retry
                }
            }
        }, (_tempDirectory, prefix, create, cancellationToken), cancellationToken);
    }
}