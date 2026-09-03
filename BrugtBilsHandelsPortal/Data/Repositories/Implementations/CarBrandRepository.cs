using BrugtBilsHandelsPortal.Data.Connections;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Implementations;

/// <summary>Leverer opslag i den fælles liste over bilmærker.</summary>
public class CarBrandRepository : ICarBrandRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CarBrandRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<CarBrand>> GetAllAsync()
    {
        const string sql = """
                           SELECT
                               BrandId,
                               BrandName
                           FROM dbo.CarBrand
                           ORDER BY BrandName;
                           """;

        var brands = new List<CarBrand>();

        await using var connection = _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            brands.Add(new CarBrand { BrandId = reader.GetInt32(reader.GetOrdinal("BrandId")), BrandName = reader.GetString(reader.GetOrdinal("BrandName")) });
        }

        return brands;
    }

    public async Task<bool> ExistsAsync(int brandId)
    {
        const string sql = """
                           SELECT CASE WHEN EXISTS
                           (
                               SELECT 1
                               FROM dbo.CarBrand
                               WHERE BrandId = @BrandId
                           )
                           THEN CAST(1 AS bit)
                           ELSE CAST(0 AS bit)
                           END;
                           """;

        await using var connection = _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@BrandId";
        parameter.Value = brandId;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync();

        return Convert.ToBoolean(result);
    }
}