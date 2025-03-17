using System;
using System.Collections.Generic;

public class CsvRecord
{
	public DateTime Data { get; set; }
	public TimeSpan Ora { get; set; }
	public string Barcode { get; set; }  // Colonna "Numero di Lotto"
	public Dictionary<string, float> Caratteristiche { get; set; }  // Mappa dinamica per le colonne variabili

	public CsvRecord()
	{
		Caratteristiche = new Dictionary<string, float>();
	}
}
