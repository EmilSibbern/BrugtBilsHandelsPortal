using System.Data.Common;
using BrugtBilsHandelsPortal.Data.Connections;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Implementations;

/// <summary>
/// Gemmer parameteriserede auditposter i databasen.
/// </summary>
public class AuditLogRepository : IAuditLogRepository
{
    private readonly IDbConnectionFactory
        _connectionFactory;

    public AuditLogRepository(
        IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task CreateAsync(
        AuditLog auditLog)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        if (string.IsNullOrWhiteSpace(auditLog.Action))
        {
            throw new ArgumentException(
                "Audit-handlingen er påkrævet.",
                nameof(auditLog));
        }

        if (string.IsNullOrWhiteSpace(
            auditLog.EntityType))
        {
            throw new ArgumentException(
                "Audit-entitetstypen er påkrævet.",
                nameof(auditLog));
        }

        const string sql = """
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
                               @UserId,
                               @Action,
                               @EntityType,
                               @EntityId,
                               @Details,
                               @CorrelationId
                           );
                           """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText = sql;

        AddParameter(
            command,
            "@UserId",
            auditLog.UserId);

        AddParameter(
            command,
            "@Action",
            auditLog.Action.Trim());

        AddParameter(
            command,
            "@EntityType",
            auditLog.EntityType.Trim());

        AddParameter(
            command,
            "@EntityId",
            string.IsNullOrWhiteSpace(
                auditLog.EntityId)
                ? null
                : auditLog.EntityId.Trim());

        AddParameter(
            command,
            "@Details",
            string.IsNullOrWhiteSpace(
                auditLog.Details)
                ? null
                : auditLog.Details.Trim());

        AddParameter(
            command,
            "@CorrelationId",
            auditLog.CorrelationId);

        var affectedRows =
            await command.ExecuteNonQueryAsync();

        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                "Auditloggen kunne ikke gemmes.");
        }
    }

    private static void AddParameter(
        DbCommand command,
        string name,
        object? value)
    {
        var parameter =
            command.CreateParameter();

        parameter.ParameterName = name;
        parameter.Value =
            value ?? DBNull.Value;

        command.Parameters.Add(parameter);
    }
}