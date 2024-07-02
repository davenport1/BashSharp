namespace BashSharp.Interfaces;

/// <summary>
/// ICommandResult Interface
/// Defines an interface all command results adhere to for cli command returns.
/// Implementors must parse the result returned, error result returned (if any), and exception (if any) thrown.
/// </summary>
public interface ICommandResult
{
    void SetExitCode(int exitCode);
    void ParseResult(string result);
    void ParseError(string errorResult);
}