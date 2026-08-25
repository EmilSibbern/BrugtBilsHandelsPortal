using BrugtBilsHandelsPortal.Data.Connections;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Implementations;

/// <summary>Udfører parameteriserede SQL-operationer for brugeridentiteter og profiler.</summary>
public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public UserRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<bool> TestConnectionAsync()
    {
        await using var connection = _dbConnectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT 1";
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) == 1;
    }

    public async Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername)
    {
        const string sql = """
                           SELECT TOP (1)
                               u.Id,
                               u.Username,
                               u.PasswordHash,
                               u.Email,
                               u.UserGroupId,
                               u.Phone,
                               u.Address,
                               u.IsActive,
                               u.CreatedAt,
                               ug.Name AS UserGroupName
                           FROM dbo.Users AS u
                           INNER JOIN dbo.UserGroups AS ug
                               ON ug.Id = u.UserGroupId
                           WHERE u.Email = @Login
                              OR u.Username = @Login;
                           """;

        await using var connection = _dbConnectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = sql;

        var loginParameter = command.CreateParameter();
        loginParameter.ParameterName = "@Login";
        loginParameter.Value = emailOrUsername.Trim();
        command.Parameters.Add(loginParameter);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new User
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            UserGroupId = reader.GetInt32(reader.GetOrdinal("UserGroupId")),
            Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
            Address = reader.IsDBNull(reader.GetOrdinal("Address"))
                ? null
                : reader.GetString(reader.GetOrdinal("Address")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UserGroupName = reader.GetString(reader.GetOrdinal("UserGroupName"))
        };
    }

    public async Task<bool> RegistrationIdentityExistsAsync(string username, string email)
    {
        const string sql = """
                           SELECT CASE WHEN EXISTS (
                           SELECT 1
                           FROM dbo.Users
                           WHERE Username = @Username
                           OR Email = @Email
                           )
                           THEN CAST(1 AS bit)
                           ELSE CAST(0 AS bit)
                           END;
                           """;

        await using var connection = _dbConnectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = sql;

        AddParameter(command, "@Username", username);
        AddParameter(command, "@Email", email);

        var result = await command.ExecuteScalarAsync();

        return Convert.ToBoolean(result);
    }


    public async Task<int> CreateAsync(User user)
    {
        const string sql = """
                           INSERT INTO dbo.Users
                           (
                               Username,
                               PasswordHash,
                               Email,
                               UserGroupId,
                               Phone,
                               Address
                           )
                           OUTPUT INSERTED.Id
                           SELECT
                               @Username,
                               @PasswordHash,
                               @Email,
                               ug.Id,
                               @Phone,
                               @Address
                           FROM dbo.UserGroups AS ug
                           WHERE ug.Name = N'User';
                           """;

        await using var connection = _dbConnectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@Username", user.Username);
        AddParameter(command, "@PasswordHash", user.PasswordHash);
        AddParameter(command, "@Email", user.Email);
        AddParameter(command, "@Phone", user.Phone);
        AddParameter(command, "@Address", user.Address);
        var result = await command.ExecuteScalarAsync();
        if (result is null || result is DBNull)
        {
            throw new InvalidOperationException(
                "User-rollen findes ikke i UserGroups");
        }

        return Convert.ToInt32(result);
    }

    private static void AddParameter(
        System.Data.Common.DbCommand command,
        string parameterName,
        object? value)
    {
        var parameter = command.CreateParameter();

        parameter.ParameterName = parameterName;
        parameter.Value = value ?? DBNull.Value;

        command.Parameters.Add(parameter);
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        const string sql = """
                           SELECT TOP (1)
                               u.Id,
                               u.Username,
                               u.Email,
                               u.UserGroupId,
                               u.Phone,
                               u.Address,
                               u.IsActive,
                               u.CreatedAt,
                               ug.Name AS UserGroupName
                           FROM dbo.Users AS u
                           INNER JOIN dbo.UserGroups AS ug
                               ON ug.Id = u.UserGroupId
                           WHERE u.Id = @UserId
                             AND u.IsActive = 1;
                           """;

        await using var connection =
            _dbConnectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        AddParameter(command, "@UserId", userId);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        var phoneOrdinal = reader.GetOrdinal("Phone");
        var addressOrdinal = reader.GetOrdinal("Address");

        return new User
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            Email = reader.GetString(reader.GetOrdinal("Email")),

            UserGroupId = reader.GetInt32(
                reader.GetOrdinal("UserGroupId")),

            Phone = reader.IsDBNull(phoneOrdinal)
                ? null
                : reader.GetString(phoneOrdinal),

            Address = reader.IsDBNull(addressOrdinal)
                ? null
                : reader.GetString(addressOrdinal),

            IsActive = reader.GetBoolean(
                reader.GetOrdinal("IsActive")),

            CreatedAt = reader.GetDateTime(
                reader.GetOrdinal("CreatedAt")),

            UserGroupName = reader.GetString(
                reader.GetOrdinal("UserGroupName"))
        };
    }
    
    public async Task<bool> IdentityExistsForOtherUserAsync(
        int userId,
        string username,
        string email)
    {
        const string sql = """
                           SELECT CASE WHEN EXISTS
                           (
                               SELECT 1
                               FROM dbo.Users
                               WHERE Id <> @UserId
                                 AND
                                 (
                                     Username = @Username
                                     OR Email = @Email
                                 )
                           )
                           THEN CAST(1 AS bit)
                           ELSE CAST(0 AS bit)
                           END;
                           """;

        await using var connection =
            _dbConnectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        AddParameter(command, "@UserId", userId);
        AddParameter(command, "@Username", username);
        AddParameter(command, "@Email", email);

        var result = await command.ExecuteScalarAsync();

        return Convert.ToBoolean(result);
    }

    public async Task<bool> UpdateProfileAsync(User user)
    {
        const string sql = """
                           UPDATE dbo.Users
                           SET
                               Username = @Username,
                               Email = @Email,
                               Phone = @Phone,
                               Address = @Address
                           WHERE Id = @UserId
                             AND IsActive = 1;
                           """;

        await using var connection =
            _dbConnectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        AddParameter(command, "@UserId", user.Id);
        AddParameter(command, "@Username", user.Username);
        AddParameter(command, "@Email", user.Email);
        AddParameter(command, "@Phone", user.Phone);
        AddParameter(command, "@Address", user.Address);

        var affectedRows = await command.ExecuteNonQueryAsync();

        return affectedRows == 1;
    }
}
