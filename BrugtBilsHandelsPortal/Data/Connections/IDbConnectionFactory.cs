using System.Data.Common;

namespace BrugtBilsHandelsPortal.Data.Connections;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}