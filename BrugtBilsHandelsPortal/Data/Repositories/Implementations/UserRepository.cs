using System.Data;
using BrugtBilsHandelsPortal.Data.Connections;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.DTOs.Common;
using BrugtBilsHandelsPortal.DTOs.Users;

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

    public async Task<PagedResult<AdminUserDto>>
        SearchForAdministrationAsync(
            AdminUserSearchDto search)
    {
        ArgumentNullException.ThrowIfNull(search);

        // --------------------------------------------------
        // 1. Fælles filtre
        // --------------------------------------------------
        // De samme filtre anvendes både til:
        // - at tælle det samlede antal brugere
        // - at hente den aktuelle side med brugere
        //
        // LIKE bruges til delvise søgninger.
        // ESCAPE N'~' betyder, at ~ bruges til at markere,
        // at næste tegn skal behandles som almindelig tekst.
        const string filters = """
                               WHERE
                               (
                                   @QueryPattern IS NULL
                                   OR u.Username LIKE
                                       @QueryPattern ESCAPE N'~'
                                   OR u.Email LIKE
                                       @QueryPattern ESCAPE N'~'
                               )
                               AND
                               (
                                   @IsActive IS NULL
                                   OR u.IsActive = @IsActive
                               )
                               AND
                               (
                                   @Role IS NULL
                                   OR ug.Name = @Role
                               )
                               """;

        // --------------------------------------------------
        // 2. SQL til samlet antal brugere
        // --------------------------------------------------
        // Paging kræver, at UI'et ved, hvor mange resultater
        // der findes på tværs af alle sider.
        var countSql = $"""
                        SELECT COUNT(*)
                        FROM dbo.Users AS u
                        INNER JOIN dbo.UserGroups AS ug
                            ON ug.Id = u.UserGroupId
                        {filters}
                        """;

        // --------------------------------------------------
        // 3. SQL til den aktuelle side
        // --------------------------------------------------
        // Der vælges kun ikke-følsomme oplysninger.
        // PasswordHash, telefon og adresse hentes ikke.
        //
        // Sorteringen indeholder Id som ekstra felt, så
        // rækkefølgen er stabil, hvis to brugere har samme
        // CreatedAt.
        var resultSql = $"""
                         SELECT
                             u.Id,
                             u.Username,
                             u.Email,
                             ug.Name AS Role,
                             u.IsActive,
                             u.CreatedAt,
                             u.UpdatedAt
                         FROM dbo.Users AS u
                         INNER JOIN dbo.UserGroups AS ug
                             ON ug.Id = u.UserGroupId
                         {filters}
                         ORDER BY
                             u.CreatedAt DESC,
                             u.Id DESC
                         OFFSET @Offset ROWS
                         FETCH NEXT @PageSize ROWS ONLY;
                         """;

        // --------------------------------------------------
        // 4. Åbn databaseforbindelsen
        // --------------------------------------------------
        await using var connection =
            _dbConnectionFactory.CreateConnection();

        await connection.OpenAsync();

        // --------------------------------------------------
        // 5. Klargør søgeteksten
        // --------------------------------------------------
        // Søgeteksten trimmes og særlige LIKE-tegn escapes.
        //
        // Hvis brugeren søger efter:
        //   bil
        //
        // bliver parameteren:
        //   %bil%
        //
        // Procenttegnene betyder, at "bil" må stå hvor som
        // helst i brugernavnet eller e-mailen.
        var queryPattern =
            CreateAdminUserQueryPattern(
                search.Query);

        // --------------------------------------------------
        // 6. Tæl alle matchende brugere
        // --------------------------------------------------
        int totalItems;

        await using (var countCommand =
                     connection.CreateCommand())
        {
            countCommand.CommandText =
                countSql;

            AddAdminSearchParameters(
                countCommand,
                search,
                queryPattern);

            var countResult =
                await countCommand.ExecuteScalarAsync();

            totalItems =
                Convert.ToInt32(countResult);
        }

        // --------------------------------------------------
        // 7. Hent den ønskede side
        // --------------------------------------------------
        var users =
            new List<AdminUserDto>();

        await using (var command =
                     connection.CreateCommand())
        {
            command.CommandText =
                resultSql;

            AddAdminSearchParameters(
                command,
                search,
                queryPattern);

            // Eksempel:
            // Side 1 med 20 brugere giver offset 0.
            // Side 2 med 20 brugere giver offset 20.
            AddParameter(
                command,
                "@Offset",
                (search.Page - 1)
                * search.PageSize);

            AddParameter(
                command,
                "@PageSize",
                search.PageSize);

            await using var reader =
                await command.ExecuteReaderAsync();

            var updatedAtOrdinal =
                reader.GetOrdinal("UpdatedAt");

            // --------------------------------------------------
            // 8. Map databaserækker til sikre DTO'er
            // --------------------------------------------------
            while (await reader.ReadAsync())
            {
                users.Add(
                    new AdminUserDto
                    {
                        Id = reader.GetInt32(
                            reader.GetOrdinal("Id")),

                        Username = reader.GetString(
                            reader.GetOrdinal(
                                "Username")),

                        Email = reader.GetString(
                            reader.GetOrdinal(
                                "Email")),

                        Role = reader.GetString(
                            reader.GetOrdinal(
                                "Role")),

                        IsActive = reader.GetBoolean(
                            reader.GetOrdinal(
                                "IsActive")),

                        CreatedAt =
                            reader.GetDateTime(
                                reader.GetOrdinal(
                                    "CreatedAt")),

                        UpdatedAt =
                            reader.IsDBNull(
                                updatedAtOrdinal)
                                ? null
                                : reader.GetDateTime(
                                    updatedAtOrdinal)
                    });
            }
        }

        // --------------------------------------------------
        // 9. Returnér resultat og pagingoplysninger
        // --------------------------------------------------
        return new PagedResult<AdminUserDto>
        {
            Items = users,
            Page = search.Page,
            PageSize = search.PageSize,
            TotalItems = totalItems
        };
    }

    public async Task<AdminUserDeactivationStatus> DeactivateWithAuditAsync(int targetUserId, int administratorUserId,
        Guid correlationId)
    {
        await using var connection =
            _dbConnectionFactory.CreateConnection();

        await connection.OpenAsync();

        // Isolationen beskytter kontrollen af den sidste aktive administrator
        // mod konkurrerende deaktiveringer.
        await using var transaction =
            await connection.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            // Kontrollér administratoren direkte i databasen, så en gammel
            // rolleclaim ikke alene kan autorisere handlingen.
            const string administratorSql = """
                                            SELECT CASE
                                                WHEN EXISTS
                                                (
                                                    SELECT 1
                                                    FROM dbo.Users AS u
                                                    INNER JOIN dbo.UserGroups AS ug
                                                        ON ug.Id = u.UserGroupId
                                                    WHERE u.Id = @AdministratorUserId
                                                      AND u.IsActive = 1
                                                      AND ug.Name = N'Administrator'
                                                )
                                                THEN CAST(1 AS bit)
                                                ELSE CAST(0 AS bit)
                                            END;
                                            """;

            await using (var administratorCommand =
                         connection.CreateCommand())
            {
                administratorCommand.Transaction = transaction;
                administratorCommand.CommandText = administratorSql;

                AddParameter(
                    administratorCommand,
                    "@AdministratorUserId",
                    administratorUserId);

                var result =
                    await administratorCommand.ExecuteScalarAsync();

                if (!Convert.ToBoolean(result))
                {
                    await transaction.RollbackAsync();

                    return AdminUserDeactivationStatus
                        .AdministratorNotAuthorized;
                }
            }

            if (targetUserId == administratorUserId)
            {
                await transaction.RollbackAsync();

                return AdminUserDeactivationStatus
                    .CannotDeactivateSelf;
            }

            // Lås målbrugeren, indtil transaktionen er afsluttet.
            const string targetUserSql = """
                                         SELECT TOP (1)
                                             u.IsActive,
                                             ug.Name AS Role
                                         FROM dbo.Users AS u
                                             WITH (UPDLOCK, HOLDLOCK)
                                         INNER JOIN dbo.UserGroups AS ug
                                             ON ug.Id = u.UserGroupId
                                         WHERE u.Id = @TargetUserId;
                                         """;

            bool targetUserExists;
            bool targetUserIsActive = false;
            string? targetUserRole = null;

            await using (var targetCommand =
                         connection.CreateCommand())
            {
                targetCommand.Transaction = transaction;
                targetCommand.CommandText = targetUserSql;

                AddParameter(
                    targetCommand,
                    "@TargetUserId",
                    targetUserId);

                await using var reader =
                    await targetCommand.ExecuteReaderAsync();

                targetUserExists = await reader.ReadAsync();

                if (targetUserExists)
                {
                    targetUserIsActive = reader.GetBoolean(
                        reader.GetOrdinal("IsActive"));

                    targetUserRole = reader.GetString(
                        reader.GetOrdinal("Role"));
                }
            }

            if (!targetUserExists)
            {
                await transaction.RollbackAsync();

                return AdminUserDeactivationStatus.UserNotFound;
            }

            if (!targetUserIsActive)
            {
                await transaction.RollbackAsync();

                return AdminUserDeactivationStatus.AlreadyInactive;
            }

            // Hvis målet er administrator, skal mindst én anden aktiv
            // administrator blive tilbage.
            if (targetUserRole == "Administrator")
            {
                const string countAdministratorsSql = """
                                                      SELECT COUNT(*)
                                                      FROM dbo.Users AS u
                                                          WITH (UPDLOCK, HOLDLOCK)
                                                      INNER JOIN dbo.UserGroups AS ug
                                                          ON ug.Id = u.UserGroupId
                                                      WHERE u.IsActive = 1
                                                        AND ug.Name = N'Administrator';
                                                      """;

                await using var countCommand =
                    connection.CreateCommand();

                countCommand.Transaction = transaction;
                countCommand.CommandText = countAdministratorsSql;

                var countResult =
                    await countCommand.ExecuteScalarAsync();

                if (Convert.ToInt32(countResult) <= 1)
                {
                    await transaction.RollbackAsync();

                    return AdminUserDeactivationStatus
                        .LastActiveAdministrator;
                }
            }

            const string deactivateSql = """
                                         UPDATE dbo.Users
                                         SET
                                             IsActive = 0,
                                             UpdatedAt = SYSUTCDATETIME()
                                         WHERE Id = @TargetUserId
                                           AND IsActive = 1;
                                         """;

            await using (var deactivateCommand =
                         connection.CreateCommand())
            {
                deactivateCommand.Transaction = transaction;
                deactivateCommand.CommandText = deactivateSql;

                AddParameter(
                    deactivateCommand,
                    "@TargetUserId",
                    targetUserId);

                var affectedRows =
                    await deactivateCommand.ExecuteNonQueryAsync();

                if (affectedRows != 1)
                {
                    await transaction.RollbackAsync();

                    return AdminUserDeactivationStatus
                        .AlreadyInactive;
                }
            }

            // Auditposten gemmes i samme transaktion som statusændringen.
            const string auditSql = """
                                    INSERT INTO dbo.AuditLogs
                                    (
                                        UserId,
                                        Action,
                                        EntityType,
                                        EntityId,
                                        Details,
                                        CorrelationId
                                    )
                                    VALUES
                                    (
                                        @AdministratorUserId,
                                        @Action,
                                        @EntityType,
                                        @EntityId,
                                        @Details,
                                        @CorrelationId
                                    );
                                    """;

            await using (var auditCommand =
                         connection.CreateCommand())
            {
                auditCommand.Transaction = transaction;
                auditCommand.CommandText = auditSql;

                AddParameter(
                    auditCommand,
                    "@AdministratorUserId",
                    administratorUserId);

                AddParameter(
                    auditCommand,
                    "@Action",
                    "UserDeactivated");

                AddParameter(
                    auditCommand,
                    "@EntityType",
                    "User");

                AddParameter(
                    auditCommand,
                    "@EntityId",
                    targetUserId.ToString());

                AddParameter(
                    auditCommand,
                    "@Details",
                    "Brugerkonto deaktiveret af administrator.");

                AddParameter(
                    auditCommand,
                    "@CorrelationId",
                    correlationId);

                var auditRows =
                    await auditCommand.ExecuteNonQueryAsync();

                if (auditRows != 1)
                {
                    throw new InvalidOperationException(
                        "Auditloggen kunne ikke gemmes.");
                }
            }

            await transaction.CommitAsync();

            return AdminUserDeactivationStatus.Succeeded;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<AdminUserActivationStatus> ReactivateWithAuditAsync(int targetUserId, int administratorUserId,
        Guid correlationId)
    {
        await using var connection =
            _dbConnectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            // --------------------------------------------------
            // 1. Kontrollér administratoren i databasen
            // --------------------------------------------------
            const string administratorSql = """
                                            SELECT CASE
                                                WHEN EXISTS
                                                (
                                                    SELECT 1
                                                    FROM dbo.Users AS u
                                                    INNER JOIN dbo.UserGroups AS ug
                                                        ON ug.Id = u.UserGroupId
                                                    WHERE u.Id = @AdministratorUserId
                                                      AND u.IsActive = 1
                                                      AND ug.Name = N'Administrator'
                                                )
                                                THEN CAST(1 AS bit)
                                                ELSE CAST(0 AS bit)
                                            END;
                                            """;

            await using (var administratorCommand =
                         connection.CreateCommand())
            {
                administratorCommand.Transaction =
                    transaction;

                administratorCommand.CommandText =
                    administratorSql;

                AddParameter(
                    administratorCommand,
                    "@AdministratorUserId",
                    administratorUserId);

                var result =
                    await administratorCommand
                        .ExecuteScalarAsync();

                if (!Convert.ToBoolean(result))
                {
                    await transaction.RollbackAsync();

                    return AdminUserActivationStatus
                        .AdministratorNotAuthorized;
                }
            }

            // --------------------------------------------------
            // 2. Hent og lås målbrugeren
            // --------------------------------------------------
            const string targetUserSql = """
                                         SELECT TOP (1)
                                             IsActive
                                         FROM dbo.Users
                                             WITH (UPDLOCK, HOLDLOCK)
                                         WHERE Id = @TargetUserId;
                                         """;

            bool targetUserExists;
            bool targetUserIsActive = false;

            await using (var targetCommand =
                         connection.CreateCommand())
            {
                targetCommand.Transaction =
                    transaction;

                targetCommand.CommandText =
                    targetUserSql;

                AddParameter(
                    targetCommand,
                    "@TargetUserId",
                    targetUserId);

                var result =
                    await targetCommand
                        .ExecuteScalarAsync();

                targetUserExists =
                    result is not null
                    && result is not DBNull;

                if (targetUserExists)
                {
                    targetUserIsActive =
                        Convert.ToBoolean(result);
                }
            }

            if (!targetUserExists)
            {
                await transaction.RollbackAsync();

                return AdminUserActivationStatus
                    .UserNotFound;
            }

            if (targetUserIsActive)
            {
                await transaction.RollbackAsync();

                return AdminUserActivationStatus
                    .AlreadyActive;
            }

            // --------------------------------------------------
            // 3. Genaktivér brugeren
            // --------------------------------------------------
            const string reactivateSql = """
                                         UPDATE dbo.Users
                                         SET
                                             IsActive = 1,
                                             UpdatedAt =
                                                 SYSUTCDATETIME()
                                         WHERE Id = @TargetUserId
                                           AND IsActive = 0;
                                         """;

            await using (var reactivateCommand =
                         connection.CreateCommand())
            {
                reactivateCommand.Transaction =
                    transaction;

                reactivateCommand.CommandText =
                    reactivateSql;

                AddParameter(
                    reactivateCommand,
                    "@TargetUserId",
                    targetUserId);

                var affectedRows =
                    await reactivateCommand
                        .ExecuteNonQueryAsync();

                if (affectedRows != 1)
                {
                    await transaction.RollbackAsync();

                    return AdminUserActivationStatus
                        .AlreadyActive;
                }
            }

            // --------------------------------------------------
            // 4. Audit-log genaktiveringen
            // --------------------------------------------------
            const string auditSql = """
                                    INSERT INTO dbo.AuditLogs
                                    (
                                        UserId,
                                        Action,
                                        EntityType,
                                        EntityId,
                                        Details,
                                        CorrelationId
                                    )
                                    VALUES
                                    (
                                        @AdministratorUserId,
                                        @Action,
                                        @EntityType,
                                        @EntityId,
                                        @Details,
                                        @CorrelationId
                                    );
                                    """;

            await using (var auditCommand =
                         connection.CreateCommand())
            {
                auditCommand.Transaction =
                    transaction;

                auditCommand.CommandText =
                    auditSql;

                AddParameter(
                    auditCommand,
                    "@AdministratorUserId",
                    administratorUserId);

                AddParameter(
                    auditCommand,
                    "@Action",
                    "UserReactivated");

                AddParameter(
                    auditCommand,
                    "@EntityType",
                    "User");

                AddParameter(
                    auditCommand,
                    "@EntityId",
                    targetUserId.ToString());

                AddParameter(
                    auditCommand,
                    "@Details",
                    "Brugerkonto genaktiveret af administrator.");

                AddParameter(
                    auditCommand,
                    "@CorrelationId",
                    correlationId);

                var affectedRows =
                    await auditCommand
                        .ExecuteNonQueryAsync();

                if (affectedRows != 1)
                {
                    throw new InvalidOperationException(
                        "Auditloggen kunne ikke gemmes.");
                }
            }

            // --------------------------------------------------
            // 5. Gem statusændring og auditlog samlet
            // --------------------------------------------------
            await transaction.CommitAsync();

            return AdminUserActivationStatus
                .Succeeded;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Omdanner fri søgetekst til et sikkert LIKE-mønster.
    /// </summary>
    private static string?
        CreateAdminUserQueryPattern(
            string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        // SQL LIKE har særlige tegn:
        //
        // % betyder nul eller flere vilkårlige tegn.
        // _ betyder præcis ét vilkårligt tegn.
        // [ bruges til tegnmønstre i SQL Server.
        //
        // Vi sætter ~ foran disse tegn, så brugerens input
        // behandles som almindelig tekst.
        //
        // ~ skal escapes først, fordi det er det escape-tegn,
        // vi selv har valgt.
        var escapedQuery =
            query.Trim()
                .Replace("~", "~~")
                .Replace("%", "~%")
                .Replace("_", "~_")
                .Replace("[", "~[");

        // De to procenttegn, som vi selv tilføjer, gør
        // søgningen delvis:
        //
        // "anna" matcher eksempelvis både:
        // - anna
        // - johanna
        // - anna@example.dk
        return $"%{escapedQuery}%";
    }

    /// <summary>
    /// Tilføjer de fælles, parameteriserede søgefiltre.
    /// </summary>
    private static void AddAdminSearchParameters(
        System.Data.Common.DbCommand command,
        AdminUserSearchDto search,
        string? queryPattern)
    {
        // Parameterisering betyder, at værdierne sendes
        // adskilt fra SQL-koden. Brugerinput bliver derfor
        // ikke udført som SQL.
        AddParameter(
            command,
            "@QueryPattern",
            queryPattern);

        // null betyder, at både aktive og inaktive brugere
        // skal medtages.
        AddParameter(
            command,
            "@IsActive",
            search.IsActive);

        // En tom rolle behandles som intet rollefilter.
        AddParameter(
            command,
            "@Role",
            string.IsNullOrWhiteSpace(
                search.Role)
                ? null
                : search.Role);
    }
}