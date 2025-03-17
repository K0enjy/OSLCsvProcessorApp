using System;
using System.Collections.Generic;

public class CsvRecord
{
	public DateTime Data { get; set; }
	public TimeSpan Ora { get; set; }
	public string Operatore { get; set; }
	public string Barcode { get; set; }
	public int NumeroMisura { get; set; }
	public Dictionary<string, float> Caratteristiche { get; set; }
}
