[![](https://img.shields.io/nuget/v/soenneker.utils.path.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.path/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.path/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.path/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.utils.path.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.path/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.path/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.utils.path/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Utils.Path
Reserves collision-free file names, creates unique temporary directories, and generates GUID-based path candidates.

## Installation

```bash
dotnet add package Soenneker.Utils.Path
```

## Quick start

```csharp
using Soenneker.Utils.Path.Registrars;

services.AddPathUtilAsSingleton();
```

Then inject `IPathUtil` wherever you need it.

`GetTempDirectory` and `GetLastPathSegment` are static helpers on `PathUtil`. The unique-path and
temp-directory operations are exposed through `IPathUtil`.

## Reserve a file name from a URI

```csharp
string path = await pathUtil.GetUniqueFilePathFromUri(
    downloadDirectory,
    "https://example.com/files/report.pdf",
    cancellationToken);
```

This method extracts the URI path's file name and atomically creates an empty file to reserve it.
If `report.pdf` already exists, it tries `report(1).pdf`, `report(2).pdf`, and so on. The directory
must already exist. The caller owns the reserved file and should overwrite it or delete it when no
longer needed. Non-collision filesystem failures propagate rather than being retried forever.

## Generate random candidate paths

```csharp
string candidate = await pathUtil.GetRandomUniqueFilePath(outputDirectory, ".json", cancellationToken);
string tempCandidate = await pathUtil.GetRandomTempFilePath("tmp", cancellationToken);
```

These methods return GUID-based paths that did not exist when checked; they do not create or
reserve files. Another process can claim a result before it is opened. Use `FileMode.CreateNew`
when the caller needs atomic ownership and retry on a collision. Extensions with or without a
leading dot are accepted; path components are discarded.

## Create a temporary directory

```csharp
string workspace = await pathUtil.GetUniqueTempDirectory("import", create: true, cancellationToken);

try
{
    // Use workspace.
}
finally
{
    Directory.Delete(workspace, recursive: true);
}
```

The default `create: true` atomically creates a GUID-suffixed directory below the process temp
directory. With `create: false`, the method only returns a currently unused candidate and has the
same check-then-use race as random file paths. Prefix path components are removed. Cleanup remains
the caller's responsibility.

`PathUtil.GetTempDirectory()` returns the process-cached value of `Path.GetTempPath()`.
`PathUtil.GetLastPathSegment(path)` ignores trailing platform directory separators and returns
`null` for empty or separator-only input; it does not access the filesystem.
