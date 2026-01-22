using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using BASE.Service.Core.Attribute;
using BASE.Service.Core.Database.Model;
using BASE.Service.Core.Services;
using Dapper;
using MySqlConnector;

namespace BASE.Service.Core.Database
{
    public class MySQLService : IMySQLService
    {
        private const string MasterConnectionKey = "ConnectionStrings:MasterMySql";
        //private readonly IConfiguration _configuration;

        public MySQLService()
        {
            //_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        #region Methods connect

        /// <summary>
        /// Lấy chuỗi kết nối của customer từ master DB
        /// </summary>
        /// <param name="databaseID">ID của customer database</param>
        /// <returns>Connection string tương ứng</returns>
        public async Task<DatabaseConfig> GetDatabaseConfig(Guid databaseID)
        {
            var masterConnectionString = GetMasterConnectionString();
            IDbConnection masterConnection = new MySqlConnection(masterConnectionString);

            try
            {
                masterConnection.Open();

                const string sql = @"SELECT * FROM database_config WHERE DatabaseID = @DatabaseID AND Status = 0 LIMIT 1;";

                var databaseConfig = await masterConnection.QueryFirstOrDefaultAsync<DatabaseConfig>(sql, new { DatabaseID = databaseID });

                if (databaseConfig == null)
                {
                    throw new InvalidOperationException($"Connection string not found for databaseID: {databaseID}");
                }

                return databaseConfig;
            }
            finally
            {
                if (masterConnection.State == ConnectionState.Open)
                {
                    masterConnection.Close();
                }
                masterConnection.Dispose();
            }
        }

        /// <summary>
        /// Lấy cấu hình DB Master
        /// </summary>
        /// <returns></returns>
        private string GetMasterConnectionString()
        {
            var masterDBStringBuilder = new MySqlConnectionStringBuilder()
            {
                Port = 3306,
                Server = "localhost",
                Database = "master_database",
                UserID = "lvhung",
                Password = "12345678@Abc",
                SslMode = MySqlSslMode.Disabled,
                AllowUserVariables = true,
                MaximumPoolSize = 200
            };

            return masterDBStringBuilder.ToString();
        }

        /// <summary>
        /// Lấy kết nối MySQL
        /// </summary>
        /// <param name="databaseID"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<IDbConnection> GetDBConnectionAsync(Guid databaseID)
        {
            var dbConfig = await GetDatabaseConfig(databaseID);

            if (dbConfig == null)
            {
                throw new Exception("Database is null or empty.");
            }

            var cnnDBStringBuilder = new MySqlConnectionStringBuilder()
            {
                Port = dbConfig.Port != null ? (uint)dbConfig.Port : 3306,
                Server = dbConfig.Server,
                Database = dbConfig.Database,
                UserID = dbConfig.UserID,
                Password = dbConfig.Password,
                SslMode = MySqlSslMode.Disabled,
                AllowUserVariables = true,
                MaximumPoolSize = 200
            };

            var cnn = new MySqlConnection(cnnDBStringBuilder.ToString());
            return cnn;
        }

        /// <summary>
        /// Mở kết nối
        /// </summary>
        /// <param name="cnn"></param>
        public void OpenConnection(IDbConnection cnn)
        {
            if (cnn.State != ConnectionState.Open)
            {
                cnn.Open();
            }
        }

        /// <summary>
        /// Đóng kết nối
        /// </summary>
        /// <param name="cnn"></param>
        public void CloseConnection(IDbConnection cnn)
        {
            if (cnn != null)
            {
                if (cnn.State != ConnectionState.Closed)
                {
                    cnn.Close();
                }
                cnn.Dispose();
            }
        }

        #endregion

        #region Methods query

        #endregion
    }
}
