using System;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.Contracts;

namespace Soenneker.Utils.Path.Abstract;

/// <summary>
/// A utility library for directory path related operations
/// </summary>
public interface IPathUtil
{
    /// <summary>
    /// Generates a unique file path based on a specified directory and URI. 
    /// If a file with the same name exists, a numeric suffix is appended to the file name to ensure uniqueness.
    /// </summary>
    /// <param name="directory">The directory where the file path should be generated.</param>
    /// <param name="uri">The URI used to extract the file name.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A unique file path in the specified directory.</returns>
    ValueTask<string> GetThreadSafeUniqueFilePath(string directory, string uri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a random, unique file path in a specified directory with a given file extension.
    /// </summary>
    /// <param name="directory">The directory where the file path should be generated.</param>
    /// <param name="fileExtension">The desired file extension (e.g., ".txt").</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A unique file path in the specified directory with the specified file extension.</returns>
    ValueTask<string> GetThreadSafeRandomUniqueFilePath(string directory, string fileExtension, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a random, unique file path in the system's temporary storage directory with a given file extension.
    /// </summary>
    /// <param name="fileExtension">The desired file extension (e.g., ".tmp").</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A unique file path in the system's temporary directory with the specified file extension.</returns>
    ValueTask<string> GetThreadSafeTempUniqueFilePath(string fileExtension, CancellationToken cancellationToken = default);
}