using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

public class DatabaseRepository : DatabaseRepository.IDatabaseRepository
{
	public interface IDatabaseRepository
	{
		int CreateRaccoltaDatiTestata(string barcode);
		int CreateRaccoltaDatiDettaglio(int idRaccoltaDati, string barcode);
		void InsertRaccoltaDatiCicliLavorazioneMisure(int idDettaglio, Dictionary<string, float> caratteristiche, int numeroMisura, DateTime dataOra);

		// **💡 Nuovo metodo per ottenere le colonne valide dal database**
		List<string> GetColonneValide(string barcode);
	}


	private readonly IDatabaseContext _databaseContext;
	private readonly ILogger<DatabaseRepository> _logger;

	public DatabaseRepository(IDatabaseContext databaseContext, ILogger<DatabaseRepository> logger)
	{
		_databaseContext = databaseContext;
		_logger = logger;
	}

public int CreateRaccoltaDatiTestata(string barcode)
{
    using (var connection = _databaseContext.GetConnection())
    {
        connection.Open();
        using (var transaction = connection.BeginTransaction()) // **Iniziamo la transazione**
        {
            try
            {
                // **Recuperiamo l'IDRisorsa dal database basandoci sul barcode**
                int? idRisorsa = connection.QueryFirstOrDefault<int?>(
                    "SELECT IDRisorsa FROM CommesseCicliLavorazione WHERE Barcode = @Barcode",
                    new { Barcode = barcode },
                    transaction: transaction
                );

                if (!idRisorsa.HasValue)
                {
                    throw new Exception($"Nessuna risorsa trovata per il barcode {barcode}");
                }

                // **Recuperiamo l'ID della procedura CQ**
                int idProcedura = connection.QuerySingle<int>(
                    "SELECT ID FROM [Procedure] WHERE CODICE = 'CQ'",
                    transaction: transaction
                );

                // **Creiamo la testata della raccolta dati con SqlCommand e associamo la transazione**
                using (SqlCommand cmd = new SqlCommand("RaccoltaDatiTestata_Crea", connection, transaction)) // 🔹 Associazione della transazione
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Computer", DBNull.Value);
                    cmd.Parameters.AddWithValue("@IDRisorsa", idRisorsa);
                    cmd.Parameters.AddWithValue("@IDOperatoreDiControllo", DBNull.Value);
                    cmd.Parameters.AddWithValue("@IDProcedura", idProcedura);
                    cmd.Parameters.AddWithValue("@IDTerminale", DBNull.Value);
                    cmd.Parameters.AddWithValue("@DataOraInizio", DateTime.Now);
                    cmd.Parameters.AddWithValue("@DataOraFine", DateTime.Now.AddSeconds(1));
                    cmd.Parameters.AddWithValue("@Pausa", 0);
                    cmd.Parameters.AddWithValue("@Esportato", 0);
                    cmd.Parameters.AddWithValue("@Chiusura", -1);
                    cmd.Parameters.AddWithValue("@Gruppo", DBNull.Value);
                    cmd.Parameters.AddWithValue("@RiferimentoTelefonata", DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProvieneDaJob", 0);
                    cmd.Parameters.AddWithValue("@InJobRemota", 0);
                    cmd.Parameters.AddWithValue("@AccontoSaldo", 'S');
                    cmd.Parameters.AddWithValue("@InJobRemotaEsterna", 0);
                    cmd.Parameters.AddWithValue("@IDRisorsaRilevata", DBNull.Value);
                    cmd.Parameters.AddWithValue("@Descrizione", DBNull.Value);
                    cmd.Parameters.AddWithValue("@InJobAttivitaInCorso", 0);
                    cmd.Parameters.AddWithValue("@IDRaccoltaDatiControllo", DBNull.Value);
                    cmd.Parameters.AddWithValue("@Validato", 0);
                    cmd.Parameters.AddWithValue("@IDListaPrelievoTestata", DBNull.Value);
                    cmd.Parameters.AddWithValue("@InModifica", 0);
                    cmd.Parameters.AddWithValue("@BarcodeSchema", DBNull.Value);
                    cmd.Parameters.AddWithValue("@DichiarazioneBarcodeInApertura", 0);
                    cmd.Parameters.AddWithValue("@IDMovimentoMagazzinoDettaglio", DBNull.Value);

                    SqlParameter outputParam = new SqlParameter("@ID", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    cmd.ExecuteNonQuery(); // ✅ Ora è associato a una transazione

                    int idRaccoltaDati = (int)cmd.Parameters["@ID"].Value;

                    transaction.Commit(); // **Confermiamo la transazione**
                    return idRaccoltaDati;
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // **Annulliamo la transazione in caso di errore**
                _logger.LogError($"Errore in CreateRaccoltaDatiTestata: {ex.Message}");
                throw;
            }
        }
    }
}


	public int CreateRaccoltaDatiDettaglio(int idRaccoltaDati, string barcode)
	{
		using (var connection = _databaseContext.GetConnection())
		{
			connection.Open();
			using (var transaction = connection.BeginTransaction())
			{
				try
				{
					var parameters = new DynamicParameters();
					parameters.Add("@IDRD", idRaccoltaDati);
					parameters.Add("@Barcode", barcode);
					parameters.Add("@ID", dbType: DbType.Int32, direction: ParameterDirection.Output);

					connection.Execute("RaccoltaDatiDettaglio_Crea", parameters, transaction, commandType: CommandType.StoredProcedure);

					int idDettaglio = parameters.Get<int>("@ID");

					transaction.Commit();
					return idDettaglio;
				}
				catch (Exception ex)
				{
					transaction.Rollback();
					_logger.LogError($"Errore in CreateRaccoltaDatiDettaglio: {ex.Message}");
					throw;
				}
			}
		}
	}


public void InsertRaccoltaDatiCicliLavorazioneMisure(int idDettaglio, Dictionary<string, float> caratteristiche, int numeroMisura, DateTime dataOra)
{
    using (var connection = _databaseContext.GetConnection())
    {
        connection.Open();
        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                int numeroRipetizione = 1; // **Numero incrementale per ogni riga**

                foreach (var caratteristica in caratteristiche)
                {
                    // **Recuperiamo l'ID della caratteristica e l'IDStrumento**
                    var result = connection.QueryFirstOrDefault<(int IDCaratteristica, int? IDStrumento)>(
                        @"SELECT ppcC.ID AS IDCaratteristica, ppcC.IDStrumento
                          FROM ProdottiPianoControllo ppc
                          INNER JOIN ProdottiPianoControlloCaratteristiche ppcC 
                          ON ppc.ID = ppcC.IDProdottiPianoControllo
                          WHERE ppcC.Caratteristica = @Caratteristica 
                          AND ppc.DataInizioValidita <= GETDATE() 
                          AND ppc.DataFineValidita >= GETDATE()",
                        new { Caratteristica = caratteristica.Key },
                        transaction: transaction
                    );

                    if (result.IDCaratteristica == 0)
                    {
                        _logger.LogWarning($"Caratteristica '{caratteristica.Key}' non trovata nel database. Riga ignorata.");
                        continue; // **Saltiamo la riga se la caratteristica non esiste**
                    }

                    int idCaratteristica = result.IDCaratteristica;
                    int idStrumento = result.IDStrumento ?? 0; // **Se NULL, mettiamo 0**

                    // **Eseguiamo l'INSERT diretto**
                    string insertQuery = @"
                        INSERT INTO RaccoltaDatiCicliLavorazioneMisure 
                        (IDRaccoltaDatiCicliLavorazione, IDProdottiPianiControlloCaratteristiche, Valore, 
                         NumeroRipetizione, NumeroCampione, IDStrumento, DataOra) 
                        VALUES 
                        (@IDRDCCL, @IDProdottiPianiControlloCaratteristiche, @Valore, 
                         @NumeroRipetizione, @NumeroCampione, @IDStrumento, @DataOra)";

                    connection.Execute(insertQuery, new
                    {
                        IDRDCCL = idDettaglio,
                        IDProdottiPianiControlloCaratteristiche = idCaratteristica,
                        Valore = caratteristica.Value,
                        NumeroRipetizione = numeroRipetizione++, // **Incrementiamo per ogni riga**
                        NumeroCampione = numeroMisura, // **Da CSV, può diventare incrementale**
                        IDStrumento = idStrumento,
                        DataOra = dataOra
                    }, transaction: transaction);

                    _logger.LogInformation($"Inserito record per caratteristica '{caratteristica.Key}' (ID: {idCaratteristica}), valore: {caratteristica.Value}");
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Errore in InsertRaccoltaDatiCicliLavorazioneMisure: {ex.Message}");
                throw new CustomException("Errore nell'inserimento delle misurazioni", ex);
            }
        }
    }
}



	// **💡 Implementiamo GetColonneValide per verificare le colonne disponibili nel database**
	public List<string> GetColonneValide(string barcode)
	{
		using (var connection = _databaseContext.GetConnection())
		{
			connection.Open();
			try
			{
				// **Recuperiamo l'IDProdotto dal barcode attuale**
				int? idProdotto = connection.QueryFirstOrDefault<int?>(
					"SELECT IDProdotto FROM CommesseCicliLavorazione WHERE Barcode = @Barcode",
					new { Barcode = barcode }
				);

				if (!idProdotto.HasValue)
				{
					throw new Exception($"Nessun prodotto trovato per il barcode {barcode}");
				}

				// **Recuperiamo le caratteristiche valide per il prodotto e la data corrente**
				return connection.Query<string>(
					@"SELECT Caratteristica 
                  FROM ProdottiPianoControllo ppc 
                  INNER JOIN ProdottiPianoControlloCaratteristiche ppcC 
                  ON ppc.ID = ppcC.IDProdottiPianoControllo 
                  WHERE ppc.IDProdotto = @IDProdotto 
                  AND ppc.DataInizioValidita <= GETDATE() 
                  AND ppc.DataFineValidita >= GETDATE()",
					new { IDProdotto = idProdotto.Value }
				).ToList();
			}
			catch (Exception ex)
			{
				_logger.LogError($"Errore in GetColonneValide: {ex.Message}");
				throw;
			}
		}
	}
}
