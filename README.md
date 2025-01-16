# BashSharp

A cross-platform C# library for executing bash commands with strongly-typed results. Works on Windows (via WSL), Linux, and macOS.

[![NuGet](https://img.shields.io/nuget/v/BashSharp.svg)](https://www.nuget.org/packages/BashSharp/)
[![Build Status](https://github.com/davenport1/BashSharp/workflows/CI/CD/badge.svg)](https://github.com/yourusername/BashSharp/actions)

## Features

- Execute bash commands and get strongly-typed results
- Cross-platform support (Windows via WSL, Linux, and macOS)
- Async/await support with cancellation and timeout
- Error handling and exit code tracking
- Strongly-typed command results via custom result models
- Efficient streaming output handling for large outputs

## Installation

Install via NuGet:

```bash
dotnet add package BashSharp
```

### Prerequisites

- .NET 8.0 or later
- On Windows: Windows Subsystem for Linux (WSL) with Debian
- On Linux/macOS: bash shell

## Usage

### Basic Command Execution

```csharp
// Simple execution - returns success/failure
bool success = await BashCommandService.ExecuteCommand("echo 'Hello World'");

// Get exit code
int exitCode = await BashCommandService.ExecuteCommandWithCode("ls -la");

// Get strongly-typed results
public class LsResult : ICommandResult
{
    public List<string> Files { get; private set; } = new();
    public int ExitCode { get; private set; }

    public void SetExitCode(int exitCode) => ExitCode = exitCode;
    
    public void ParseResult(string output)
    {
        Files = output.Split('\n')
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    public void ParseError(string error) { }
}

var result = await BashCommandService.ExecuteCommandWithResults<LsResult>("ls");
foreach (var file in result.Files)
{
    Console.WriteLine(file);
}
```

### Timeout and Cancellation

```csharp
// With timeout
await BashCommandService.ExecuteCommand("long-running-command", timeoutMs: 5000);

// With cancellation
using var cts = new CancellationTokenSource();
var task = BashCommandService.ExecuteCommand("long-running-command", cancellationToken: cts.Token);
// Cancel after 1 second
await Task.Delay(1000);
cts.Cancel();
```

### Error Handling

The library throws exceptions when commands fail or return non-zero exit codes. Use try-catch blocks to handle errors:

```csharp
try 
{
    await BashCommandService.ExecuteCommand("invalid-command");
}
catch (Exception ex)
{
    Console.WriteLine($"Command failed: {ex.Message}");
}
```

## Platform Support

- Windows: Requires Windows Subsystem for Linux (WSL) with Debian
- Linux: Requires bash shell
- macOS: Requires bash shell

### Windows Setup

1. Enable WSL:
```powershell
wsl --install -d Debian
```

2. Set WSL 2 as default:
```powershell
wsl --set-default-version 2
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the terms of the LICENSE file included in the repository.

## Acknowledgments

- Thanks to all contributors
- Built with .NET 8.0
