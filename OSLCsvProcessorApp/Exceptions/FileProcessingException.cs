using System;

public class FileProcessingException : Exception
{
	public FileProcessingException(string message, Exception innerException = null)
		: base(message, innerException) { }
}
