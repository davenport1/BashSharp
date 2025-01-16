using BashSharp;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BashSharpBenchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class BenchmarkTests
{
    [Benchmark(Baseline = true)]
    public async Task SimpleCommand()
    {
        await BashCommandService.ExecuteCommand("echo 'test'");
    }

    [Benchmark]
    public async Task CommandWithOutput()
    {
        await BashCommandService.ExecuteCommandWithResults<TestCommandResult>("echo 'test'");
    }

    [Benchmark]
    public async Task LargeOutput()
    {
        await BashCommandService.ExecuteCommandWithResults<TestCommandResult>("seq 1 1000");
    }

    [Benchmark]
    public async Task MultilineOutput()
    {
        await BashCommandService.ExecuteCommandWithResults<TestCommandResult>("echo -e 'line1\\nline2\\nline3\\nline4\\nline5'");
    }

    [Benchmark]
    public async Task CommandWithPipe()
    {
        await BashCommandService.ExecuteCommand("echo 'test' | grep test");
    }

    [Benchmark]
    public async Task EnvironmentVariables()
    {
        await BashCommandService.ExecuteCommand("TEST_VAR='test' bash -c 'echo $TEST_VAR'");
    }
}

public class TestCommandResult : ICommandResult
{
    public string? ParsedOutput { get; private set; }
    public int ExitCode { get; set; }

    public void SetExitCode(int exitCode) => ExitCode = exitCode;
    
    public void ParseResult(string output) => ParsedOutput = output.Trim();

    public void ParseError(string error) { }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<BenchmarkTests>();
    }
} 