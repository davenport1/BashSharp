using BashSharp;

namespace BashSharpTests;

/// <summary>
/// Tests edge cases and error conditions
/// </summary>
public class EdgeCaseTests
{
    [Fact]
    public async Task TestEmptyCommand()
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await BashCommandService.ExecuteCommand(""));
    }

    [Fact]
    public async Task TestVeryLongCommand()
    {
        string longCommand = new string('a', 10000);
        await Assert.ThrowsAsync<Exception>(async () =>
            await BashCommandService.ExecuteCommand(longCommand));
    }

    [Fact]
    public async Task TestMultilineOutput()
    {
        string cmd = "printf \"line1\\nline2\\nline3\"";
        var result = await BashCommandService.ExecuteCommandWithResults<TestCommandResult>(cmd);
        Assert.NotNull(result);
        Assert.Contains("line1", result.ParsedOutput);
        Assert.Contains("line2", result.ParsedOutput);
        Assert.Contains("line3", result.ParsedOutput);
    }

    [Fact]
    public async Task TestCommandWithUnicode()
    {
        string cmd = "printf \"🚀\"";
        var result = await BashCommandService.ExecuteCommandWithResults<TestCommandResult>(cmd);
        Assert.NotNull(result);
        Assert.Contains("🚀", result.ParsedOutput);
    }

    [Fact]
    public async Task TestVeryQuickCommand()
    {
        string cmd = "exit 0";
        var result = await BashCommandService.ExecuteCommand(cmd);
        Assert.True(result);
    }

    [Fact]
    public async Task TestNestedQuotes()
    {
        string cmd = "printf \"nested quotes\"";
        var result = await BashCommandService.ExecuteCommandWithResults<TestCommandResult>(cmd);
        Assert.NotNull(result);
        Assert.Equal("nested quotes", result.ParsedOutput);
    }

    [Fact]
    public async Task TestCommandWithEnvironmentVariables()
    {
        string cmd = "export TEST_VAR=hello; printf \"$TEST_VAR\"";
        var result = await BashCommandService.ExecuteCommandWithResults<TestCommandResult>(cmd);
        Assert.NotNull(result);
        Assert.Equal("hello", result.ParsedOutput);
    }

    [Fact]
    public async Task TestNonExistentCommand()
    {
        await Assert.ThrowsAsync<Exception>(async () =>
            await BashCommandService.ExecuteCommand("nonexistentcommand"));
    }
} 