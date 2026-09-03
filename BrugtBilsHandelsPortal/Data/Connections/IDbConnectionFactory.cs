using System.Data.Common;

namespace BrugtBilsHandelsPortal.Data.Connections;

/// <summary>Opretter uåbnede databaseforbindelser uden at eksponere konfigurationsdetaljer.</summary>
public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}