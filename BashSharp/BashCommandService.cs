using BashSharp.Interfaces;
using BashSharp.Enumerations;
using System.Diagnostics;

namespace BashSharp;

/// <summary>
/// A static bash command service that allows you to execute bash commands and either retrieve the results
///     through an ICommandResult object, retrieve a boolean whether it was successful or not, or retrieve the
///     exit code returned from the bash command executed.
/// </summary>
public static class BashCommandService
{
    /// <summary>
    /// Executes the command and returns boolean indicating if exit code was 0. Enters the faulted state if standard error is read
    /// </summary>
    /// <param name="bashCommand"></param>
    /// <returns></returns>
    public static Task<bool> ExecuteCommand(string bashCommand)
    {
        var source = new TaskCompletionSource<bool>();
        Process process = BuildProcess(bashCommand);
        process.Exited += async (sender, args) =>
        {
            string error = await process.StandardError.ReadToEndAsync();
            if (process.ExitCode != 0)
            {
                source.SetException(new Exception($"Error: {error} - Process exited with code {process.ExitCode}"));
            }
            else
            {
                source.SetResult(process.ExitCode == 0);
            }
            
            process.Dispose();
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            source.SetResult(false);
            source.SetException(ex);
        }

        return source.Task;
    }

    /// <summary>
    /// Executes the command and returns the exit code, or sets the state to faulted if standard error is read
    /// </summary>
    /// <param name="bashCommand"></param>
    /// <returns></returns>
    public static Task<int> ExecuteCommandWithCode(string bashCommand)
    {
        var source = new TaskCompletionSource<int>();
        Process process = BuildProcess(bashCommand);
        process.Exited += async (sender, args) =>
        {
            string error = await process.StandardError.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(error))
            {
                source.SetException(new Exception($"Error: {error} - Process exited with code {process.ExitCode}"));
            }
            else
            {
                source.SetResult(process.ExitCode);
            }
            
            process.Dispose();
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            source.SetException(ex);
        }

        return source.Task;
    }

    /// <summary>
    /// Executes the command passed asynchronously and uses the ICommandResult to parse the responses
    /// </summary>
    /// <param name="bashCommand"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>Task wrapped ICommandResult with parsed result, error and/or exception</returns>
    public static Task<T> ExecuteCommandWithResults<T>(string bashCommand) where T : ICommandResult, new()
    {
        var source = new TaskCompletionSource<T>();
        var commandResult = new T();
        Process process = BuildProcess(bashCommand);
        process.Exited += async (sender, args) =>
        {
            string result = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            
            if (!string.IsNullOrWhiteSpace(result))
            {
                commandResult.ParseResult(result);
            }
            
            if (!string.IsNullOrWhiteSpace(error))
            {
                commandResult.ParseError(error);
            }
            
            commandResult.SetExitCode(process.ExitCode);
            source.SetResult(commandResult);
            process.Dispose();
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            source.SetException(ex);
        }
        
        return source.Task;
    }

    /// <summary>
    /// Builds the process to be executed
    /// </summary>
    /// <param name="bashCommand"></param>
    /// <returns></returns>
    private static Process BuildProcess(string bashCommand)
    {
        string escapedArgs = bashCommand.Replace("\"", "\\\"");
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"{escapedArgs}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };
    }
}