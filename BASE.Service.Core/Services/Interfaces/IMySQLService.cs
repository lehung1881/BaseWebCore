using System;
using System.Collections.Generic;
using System.Data;

namespace BASE.Service.Core.Services
{
    public interface IMySQLService
    {
        /// <summary>
        /// Lấy connection string
        /// </summary>
        /// <param name="databaseID"></param>
        /// <returns></returns>
        Task<IDbConnection> GetDBConnectionAsync(Guid databaseID);
    }
}
