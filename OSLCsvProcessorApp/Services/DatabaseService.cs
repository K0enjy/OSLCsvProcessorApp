using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;

public class DatabaseService : DatabaseService.IDatabaseService
{
	public interface IDatabaseService
	{
		int CreateRaccoltaDatiTestata(string barcode);
		int CreateRaccoltaDatiDettaglio(int idRaccoltaDati, string barcode);
		void InsertRaccoltaDatiCicliLavorazioneMisure(int idDettaglio, Dictionary<string, float> caratteristiche, int numeroMisura, DateTime dataOra);
	}

	private readonly IDatabaseContext _databaseContext;
	private readonly ILogger<DatabaseService> _logger;

	public DatabaseService(IDatabaseContext databaseContext, ILogger<DatabaseService> logger)
	{
		_databaseContext = databaseContext;
		_logger = logger;
	}

	public int CreateRaccoltaDatiTestata(string barcode)
	{
		using (var connection = _databaseContext.GetConnection())
		{
			connection.Open();
			try
			{
				return connection.QuerySingle<int>(
					"EXEC RaccoltaDatiTestata_Crea @Barcode",
					new { Barcode = barcode });
			}
			catch (Exception ex)
			{
				_logger.LogError($"Errore in CreateRaccoltaDatiTestata: {ex.Message}");
				throw;
			}
		}
	}

	public int CreateRaccoltaDatiDettaglio(int idRaccoltaDati, string barcode)
	{
		using (var connection = _databaseContext.GetConnection())
		{
			connection.Open();
			try
			{
				return connection.QuerySingle<int>(
					"EXEC RaccoltaDatiDettaglio_Crea @IDRD, @Barcode",
					new { IDRD = idRaccoltaDati, Barcode = barcode });
			}
			catch (Exception ex)
			{
				_logger.LogError($"Errore in CreateRaccoltaDatiDettaglio: {ex.Message}");
				throw;
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
					foreach (var caratteristica in caratteristiche)
					{
						connection.Execute(
							"EXEC RaccoltaDatiCicliLavorazioneMisure_Crea @IDDettaglio, @Caratteristica, @NumeroMisura, @DataOra",
							new
							{
								IDDettaglio = idDettaglio,
								Caratteristica = caratteristica.Key,
								NumeroMisura = numeroMisura,
								DataOra = dataOra
							},
							transaction: transaction);
					}
					transaction.Commit();
				}
				catch (Exception ex)
				{
					transaction.Rollback();
					_logger.LogError($"Errore in InsertRaccoltaDatiCicliLavorazioneMisure: {ex.Message}");
					throw;
				}
			}
		}
	}
}
