using System;

/// <summary>Something is wrong with the city map file. The message says what and where, in words.</summary>
public class CityMapException : Exception
{
    public CityMapException(string message) : base(message)
    {
    }
}
