using System;

public class CsvParsingException : Exception
{
	public CsvParsingException(string message, Exception innerException = null)
		: base(message, innerException) { }
}
