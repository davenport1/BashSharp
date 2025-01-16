using System.Diagnostics;
using System.Text;
using BashSharp;
using BashSharp.Interfaces;

namespace BashSharpTests;

public class BashCommandTests
{
    [Fact]
    public void RunBashCommand()
    {
        string cmd = "echo \"hello world\"";
        var source = new TaskCompletionSource<int>();
        var escapedArgs = cmd.Replace("\"", "\\\"");
        var process = new Process
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
        process.Exited += (sender, args) =>
        {
            if (process.ExitCode == 0)
            {
                source.SetResult(0);
            }
            else
            {
                source.SetException(new Exception($"Command `{cmd}` failed with exit code `{process.ExitCode}`"));
            }

            process.Dispose();
        };

        try
        {
            process.Start();
        }
        catch (Exception e)
        {
            source.SetException(e);
        }
    }
    
    [Fact]
    public async Task TestCommandReturnsBool()
    {
        string cmd = "echo Hello World";
        bool result = await BashCommandService.ExecuteCommand(cmd);
    
        Assert.True(result, "The async command execution should be successful");
    }
    
    [Fact]
    public async Task TestCommandReturnsCodeFails()
    {
        string cmd = "exit 1";
        int task = await BashCommandService.ExecuteCommandWithCode(cmd);
        Assert.Equal(1, task);
    }
    
    [Fact]
    public async Task TestCommandReturnsResultsAsync()
    {
        string cmd = "echo \"Hello World\"";
        var result = await BashCommandService.ExecuteCommandWithResults<TestCommandResult>(cmd);

        Assert.NotNull(result);
        Assert.Equal("Hello World", result.ParsedOutput);
    }

    [Fact]
    public async Task TestCommandTimeout()
    {
        string cmd = "sleep 10"; // Command that takes longer than timeout
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await BashCommandService.ExecuteCommand(cmd, timeoutMs: 100)); // 100ms timeout
    }

    [Fact]
    public async Task TestCommandCancellation()
    {
        string cmd = "sleep 5";
        using var cts = new CancellationTokenSource();
        var task = BashCommandService.ExecuteCommand(cmd, cancellationToken: cts.Token);
        
        await Task.Delay(100); // Give the command time to start
        cts.Cancel();
        
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
    }

    [Fact]
    public async Task TestLargeOutput()
    {
        string cmd = "seq 1 10000";
        var result = await BashCommandService.ExecuteCommandWithResults<TestCommandResult>(cmd, timeoutMs: 60000);
        
        Assert.NotNull(result);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("10000", result.ParsedOutput);
    }

    [Fact]
    public async Task TestWslAvailabilityOnWindows()
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            string cmd = "echo 'Testing WSL'";
            var result = await BashCommandService.ExecuteCommand(cmd);
            Assert.True(result);
        }
    }
}

/// <summary>
/// Test implementation of ICommandResult for unit testing
/// </summary>
public class TestCommandResult : ICommandResult
{
    /// <summary>
    /// The parsed output from the command
    /// </summary>
    public string? ParsedOutput { get; private set; }

    /// <summary>
    /// The command's exit code
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Sets the command's exit code
    /// </summary>
    public void SetExitCode(int exitCode)
    {
        ExitCode = exitCode;
    }
    
    /// <summary>
    /// Parses the command's standard output
    /// </summary>
    public void ParseResult(string output)
    {
        ParsedOutput = output.Trim();
    }

    /// <summary>
    /// Parses the command's standard error
    /// </summary>
    public void ParseError(string error)
    {
        // Not implemented for test class
    }
}