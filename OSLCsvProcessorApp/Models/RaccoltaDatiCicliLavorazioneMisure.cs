public class RaccoltaDatiCicliLavorazioneMisure
{
	public int ID { get; set; }
	public int IDRaccoltaDatiDettaglio { get; set; }
	public int IDProdottiPianiControlloCaratteristiche { get; set; }
	public int NumeroRipetizione { get; set; }
	public DateTime DataOra { get; set; }
	public float ValoreMisura { get; set; }
}
