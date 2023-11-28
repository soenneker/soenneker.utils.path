using Soenneker.Utils.Path.Abstract;
using System;
using System.Linq;

namespace Soenneker.Utils.Path;

/// <inheritdoc cref="IPathUtil"/>
public class PathUtil: IPathUtil
{
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
}
