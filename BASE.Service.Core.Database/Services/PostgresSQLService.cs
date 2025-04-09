using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using Dapper;
using BASE.Service.Core.Services;
using Npgsql;
using BASE.Service.Core.Attribute;

namespace BASE.Service.Core.Database
{
    public partial class PostgresSQLService : IPostgresSQLService
    {

        #region Methods connect

        /// <summary>
        /// Lấy chuỗi kết nối
        /// </summary>
        public string GetConnectionString(Guid databaseID)
        {
            return "User ID=postgres;Password=Lehung@181;Host=localhost;Port=5432;Database=db_employee;Pooling=true;";
        }

        /// <summary>
        /// Lấy kết nối
        /// </summary>
        /// <param name="cnnString"></param>
        /// <returns></returns>
        public IDbConnection GetConnection(Guid databaseID)
        {
            var cnnString = GetConnectionString(databaseID);
            if (string.IsNullOrEmpty(cnnString))
            {
                throw new Exception("Connection string is null or empty.");
            }
            var cnn = new NpgsqlConnection(cnnString);
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

        #region Methods helper

        /// <summary>
        /// Lấy tên bảng trong Database
        /// </summary>
        /// <param name="hasSchema"></param>
        /// <returns></returns>
        public string GetViewOrTableName<T>(bool hasSchema = true)
        {
            string tableName = "";
            var tableAttr = (ConfigTable)typeof(T).GetCustomAttributes(typeof(ConfigTable), false).FirstOrDefault();

            string viewOrTabble = !string.IsNullOrEmpty(tableAttr.ViewName) ? tableAttr.ViewName : tableAttr.TableName;
            if (hasSchema)
            {
                tableName = $"{tableAttr.Schema}.{viewOrTabble}";
            }
            else
            {
                tableName = $"{viewOrTabble}";
            }
            return tableName;
        }

        /// <summary>
        /// Lấy tên bảng trong Database
        /// </summary>
        /// <param name="hasSchema"></param>
        /// <returns></returns>
        public string GetTableName<T>(bool hasSchema = true)
        {
            string tableName = "";
            var tableAttr = (ConfigTable)typeof(T).GetCustomAttributes(typeof(ConfigTable), false).FirstOrDefault();
            if (hasSchema)
            {
                tableName = $"{tableAttr.Schema}.{tableAttr.TableName}";
            }
            else
            {
                tableName = $"{tableAttr.TableName}";
            }
            return tableName;
        }

        /// <summary>
        /// Lấy ra khóa chính của Model
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của model</typeparam>
        /// <returns>Tên trường khóa chính</returns>
        public string GetPrimaryKeyFiled<T>()
        {
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var keyAttribute = property.GetCustomAttribute<KeyAttribute>();
                if (keyAttribute != null)
                {
                    return property.Name;
                }
            }
            return string.Empty;
        }

        #endregion

        #region Methods get

        /// <summary>
        /// Lấy dữ liệu theo ID
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu trả về</typeparam>
        /// <param name="databaseID">ID của database</param>
        /// <param name="id">ID cần truy vấn</param>
        /// <returns>Dữ liệu theo kiểu T</returns>
        public T GetByID<T>(Guid databaseID, Guid id)
        {
            return GetByListID<T>(databaseID, new List<Guid> { id }).FirstOrDefault();
        }

        /// <summary>
        /// Lấy dữ liệu theo danh sách ID
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu trả về</typeparam>
        /// <param name="databaseID">ID của database</param>
        /// <param name="ids">Danh sách ID cần truy vấn</param>
        /// <returns>Danh sách dữ liệu theo kiểu T</returns>
        public List<T> GetByListID<T>(Guid databaseID, List<Guid> ids)
        {
            IDbConnection cnn = null;
            try
            {
                cnn = GetConnection(databaseID);
                OpenConnection(cnn);
                string tableName = GetViewOrTableName<T>();
                string primaryKey = GetPrimaryKeyFiled<T>();
                string script = $"select * from {tableName} where {primaryKey} in(:ids);";
                var param = new Dictionary<string, object>();
                param.Add("ids", ids);
                return Query<T>(databaseID, script, param, CommandType.Text).ToList();
            }
            finally
            {
                CloseConnection(cnn);
            }
        }

        #endregion

        #region Methods query

        /// <summary>
        /// Truy vấn lấy dữ liệu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseID"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public IEnumerable<dynamic> Query(Guid databaseID, string sql, object param = null, CommandType? commandType = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null)
        {
            IDbConnection cnn = null;
            try
            {
                cnn = GetConnection(databaseID);
                OpenConnection(cnn);
                return cnn.Query(sql, param, transaction, buffered, commandTimeout, commandType);
            }
            finally
            {
                CloseConnection(cnn);
            }
        }

        /// <summary>
        /// Truy vấn lấy dữ liệu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseID"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public List<T> Query<T>(Guid databaseID, string sql, object param = null, CommandType? commandType = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null)
        {
            IDbConnection cnn = null;
            try
            {
                cnn = GetConnection(databaseID);
                OpenConnection(cnn);
                return cnn.Query<T>(sql, param, transaction, buffered, commandTimeout, commandType).ToList();
            }
            finally
            {
                CloseConnection(cnn);
            }
        }

