using System.Data;
using BRUNINI_CsvParser.Exceptions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace BRUNINI_CsvParser.Services
{
	public interface IDatabaseOperations
	{
		int CreateRaccoltaDatiTestata(string barcode);
		int CreateRaccoltaDatiDettaglio(int idRaccoltaDati, string barcode);
		void InsertRaccoltaDatiCicliLavorazioneMisure(int idDettaglio, Dictionary<string, float> caratteristiche, int numeroMisura, DateTime dataOra);

		List<string> GetColonneValide(string barcode);
	}

	public class DatabaseOperations : IDatabaseOperations
	{
		private readonly IDatabaseContext _databaseContext;
		private readonly ILogger<DatabaseOperations> _logger;

		public DatabaseOperations(IDatabaseContext databaseContext, ILogger<DatabaseOperations> logger)
		{
			_databaseContext = databaseContext;
			_logger = logger;
		}

		public int CreateRaccoltaDatiTestata(string barcode)
		{
			using (var connection = _databaseContext.GetConnection())
			{
				connection.Open();
				using (var transaction = connection.BeginTransaction()) 
				{
					try
					{
						int? idRisorsa = connection.QueryFirstOrDefault<int?>(
							"SELECT IDRisorsa FROM CommesseCicliLavorazione WHERE Barcode = @Barcode",
							new { Barcode = barcode },
							transaction: transaction
						);

						if (!idRisorsa.HasValue)
						{
							throw new Exception($"Nessuna risorsa trovata per il barcode {barcode}");
						}

						int idProcedura = connection.QuerySingle<int>(
							"SELECT ID FROM [Procedure] WHERE CODICE = 'CQ'",
							transaction: transaction
						);

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

							cmd.ExecuteNonQuery(); 

							int idRaccoltaDati = (int)cmd.Parameters["@ID"].Value;

							transaction.Commit(); 
							return idRaccoltaDati;
						}
					}
					catch (Exception ex)
					{
						transaction.Rollback(); 
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
						int numeroRipetizione = 1;

						foreach (var caratteristica in caratteristiche)
						{							
							var result = connection.QueryFirstOrDefault<(int IDCaratteristica, int? IDStrumento)>(
								@"SELECT 
									ppcC.ID AS IDCaratteristica, 
									ppcC.IDStrumento
								FROM 
									ProdottiPianoControllo ppc
								INNER JOIN 
									ProdottiPianoControlloCaratteristiche ppcC 
										ON ppc.ID = ppcC.IDProdottiPianoControllo
								WHERE 
									ppcC.Caratteristica = @Caratteristica 
									AND ppc.DataInizioValidita <= GETDATE() 
									AND ppc.DataFineValidita >= GETDATE()",
								new { Caratteristica = caratteristica.Key },
								transaction: transaction
							);

							if (result.IDCaratteristica == 0)
							{
								_logger.LogWarning($"Caratteristica '{caratteristica.Key}' non trovata nel database. Riga ignorata.");
								continue; 
							}

							int idCaratteristica = result.IDCaratteristica;
							int idStrumento = result.IDStrumento ?? 0;

							string insertQuery = @"INSERT INTO RaccoltaDatiCicliLavorazioneMisure 
														(IDRaccoltaDatiCicliLavorazione, IDProdottiPianiControlloCaratteristiche, Valore, NumeroRipetizione, NumeroCampione, IDStrumento, DataOra) 
													VALUES 
														(@IDRDCCL, @IDProdottiPianiControlloCaratteristiche, @Valore, @NumeroRipetizione, @NumeroCampione, @IDStrumento, @DataOra)";

							connection.Execute(insertQuery, new
							{
								IDRDCCL = idDettaglio,
								IDProdottiPianiControlloCaratteristiche = idCaratteristica,
								Valore = caratteristica.Value,
								NumeroRipetizione = numeroRipetizione++, 
								NumeroCampione = numeroMisura,
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



		public List<string> GetColonneValide(string barcode)
		{
			using (var connection = _databaseContext.GetConnection())
			{
				connection.Open();
				try
				{				
					int? idProdotto = connection.QueryFirstOrDefault<int?>(
						"SELECT IDProdotto FROM CommesseCicliLavorazione WHERE Barcode = @Barcode",
						new { Barcode = barcode }
					);

					if (!idProdotto.HasValue)
					{
						throw new Exception($"Nessun prodotto trovato per il barcode {barcode}");
					}

					return connection.Query<string>(
						@"SELECT 
							Caratteristica 
						  FROM 
							ProdottiPianoControllo ppc 
						  INNER JOIN 
							ProdottiPianoControlloCaratteristiche ppcC 
								ON ppc.ID = ppcC.IDProdottiPianoControllo 
						  WHERE 
							ppc.IDProdotto = @IDProdotto 
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
}


