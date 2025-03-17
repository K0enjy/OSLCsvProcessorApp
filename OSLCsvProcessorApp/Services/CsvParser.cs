using System;
using System.Collections.Generic;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
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
				Delimiter = ",",  // Usa ";" se il file ha separatori diversi
				HasHeaderRecord = true,
				MissingFieldFound = null,
				BadDataFound = null
			});

			// **Impostiamo il formato data personalizzato**
			csv.Context.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = new[] { "dd/MM/yyyy", "d/M/yyyy" };

			// **Leggiamo l'header**
			csv.Read();
			csv.ReadHeader();
			var headers = csv.HeaderRecord;

			// **SALTIAMO LE PROSSIME 3 RIGHE (2-4)**
			for (int i = 0; i < 3; i++)
			{
				csv.Read();
			}

			// Definiamo le colonne costanti
			string colData = "data";
			string colOra = "Ora";
			string colBarcode = "Numero di Lotto";

			List<CsvRecord> records = new();

			while (csv.Read())
			{
				try
				{
					var record = new CsvRecord
					{
						Data = csv.GetField<DateTime>(colData),
						Ora = TimeSpan.Parse(csv.GetField(colOra)),
						Barcode = csv.GetField(colBarcode)
					};

					// **Leggiamo dinamicamente tutte le altre colonne**
					for (int i = 0; i < headers.Length; i++)
					{
						string columnName = headers[i];
						if (columnName != colData && columnName != colOra && columnName != colBarcode)
						{
							if (float.TryParse(csv.GetField(columnName), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
							{
								record.Caratteristiche[columnName] = value;
							}
						}
					}

					records.Add(record);
				}
				catch (Exception ex)
				{
					throw new CsvParsingException($"Errore nel parsing della riga {csv.Parser.RawRow}: {ex.Message}", ex);
				}
			}

			return records;
		}
		catch (Exception ex)
		{
			throw new CsvParsingException($"Errore nel parsing del file {filePath}", ex);
		}
	}
}
