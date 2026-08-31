using BrugtBilsHandelsPortal.Models.Entities;

namespace BrugtBilsHandelsPortal.Data.Repositories.Interfaces;

/// <summary>
/// Gemmer sporbare sikkerheds- og administrationshandlinger.
/// </summary>
public interface IAuditLogRepository
{
    Task CreateAsync(AuditLog auditLog);
}