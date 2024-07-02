# BashSharp - A C# wrapper to execute bash commands using the Process library 

BashSharp gives an easy to use interface to implement models based on cli standard output that you expect. 

Implement the ICommandResult interface for whatever output you expect and use the ExecuteCommandWithResult to create an instance of that object based on the results from the bash command you'd like to execute.
