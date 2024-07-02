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
    private const string LinuxBash = "/bin/bash";
    private const string WindowsCmd = "CMD.exe";
    private static Os _currentOs;
    private static string BashExecutable { get; set; } = "";

    static BashCommandService()
    {
        SetOsAndExecutable();
    }
    
    private static void SetOsAndExecutable()
    {
        if (OperatingSystem.IsWindows()) _currentOs = Os.Windows;
        else if (OperatingSystem.IsLinux()) _currentOs = Os.Linux;
        else if (OperatingSystem.IsMacOS()) _currentOs = Os.Mac; 
        
        BashExecutable = _currentOs == Os.Windows ? WindowsCmd : LinuxBash;
    }

    public static Task<bool> ExecuteCommand(string bashCommand)
    {
        throw new NotImplementedException();
    }

    public static Task<int> ExecuteCommandWithCodeAsync(string bashCommand)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Executes the command passed asynchronously and uses the ICommandResult to parse the responses
    /// </summary>
    /// <param name="bashCommand"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>Task wrapped ICommandResult with parsed result, error and/or exception</returns>
    public static Task<T> ExecuteCommandWithResultsAsync<T>(string bashCommand) where T : ICommandResult, new()
    {
        var source = new TaskCompletionSource<T>();
        T commandResult = new T();
        Process process = BuildProcess(bashCommand);
        
        process.Exited += async (sender, args) =>
        {
            string result = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            
            if (process.ExitCode == 0)
            {
                commandResult.Parse(result);
            }
            else
            {
                commandResult.ParseError(error);
            }
            source.SetResult(commandResult);
            process.Dispose();
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            commandResult.ParseException(ex);
            source.SetException(ex);
        }
        
        return source.Task;
    }

    private static Process BuildProcess(string bashCommand)
    {
        var escapedArgs = bashCommand.Replace("\"", "\\\"");
        
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