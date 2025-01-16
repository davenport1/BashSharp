namespace BashSharp.Interfaces;

/// <summary>
/// ICommandResult Interface
/// Defines an interface all command results adhere to for cli command returns.
/// Implementors must parse the result returned, error result returned (if any), and exception (if any) thrown.
/// </summary>
public interface ICommandResult
{
    /// <summary>Sets the command's exit code</summary>
    /// <param name="exitCode">The process exit code</param>
    void SetExitCode(int exitCode);

    /// <summary>Parses the command's standard output</summary>
    /// <param name="result">The standard output content</param>
    void ParseResult(string result);

    /// <summary>Parses the command's standard error</summary>
    /// <param name="errorResult">The standard error content</param>
    void ParseError(string errorResult);
}