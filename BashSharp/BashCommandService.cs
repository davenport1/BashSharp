using BashSharp.Interfaces;
using BashSharp.Enumerations;
using System.Diagnostics;
using System.Text;

namespace BashSharp;

/// <summary>
/// A static bash command service that allows you to execute bash commands and either retrieve the results
///     through an ICommandResult object, retrieve a boolean whether it was successful or not, or retrieve the
///     exit code returned from the bash command executed.
/// </summary>
public static class BashCommandService
{
    private const int DefaultTimeoutMs = 30000; // 30 seconds default timeout
    private const int BufferSize = 4096; // 4KB buffer size

    /// <summary>
    /// Executes the command and returns boolean indicating if exit code was 0
    /// </summary>
    /// <param name="bashCommand">The command to execute</param>
    /// <param name="timeoutMs">Timeout in milliseconds, defaults to 30 seconds</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>True if command succeeded, false otherwise</returns>
    public static async Task<bool> ExecuteCommand(string bashCommand, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bashCommand))
            throw new ArgumentException("Command cannot be empty", nameof(bashCommand));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);
        
        using var process = BuildProcess(bashCommand);
        var tcs = new TaskCompletionSource<bool>();
        var errorBuilder = new StringBuilder();
        
        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                errorBuilder.AppendLine(args.Data);
            }
        };

        process.Exited += (sender, args) =>
        {
            if (cts.Token.IsCancellationRequested)
            {
                tcs.TrySetCanceled();
                return;
            }

            if (process.ExitCode != 0)
            {
                tcs.TrySetException(new Exception($"Error: {errorBuilder} - Process exited with code {process.ExitCode}"));
            }
            else
            {
                tcs.TrySetResult(true);
            }
        };

        try 
        {
            process.Start();
            process.BeginErrorReadLine();
            
            using var registration = cts.Token.Register(() => 
            {
                try 
                { 
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                        tcs.TrySetCanceled(cts.Token);
                    }
                }
                catch 
                {
                    tcs.TrySetCanceled(cts.Token);
                }
            });

            return await tcs.Task;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new Exception($"Failed to execute command: {bashCommand}", ex);
        }
    }

    /// <summary>
    /// Executes the command and returns the exit code
    /// </summary>
    /// <param name="bashCommand">The command to execute</param>
    /// <param name="timeoutMs">Timeout in milliseconds, defaults to 30 seconds</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The command exit code</returns>
    public static async Task<int> ExecuteCommandWithCode(string bashCommand, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bashCommand))
            throw new ArgumentException("Command cannot be empty", nameof(bashCommand));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);
        
        using var process = BuildProcess(bashCommand);
        var tcs = new TaskCompletionSource<int>();
        var errorBuilder = new StringBuilder();
        
        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                errorBuilder.AppendLine(args.Data);
            }
        };

        process.Exited += (sender, args) =>
        {
            if (cts.Token.IsCancellationRequested)
            {
                tcs.TrySetCanceled();
                return;
            }

            if (errorBuilder.Length > 0)
            {
                tcs.TrySetException(new Exception($"Error: {errorBuilder} - Process exited with code {process.ExitCode}"));
            }
            else
            {
                tcs.TrySetResult(process.ExitCode);
            }
        };

        try
        {
            process.Start();
            process.BeginErrorReadLine();
            
            using var registration = cts.Token.Register(() => 
            {
                try 
                { 
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                        tcs.TrySetCanceled(cts.Token);
                    }
                }
                catch 
                {
                    tcs.TrySetCanceled(cts.Token);
                }
            });

            return await tcs.Task;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new Exception($"Failed to execute command: {bashCommand}", ex);
        }
    }

    /// <summary>
    /// Executes the command passed asynchronously and uses the ICommandResult to parse the responses
    /// </summary>
    /// <param name="bashCommand">The command to execute</param>
    /// <param name="timeoutMs">Timeout in milliseconds, defaults to 30 seconds</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <typeparam name="T">Type implementing ICommandResult</typeparam>
    /// <returns>Task wrapped ICommandResult with parsed result, error and/or exception</returns>
    public static async Task<T> ExecuteCommandWithResults<T>(string bashCommand, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default) where T : ICommandResult, new()
    {
        if (string.IsNullOrWhiteSpace(bashCommand))
            throw new ArgumentException("Command cannot be empty", nameof(bashCommand));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);
        
        using var process = BuildProcess(bashCommand);
        var tcs = new TaskCompletionSource<T>();
        var commandResult = new T();
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var outputComplete = new TaskCompletionSource<bool>();
        var errorComplete = new TaskCompletionSource<bool>();
        
        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                lock (outputBuilder)
                {
                    outputBuilder.AppendLine(SanitizeOutput(args.Data));
                }
            }
            else
            {
                outputComplete.SetResult(true);
            }
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                lock (errorBuilder)
                {
                    errorBuilder.AppendLine(SanitizeOutput(args.Data));
                }
            }
            else
            {
                errorComplete.SetResult(true);
            }
        };

        process.Exited += async (sender, args) =>
        {
            if (cts.Token.IsCancellationRequested)
            {
                tcs.TrySetCanceled();
                return;
            }

            try
            {
                // Wait for all output to be processed
                await Task.WhenAll(outputComplete.Task, errorComplete.Task);

                string output;
                string error;

                lock (outputBuilder)
                {
                    output = outputBuilder.ToString();
                }

                lock (errorBuilder)
                {
                    error = errorBuilder.ToString();
                }

                if (!string.IsNullOrEmpty(output))
                {
                    commandResult.ParseResult(output);
                }
                
                if (!string.IsNullOrEmpty(error))
                {
                    commandResult.ParseError(error);
                }
                
                commandResult.SetExitCode(process.ExitCode);
                tcs.TrySetResult(commandResult);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            using var registration = cts.Token.Register(() => 
            {
                try 
                { 
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                        tcs.TrySetCanceled(cts.Token);
                    }
                }
                catch 
                {
                    tcs.TrySetCanceled(cts.Token);
                }
            });

            // Wait for process to complete
            await process.WaitForExitAsync(cts.Token);
            return await tcs.Task;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new Exception($"Failed to execute command: {bashCommand}", ex);
        }
    }

    /// <summary>
    /// Builds the process to be executed
    /// </summary>
    /// <param name="bashCommand">The command to execute</param>
    /// <returns>Configured Process instance</returns>
    private static Process BuildProcess(string bashCommand)
    {
        // Configure process with basic settings
        var startInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        // On Windows, use WSL
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            try
            {
                using var wslCheck = Process.Start(new ProcessStartInfo
                {
                    FileName = "wsl",
                    Arguments = "--status",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                wslCheck?.WaitForExit();
                
                if (wslCheck?.ExitCode != 0)
                {
                    throw new PlatformNotSupportedException("WSL is not available");
                }
            }
            catch (Exception ex) when (ex is not PlatformNotSupportedException)
            {
                throw new PlatformNotSupportedException("WSL is not available");
            }
            
            startInfo.FileName = "wsl";
            startInfo.Arguments = bashCommand;  // Let WSL handle the command directly
        }
        else
        {
            startInfo.FileName = "bash";
            startInfo.Arguments = $"-c {bashCommand}";  // Basic bash execution
        }

        return new Process { StartInfo = startInfo, EnableRaisingEvents = true };
    }

    private static string SanitizeOutput(string? output)
    {
        return output ?? string.Empty;  // Just handle null case
    }
}