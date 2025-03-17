using System;
using System.Collections.Generic;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using System.IO;
using System.Linq;

public class CsvParser : CsvParser.ICsvParser
{
	public interface ICsvParser
	{
		List<CsvRecord> ParseCsv(string filePath);
	}

	public List<CsvRecord> ParseCsv(string filePath)
	{
		try
		{
			using var reader = new StreamReader(filePath);
			using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				Delimiter = ";",
				HasHeaderRecord = true
			});

			return csv.GetRecords<CsvRecord>().ToList();
		}
		catch (Exception ex)
		{
			throw new CsvParsingException($"Errore nel parsing del file {filePath}", ex);
		}
	}
}
