using Microsoft.Data.SqlClient;

public interface IDatabaseContext
{
	SqlConnection GetConnection();
}

public class DatabaseContext : IDatabaseContext
{
	private readonly string _connectionString;

	public DatabaseContext(Settings settings)
	{
		_connectionString = settings.ConnectionStringDatabase;
	}

	public SqlConnection GetConnection()
	{
		return new SqlConnection(_connectionString);
	}
}
