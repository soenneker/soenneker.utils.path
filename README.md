[![](https://img.shields.io/nuget/v/soenneker.utils.path.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.path/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.path/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.path/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.utils.path.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.path/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.path/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.utils.path/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Utils.Path
A utility library for directory path related operations.

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

## Common operations

- `GetTempDirectory()` - Convenience method to get the temp directory for the current OS. (Path.GetTempPath()).
- `GetLastPathSegment()` - Extracts the last segment of a file system path, excluding any trailing directory separators. Returns the last segment of the specified path as a string, or null if the path is null, empty, or consists only of separators.
- `GetUniqueFilePathFromUri()` - Generates a unique file path based on a specified directory and URI. If a file with the same name exists, a numeric suffix is appended to the file name to ensure uniqueness. Returns a unique file path in the specified directory.
- `GetRandomUniqueFilePath()` - Generates a random, unique file path in a specified directory with a given file extension. Returns a unique file path in the specified directory with the specified file extension.
- `GetRandomTempFilePath()` - Generates a random, unique file path in the system's temporary storage directory with a given file extension. Returns a unique file path in the system's temporary directory with the specified file extension.
- `GetUniqueTempDirectory()` - Gets a unique subdirectory inside the system temp directory in a thread-safe manner. Returns the full path to the unique temp subdirectory.
