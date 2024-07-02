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
}

public class TestCommandResult : ICommandResult
{
    public string? ParsedOutput { get; private set; }
    public int ExitCode { get; set; }

    public void SetExitCode(int exitCode)
    {
        ExitCode = exitCode;
    }
    
    public void ParseResult(string output)
    {
        ParsedOutput = output.Trim();
    }

    public void ParseError(string error)
    {
        
    }
}