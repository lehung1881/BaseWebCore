using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using BASE.Service.Core.Database.Model;
using BASE.Service.Core.Model;
using BASE.Service.Core.Services;
using Dapper;
using MySqlConnector;

namespace BASE.Service.Core.Database
{
    public class MySQLService : IMySQLService
    {
        private const string MasterConnectionKey = "ConnectionStrings:MasterMySql";

        // Cache để lưu trữ DatabaseConfig theo DatabaseID
        private static readonly ConcurrentDictionary<Guid, DatabaseConfig> _databaseConfigCache = new ConcurrentDictionary<Guid, DatabaseConfig>();

        //private readonly IConfiguration _configuration;

        public MySQLService()
        {
            //_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        #region Methods connect

        /// <summary>
        /// Lấy chuỗi kết nối của customer từ master DB với cache
        /// </summary>
        /// <param name="databaseID">ID của customer database</param>
        /// <returns>Connection string tương ứng</returns>
        public async Task<DatabaseConfig> GetDatabaseConfig(Guid databaseID)
        {
            // Kiểm tra cache trước
            if (_databaseConfigCache.TryGetValue(databaseID, out var cachedConfig))
            {
                return cachedConfig;
            }

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

                // Lưu vào cache
                _databaseConfigCache.TryAdd(databaseID, databaseConfig);

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
        /// Xóa cache của một database config cụ thể
        /// </summary>
        /// <param name="databaseID">ID của database cần xóa cache</param>
        public void ClearDatabaseConfigCache(Guid databaseID)
        {
            _databaseConfigCache.TryRemove(databaseID, out _);
        }

        /// <summary>
        /// Xóa toàn bộ cache database config
        /// </summary>
        public void ClearAllDatabaseConfigCache()
        {
            _databaseConfigCache.Clear();
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

        #region Methods query text
        /// <summary>
        /// Thực hiện truy vấn dữ liệu từ database sử dụng command text
        /// </summary>
        public async Task<List<T>> QueryUsingCommandText<T>(Guid databaseID, string commandText, Dictionary<string, object> param)
        {
            IDbConnection cnn = null;
            try
            {
                cnn = await GetDBConnectionAsync(databaseID);
                var dynamicParams = ConvertToDynamicParameters(param);
                var result = await cnn.QueryAsync<T>(sql: commandText, param: dynamicParams, commandType: CommandType.Text);
                return result.AsList();
            }
            catch
            {
                throw;
            }
            finally
            {
                if (cnn != null && cnn.State == ConnectionState.Open)
                {
                    cnn.Close();
                    cnn.Dispose();
                }
            }
        }

        /// <summary>
        /// Thực hiện truy vấn dữ liệu từ database sử dụng command text với connection có sẵn
        /// </summary>
        public async Task<List<T>> QueryUsingCommandText<T>(IDbConnection cnn, string commandText, Dictionary<string, object> param)
        {
            try
            {
                var dynamicParams = ConvertToDynamicParameters(param);
                var result = await cnn.QueryAsync<T>(sql: commandText, param: dynamicParams, commandType: CommandType.Text);
                return result.AsList();
            }
            catch
            {
                throw;
            }
            finally
            {
                if (cnn != null && cnn.State == ConnectionState.Open)
                {
                    cnn.Close();
                    cnn.Dispose();
                }
            }
        }

        /// <summary>
        /// Thực hiện truy vấn nhiều tập kết quả từ database sử dụng command text
        /// </summary>
        public async Task<List<List<object>>> QueryMultipleUsingCommandText(Guid databaseID, string commandText, List<Type> types, Dictionary<string, object> param)
        {
            IDbConnection cnn = null;
            try
            {
                cnn = await GetDBConnectionAsync(databaseID);
                var dynamicParams = ConvertToDynamicParameters(param);
                var multi = await cnn.QueryMultipleAsync(sql: commandText, param: dynamicParams, commandType: CommandType.Text);
                return await ReadMultipleResults(multi, types);
            }
            catch
            {
                throw;
            }
            finally
            {
                if (cnn != null && cnn.State == ConnectionState.Open)
                {
                    cnn.Close();
                    cnn.Dispose();
                }
            }
        }

        /// <summary>
        /// Thực hiện truy vấn nhiều tập kết quả từ database sử dụng command text với connection có sẵn
        /// </summary>
        public async Task<List<List<object>>> QueryMultipleUsingCommandText(IDbConnection cnn, string commandText, List<Type> types, Dictionary<string, object> param)
        {
            try
            {
                var dynamicParams = ConvertToDynamicParameters(param);
                var multi = await cnn.QueryMultipleAsync(sql: commandText, param: dynamicParams, commandType: CommandType.Text);
                return await ReadMultipleResults(multi, types);
            }
            catch
            {
                throw;
            }
            finally
            {
                if (cnn != null && cnn.State == ConnectionState.Open)
                {
                    cnn.Close();
                    cnn.Dispose();
                }
            }
        }

        /// <summary>
        /// Thực thi lệnh SQL (INSERT, UPDATE, DELETE) sử dụng command text
        /// </summary>
        public async Task<bool> ExecuteUsingCommandText(Guid databaseID, string commandText, Dictionary<string, object> param)
        {
            IDbConnection cnn = null;
            try
            {
                cnn = await GetDBConnectionAsync(databaseID);
                var dynamicParams = ConvertToDynamicParameters(param);
                var rowsAffected = await cnn.ExecuteAsync(sql: commandText, param: dynamicParams, commandType: CommandType.Text);
                return rowsAffected > 0;
            }
            catch
            {
                throw;
            }
            finally
            {
                if (cnn != null && cnn.State == ConnectionState.Open)
                {
                    cnn.Close();
                    cnn.Dispose();
                }
            }
        }

        /// <summary>
        /// Thực thi lệnh SQL (INSERT, UPDATE, DELETE) sử dụng command text với connection có sẵn
        /// </summary>
        public async Task<bool> ExecuteUsingCommandText(IDbConnection cnn, string commandText, Dictionary<string, object> param)
        {
            try
            {
                var dynamicParams = ConvertToDynamicParameters(param);
                var rowsAffected = await cnn.ExecuteAsync(sql: commandText, param: dynamicParams, commandType: CommandType.Text);
                return rowsAffected > 0;
            }
            catch
            {
                throw;
            }
            finally
            {
                if (cnn != null && cnn.State == ConnectionState.Open)
                {
                    cnn.Close();
                    cnn.Dispose();
                }
            }
        }

        #endregion

        #region Methods query store procedure
        /// <summary>
        /// Thực hiện truy vấn dữ liệu từ database sử dụng stored procedure
        /// </summary>
        public async Task<List<T>> QueryUsingStoreProcedure<T>(Guid databaseID, string procedureName, Dictionary<string, object> param)
        {
            IDbConnection cnn = null;
            try
            {
                cnn = await GetDBConnectionAsync(databaseID);
                var dynamicParams = ConvertToDynamicParameters(param);
                var result = await cnn.QueryAsync<T>(sql: procedureName, param: dynamicParams, commandType: CommandType.StoredProcedure);
                return result.AsList();
            }
            catch
            {
                throw;
            }
            finally
            {
                if (cnn != null && cnn.State == ConnectionState.Open)
                {
                    cnn.Close();
                    cnn.Dispose();
                }
            }
        }

        /// <summary>
        /// Thực hiện truy vấn dữ liệu từ database sử dụng stored procedure với connection có sẵn
        /// </summary>
        public async Task<List<T>> QueryUsingStoreProcedure<T>(IDbConnection cnn, string procedureName, Dictionary<string, object> param)
        {
            try
            {
                var dynamicParams = ConvertToDynamicParameters(param);
                var result = await cnn.QueryAsync<T>(sql: procedureName, param: dynamicParams, commandType: CommandType.StoredProcedure);
                return result.AsList();
            }
            catch
            {
                throw;
            }
            finally
            {
                if (cnn != null && cnn.State == ConnectionState.Open)
                {
                    cnn.Close();
                    cnn.Dispose();
                }
            }
        }

        /// <summary>
        /// Thực thi stored procedure (INSERT, UPDATE, DELETE)
        /// </summary>
        public async Task<bool> ExecuteUsingStoreProcedure(Guid databaseID, string procedureName, Dictionary<string, object> param)
        {
            IDbConnection cnn = null;
            try
            {
                cnn = await GetDBConnectionAsync(databaseID);
                var dynamicParams = ConvertToDynamicParameters(param);
                var rowsAffected = await cnn.ExecuteAsync(sql: procedureName, param: dynamicParams, commandType: CommandType.StoredProcedure);
                return rowsAffected > 0;
            }
            catch
            {
                throw;
            }
            finally
            {
                if (cnn != null && cnn.State == ConnectionState.Open)
                {
                    cnn.Close();
                    cnn.Dispose();
                }
            }
        }

        /// <summary>
        /// Thực thi stored procedure (INSERT, UPDATE, DELETE) với connection có sẵn
        /// </summary>
        public async Task<bool> ExecuteUsingStoreProcedure(IDbConnection cnn, string procedureName, Dictionary<string, object> param)
        {
            try
            {
                var dynamicParams = ConvertToDynamicParameters(param);
                var rowsAffected = await cnn.ExecuteAsync(sql: procedureName, param: dynamicParams, commandType: CommandType.StoredProcedure);
                return rowsAffected > 0;
            }
            catch
            {
                throw;
            }
            finally
            {
                if (cnn != null && cnn.State == ConnectionState.Open)
                {
                    cnn.Close();
                    cnn.Dispose();
                }
            }
        }

        /// <summary>
        /// Thực hiện truy vấn nhiều tập kết quả từ database sử dụng stored procedure
        /// </summary>
        public async Task<List<List<object>>> QueryMultipleUsingStoreProcedure(Guid databaseID, string procedureName, List<Type> types, Dictionary<string, object> param)
        {
            IDbConnection cnn = null;
            try
            {
                cnn = await GetDBConnectionAsync(databaseID);
                var dynamicParams = ConvertToDynamicParameters(param);
                var multi = await cnn.QueryMultipleAsync(sql: procedureName, param: dynamicParams, commandType: CommandType.StoredProcedure);
                return await ReadMultipleResults(multi, types);
            }
            catch
            {
                throw;
            }
            finally
            {
                if (cnn != null && cnn.State == ConnectionState.Open)
                {
                    cnn.Close();
                    cnn.Dispose();
                }
            }
        }

        /// <summary>
        /// Thực hiện truy vấn nhiều tập kết quả từ database sử dụng stored procedure với connection có sẵn
        /// </summary>
        public async Task<List<List<object>>> QueryMultipleUsingStoreProcedure(IDbConnection cnn, string procedureName, List<Type> types, Dictionary<string, object> param)
        {
            try
            {
                var dynamicParams = ConvertToDynamicParameters(param);
                var multi = await cnn.QueryMultipleAsync(sql: procedureName, param: dynamicParams, commandType: CommandType.StoredProcedure);
                return await ReadMultipleResults(multi, types);
            }
            catch
            {
                throw;
            }
            finally
            {
                if (cnn != null && cnn.State == ConnectionState.Open)
                {
                    cnn.Close();
                    cnn.Dispose();
                }
            }
        }
        #endregion

        #region Methods get
        /// <summary>
        /// Lấy theo ID
        /// </summary>
        public async Task<object?> GetDataByID(Guid databaseID, Type modelType, string id, string columns = "*")
        {
            var param = new Dictionary<string, object>();
            var sql = GenerateSelectByID(param, modelType, id, columns);
            var result = await QueryUsingCommandText<object>(databaseID, sql, param);
            return result.Count > 0 ? result.First() : null;
        }

        /// <summary>
        /// Lấy theo ID
        /// </summary>
        public async Task<BaseModel> GetDataByID(Guid databaseID, Type modelType, string id)
        {
            var param = new Dictionary<string, object>();
            var sql = GenerateSelectByID(param, modelType, id);
            var result = await QueryUsingCommandText<BaseModel>(databaseID, sql, param);
            return result.Count > 0 ? result.First() : null;
        }

        /// <summary>
        /// Lấy bản ghi theo ID
        /// </summary>
        public async Task<T> GetDataByID<T>(Guid databaseID, string id) where T : BaseModel
        {
            var param = new Dictionary<string, object>();
            var sql = GenerateSelectByID(param, typeof(T), id);
            var result = await QueryUsingCommandText<T>(databaseID, sql, param);
            return result.Count > 0 ? result.First() : null;
        }
        #endregion

        #region Helper methods
        /// <summary>
        /// Tạo câu truy vấn SELECT theo ID với hỗ trợ Table attribute
        /// </summary>
        /// <param name="param">Dictionary chứa các tham số</param>
        /// <param name="modelType">Kiểu dữ liệu của model</param>
        /// <param name="id">Giá trị ID cần tìm</param>
        /// <returns>Câu truy vấn SQL</returns>
        public string GenerateSelectByID(Dictionary<string, object> param, Type modelType, string id, string columns = "*")
        {
            var sql = $"SELECT {columns} FROM {modelType.GetViewOrTableName()} WHERE {modelType.GetPrimaryKeyFieldName()} = @IDValue;";
            param = new Dictionary<string, object>()
            {
                { "IDValue",  GetSqlValue(id)}
            };
            return sql;
        }

        /// <summary>
        /// Sinh giá trị where dựa vào kiểu dữ liệu parameter
        /// </summary>
        /// <param name="value">Parameter</param>
        protected object GetSqlValue(string value)
        {
            Guid guidValue;
            if (Guid.TryParse(value, out guidValue))
            {
                return guidValue;
            }
            else
            {
                long longValue = 0;
                if (long.TryParse(value, out longValue))
                {
                    return longValue;
                }
                else
                {
                    return value;
                }
            }
        }

        /// <summary>
        /// Chuyển đổi Dictionary sang DynamicParameters
        /// </summary>
        public DynamicParameters ConvertToDynamicParameters(Dictionary<string, object> param)
        {
            var dynamicParams = new DynamicParameters();
            if (param != null)
            {
                foreach (var kvp in param)
                {
                    dynamicParams.Add(kvp.Key, kvp.Value);
                }
            }
            return dynamicParams;
        }

        /// <summary>
        /// Đọc nhiều tập kết quả từ GridReader theo danh sách các kiểu dữ liệu
        /// </summary>
        private async Task<List<List<object>>> ReadMultipleResults(SqlMapper.GridReader multi, List<Type> types)
        {
            var results = new List<List<object>>();
            foreach (var type in types)
            {
                var result = await multi.ReadAsync(type);
                results.Add(result.Cast<object>().ToList());
            }
            return results;
        }
        #endregion
    }
}
