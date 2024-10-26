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
    /// Asynchronously generates a unique, thread-safe file path within the specified directory for a given URI.
    /// </summary>
    /// <param name="directory">The directory in which the unique file path will be created.</param>
    /// <param name="uri">The URI of the file to be saved, used to derive the file name.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the unique file path as a <see cref="string"/>.</returns>
    [Pure]
    ValueTask<string> GetThreadSafeUniqueFilePath(string directory, string uri, CancellationToken cancellationToken);
}