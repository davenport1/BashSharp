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
    
    // [Fact]
    // public async Task TestCommandReturnsBoolAsync()
    // {
    //     string cmd = "echo Hello World";
    //     bool result = await BashCommandService.ExecuteCommandAsync(cmd);
    //
    //     Assert.True(result, "The async command execution should be successful");
    // }
    
    // [Fact]
    // public async Task TestCommandReturnsCodeAsync()
    // {
    //     string cmd = "exit 1";
    //     Task<bool> task = BashCommandService.ExecuteCommandAsync(cmd);
    //
    //     await Assert.ThrowsAsync<Exception>(() => task);
    // }
    
    [Fact]
    public async Task TestCommandReturnsResultsAsync()
    {
        string cmd = "echo \"Hello World\"";
        var result = await BashCommandService.ExecuteCommandWithResultsAsync<TestCommandResult>(cmd);

        Assert.NotNull(result);
        Assert.Equal("Hello World", result.ParsedOutput);
    }
}

// TestCommandResult class implementing ICommandResult
public class TestCommandResult : ICommandResult
{
    public string? ParsedOutput { get; private set; }

    public void Parse(string output)
    {
        ParsedOutput = output.Trim();
    }

    public void ParseError(string error)
    {
        // Ignore error for this example.
    }

    public void ParseException(Exception exception)
    {
        // Ignore exception for this example.
    }
}