        /// <summary>
        /// Truy vấn lấy nhiều dữ liệu
        /// </summary>
        /// <param name="databaseID">ID của database</param>
        /// <param name="types">Danh sách Type để map dữ liệu</param>
        /// <param name="sql">Câu lệnh SQL</param>
        /// <param name="param">Tham số</param>
        /// <param name="commandType">Loại command</param>
        /// <param name="transaction">Transaction</param>
        /// <param name="commandTimeout">Timeout</param>
        /// <returns>Danh sách các list object tương ứng với các type</returns>
        public List<List<object>> QueryMultiple(Guid databaseID, List<Type> types, string sql, object param = null, CommandType? commandType = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            IDbConnection cnn = null;
            try
            {
                cnn = GetConnection(databaseID);
                OpenConnection(cnn);
                var reader = cnn.QueryMultiple(sql, param, transaction, commandTimeout, commandType);
                if (reader == null)
                {
                    return new List<List<object>>();
                }

                var results = new List<List<object>>();

                foreach (var type in types)
                {
                    var method = typeof(SqlMapper.GridReader).GetMethod("Read").MakeGenericMethod(type);
                    var result = method.Invoke(reader, null);
                    var list = ((IEnumerable<object>)result).ToList();
                    results.Add(list);
                }

                return results;
            }
            finally
            {
                CloseConnection(cnn);
            }
        }

        #endregion

        #region Methods execute

        /// <summary>
        /// Thực thi một câu lệnh
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public int Execute(Guid databaseID, string sql, object param = null, CommandType? commandType = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            IDbConnection cnn = null;
            try
            {
                cnn = GetConnection(databaseID);
                OpenConnection(cnn);
                return cnn.Execute(sql, param, transaction, commandTimeout, commandType);
            }
            finally
            {
                CloseConnection(cnn);
            }
        }

        /// <summary>
        /// Thực thi một câu lệnh
        /// </summary>
        /// <param name="databaseID"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public object ExecuteScalar(Guid databaseID, string sql, object param = null, CommandType commandType = CommandType.Text, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            IDbConnection cnn = null;
            try
            {
                cnn = GetConnection(databaseID);
                OpenConnection(cnn);
                return cnn.ExecuteScalar(sql, param, transaction, commandTimeout, commandType);
            }
            finally
            {
                CloseConnection(cnn);
            }
        }

        #endregion

        #region Methods CRUD

        /// <summary>
        /// Thực hiện insert dữ liệu
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="databaseID">ID của database</param>
        /// <param name="record">Đối tượng cần insert</param>
        /// <returns>Số bản ghi được insert</returns>
        public bool Insert<T>(Guid databaseID, T record)
        {
            IDbConnection cnn = null;
            try
            {
                cnn = GetConnection(databaseID);
                OpenConnection(cnn);

                var tableName = GetTableName<T>();
                var properties = typeof(T).GetProperties()
                    .Where(p => !p.GetCustomAttributes(typeof(NotMappedAttribute), false).Any());

                string sql = $@"INSERT INTO {tableName} ({string.Join(", ", properties.Select(p => p.Name))}) 
                    VALUES ({string.Join(", ", properties.Select(p => $":{p.Name}"))})";

                var parameters = new DynamicParameters();
                foreach (var prop in properties)
                {
                    parameters.Add(prop.Name, prop.GetValue(record));
                }

                return Execute(databaseID, sql, parameters) > 0;
            }
            finally
            {
                CloseConnection(cnn);
            }
        }

        /// <summary>
        /// Cập nhật dữ liệu theo khóa chính
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="databaseID">ID của database</param>
        /// <param name="record">Đối tượng cần cập nhật</param>
        /// <returns>Số bản ghi được cập nhật</returns>
        public bool Update<T>(Guid databaseID, T record)
        {
            IDbConnection cnn = null;
            try
            {
                cnn = GetConnection(databaseID);
                OpenConnection(cnn);

                var tableName = GetTableName<T>();
                var properties = typeof(T).GetProperties()
                    .Where(p => !p.GetCustomAttributes(typeof(NotMappedAttribute), false).Any());
                var primaryKey = GetPrimaryKeyFiled<T>();

                // Build SET clause
                var setColumns = string.Join(", ", properties
                    .Where(p => p.Name != primaryKey)
                    .Select(p => $"{p.Name} = :{p.Name}"));

                var sql = $"UPDATE {tableName} SET {setColumns} WHERE {primaryKey} = :{primaryKey}";

                var parameters = new DynamicParameters();
                foreach (var prop in properties)
                {
                    parameters.Add(prop.Name, prop.GetValue(record));
                }

                return Execute(databaseID, sql, parameters) > 0;
            }
            finally
            {
                CloseConnection(cnn);
            }
        }

        /// <summary>
        /// Xóa dữ liệu theo khóa chính
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="databaseID">ID của database</param>
        /// <param name="record">Đối tượng cần xóa</param>
        /// <returns>true nếu xóa thành công, false nếu không xóa được bản ghi nào</returns>
        public bool Delete<T>(Guid databaseID, T record)
        {
            IDbConnection cnn = null;
            try
            {
                cnn = GetConnection(databaseID);
                OpenConnection(cnn);

                string tableName = GetTableName<T>(),
                    primaryKey = GetPrimaryKeyFiled<T>();

                var properties = typeof(T).GetProperties();
                var property = properties.FirstOrDefault(p => p.Name == primaryKey);
                var primaryKeyValue = property?.GetValue(record);

                var sql = $"DELETE FROM {tableName} WHERE {primaryKey} = :id";
                var parameters = new DynamicParameters();
                parameters.Add("id", primaryKeyValue);

                return Execute(databaseID, sql, parameters) > 0;
            }
            finally
            {
                CloseConnection(cnn);
            }
        }

        #endregion

    }
}
