using System;

/// <summary>Something is wrong in Experiments.xlsx. The message says what and where.</summary>
public class ExperimentFileException : Exception
{
    public ExperimentFileException(string message) : base(message)
    {
    }
}
