using Soenneker.Utils.Path.Abstract;
using Soenneker.Utils.ExecutionContexts;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.String;

namespace Soenneker.Utils.Path;

public sealed class PathUtil : IPathUtil
{
    // Temp path is effectively stable for the process lifetime.
    private static readonly string _tempDirectory = System.IO.Path.GetTempPath();

    /// <summary>
    /// Convenience method to get the temp directory for the current OS. (Path.GetTempPath())
    /// </summary>
    /// <returns>Convenience method to get the temp directory for the current OS. (Path.GetTempPath()).</returns>
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
        int lastSep = span.LastIndexOfAny(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

        return path.Substring(lastSep + 1, end - lastSep);
    }

    public async ValueTask<string> GetUniqueFilePathFromUri(string directory, string uri, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await ExecutionContextUtil.RunInlineOrOffload(
            static ((string directory, string uri, CancellationToken cancellationToken) state) =>
                ReserveUniqueFilePath(state.directory, state.uri, state.cancellationToken),
            (directory, uri, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    private static string ReserveUniqueFilePath(string directory, string uri, CancellationToken cancellationToken)
    {
        string fileName = Uri.TryCreate(uri, UriKind.Absolute, out Uri? parsed)
            ? System.IO.Path.GetFileName(parsed.LocalPath)
            : System.IO.Path.GetFileName(uri);

        if (fileName.IsNullOrEmpty())
            fileName = "file";

        ReadOnlySpan<char> extension = default;
        ReadOnlySpan<char> baseName = default;

        for (var count = 0;; count++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (count == 1)
            {
                extension = System.IO.Path.GetExtension(fileName.AsSpan());
                baseName = System.IO.Path.GetFileNameWithoutExtension(fileName.AsSpan());
            }

            string candidatePath = count == 0
                ? System.IO.Path.Combine(directory, fileName)
                : CreateSuffixedPath(directory, baseName, extension, count);

            // Once a collision is known, skip occupied suffixes without creating exceptions.
            // CreateNew still reserves atomically if another caller wins after this check.
            if (count != 0 && File.Exists(candidatePath))
                continue;

            try
            {
                using (File.OpenHandle(candidatePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, FileOptions.None))
                {
                }
                return candidatePath;
            }
            catch (DirectoryNotFoundException)
            {
                throw;
            }
            catch (IOException) when (File.Exists(candidatePath) || Directory.Exists(candidatePath))
            {
                // Another caller may have reserved the candidate after the existence check.
            }
        }
    }

    private static string CreateSuffixedPath(string directory, ReadOnlySpan<char> baseName, ReadOnlySpan<char> extension, int count)
    {
        if (System.IO.Path.IsPathRooted(baseName))
            directory = "";

        ReadOnlySpan<char> separator = directory.Length != 0 && !System.IO.Path.EndsInDirectorySeparator(directory)
            ? (System.IO.Path.DirectorySeparatorChar == '/' ? "/" : "\\")
            : default;
        return string.Create(null, $"{directory}{separator}{baseName}({count}){extension}");
    }

    public ValueTask<string> GetRandomUniqueFilePath(string directory, string fileExtension, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ExecutionContextUtil.RunInlineOrOffload(static ((string dir, string ext, CancellationToken ct) s) =>
        {
            (string dir, string ext, CancellationToken ct) = s;
            ReadOnlySpan<char> extension = NormalizeExtension(ext);

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                string filePath = CreateRandomPath(dir, default, extension, true);
                if (!File.Exists(filePath))
                    return filePath;
            }
        }, (directory, fileExtension, cancellationToken), cancellationToken);
    }

    public ValueTask<string> GetRandomTempFilePath(string fileExtension, CancellationToken cancellationToken = default)
    {
        return GetRandomUniqueFilePath(_tempDirectory, fileExtension, cancellationToken);
    }

    public ValueTask<string> GetUniqueTempDirectory(string? prefix = null, bool create = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ExecutionContextUtil.RunInlineOrOffload(static ((string tempDir, string? pfx, bool doCreate, CancellationToken ct) s) =>
        {
            (string tempDir, string? pfx, bool doCreate, CancellationToken ct) = s;
            ReadOnlySpan<char> normalizedPrefix = GetPortableFileName(pfx, trimTrailingSeparators: true);
            if (normalizedPrefix.IsEmpty)
                normalizedPrefix = "temp";

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                string fullPath = CreateRandomPath(tempDir, normalizedPrefix, default, false);

                if (!doCreate)
                {
                    if (!Directory.Exists(fullPath))
                        return fullPath;
                    continue;
                }

                try
                {
                    DirectoryInfo info = Directory.CreateDirectory(fullPath);
                    return info.FullName;
                }
                catch (IOException)
                {
                    // Collision / transient - retry
                }
            }
        }, (_tempDirectory, prefix, create, cancellationToken), cancellationToken);
    }

    private static string CreateRandomPath(string directory, ReadOnlySpan<char> prefix, ReadOnlySpan<char> extension, bool file)
    {
        ArgumentNullException.ThrowIfNull(directory, "path1");

        // Preserve Path.Combine semantics for drive-relative prefixes on Windows.
        if (System.IO.Path.IsPathRooted(prefix))
            directory = "";

        bool separator = directory.Length != 0 && !System.IO.Path.EndsInDirectorySeparator(directory);
        bool dot = file && extension[0] != '.';
        int length = checked(directory.Length + (separator ? 1 : 0) + prefix.Length + (prefix.Length != 0 ? 1 : 0) + 32 + (dot ? 1 : 0) + extension.Length);
        return string.Create(length, new RandomPathState(directory, prefix, extension, separator, dot), static (destination, state) =>
        {
            ReadOnlySpan<char> dir = state.Directory;
            ReadOnlySpan<char> pfx = state.Prefix;
            ReadOnlySpan<char> ext = state.Extension;
            dir.CopyTo(destination);
            int offset = dir.Length;
            if (state.Separator)
                destination[offset++] = System.IO.Path.DirectorySeparatorChar;
            pfx.CopyTo(destination[offset..]);
            offset += pfx.Length;
            if (pfx.Length != 0)
                destination[offset++] = '_';
            Guid.NewGuid().TryFormat(destination.Slice(offset, 32), out _, "N");
            offset += 32;
            if (state.Dot)
                destination[offset++] = '.';
            ext.CopyTo(destination[offset..]);
        });
    }

    private readonly ref struct RandomPathState(ReadOnlySpan<char> directory, ReadOnlySpan<char> prefix, ReadOnlySpan<char> extension, bool separator, bool dot)
    {
        public readonly ReadOnlySpan<char> Directory = directory;
        public readonly ReadOnlySpan<char> Prefix = prefix;
        public readonly ReadOnlySpan<char> Extension = extension;
        public readonly bool Separator = separator;
        public readonly bool Dot = dot;
    }

    private static ReadOnlySpan<char> NormalizeExtension(string? fileExtension)
    {
        ReadOnlySpan<char> extension = GetPortableFileName(fileExtension, trimTrailingSeparators: false);
        return extension.IsEmpty ? ".tmp" : extension;
    }

    private static ReadOnlySpan<char> GetPortableFileName(string? value, bool trimTrailingSeparators)
    {
        ReadOnlySpan<char> span = value;

        if (trimTrailingSeparators)
        {
            while (!span.IsEmpty && span[^1] is '/' or '\\')
                span = span[..^1];
        }

        int separator = span.LastIndexOfAny('/', '\\');
        return separator >= 0 ? span[(separator + 1)..] : span;
    }
}
