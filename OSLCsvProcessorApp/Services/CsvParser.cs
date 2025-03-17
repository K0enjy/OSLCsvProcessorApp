using System;
using System.Collections.Generic;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using System.IO;
using System.Linq;
using System.Formats.Asn1;

public class CsvParser : CsvParser.ICsvParser
{
	public interface ICsvParser
	{
		List<CsvRecord> ParseCsv(string filePath);
	}

	public List<CsvRecord> ParseCsv(string filePath)
	{
		var records = new List<CsvRecord>();

		using (var reader = new StreamReader(filePath))
		using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			Delimiter = ";",
			HasHeaderRecord = true
		}))
		{
			records = csv.GetRecords<CsvRecord>().ToList();
		}

		return records;
	}
}
