using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace BrugtBilsHandelsPortal.Data.Connections;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                            throw new InvalidOperationException("DefaultConnection Missing in appsettings.json");
    }

    public DbConnection CreateConnection()
    {
        return new SqlConnection(
            _connectionString);
    }
}