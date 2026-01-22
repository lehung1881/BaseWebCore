using BASE.Service.Core.Model;
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

        /// <summary>
        /// Thực hiện truy vấn dữ liệu từ database sử dụng command text
        /// </summary>
        Task<List<T>> QueryUsingCommandText<T>(Guid databaseID, string commandText, Dictionary<string, object> param);

        /// <summary>
        /// Thực hiện truy vấn dữ liệu từ database sử dụng command text với connection có sẵn
        /// </summary>
        Task<List<T>> QueryUsingCommandText<T>(IDbConnection cnn, string commandText, Dictionary<string, object> param);

        /// <summary>
        /// Thực thi lệnh SQL (INSERT, UPDATE, DELETE) sử dụng command text
        /// </summary>
        Task<bool> ExecuteUsingCommandText(Guid databaseID, string commandText, Dictionary<string, object> param);

        /// <summary>
        /// Thực thi lệnh SQL (INSERT, UPDATE, DELETE) sử dụng command text với connection có sẵn
        /// </summary>
        Task<bool> ExecuteUsingCommandText(IDbConnection cnn, string commandText, Dictionary<string, object> param);

        /// <summary>
        /// Thực hiện truy vấn nhiều tập kết quả từ database sử dụng command text
        /// </summary>
        Task<List<List<object>>> QueryMultipleUsingCommandText(Guid databaseID, string commandText, List<Type> types, Dictionary<string, object> param);

        /// <summary>
        /// Thực hiện truy vấn nhiều tập kết quả từ database sử dụng command text với connection có sẵn
        /// </summary>
        Task<List<List<object>>> QueryMultipleUsingCommandText(IDbConnection cnn, string commandText, List<Type> types, Dictionary<string, object> param);

        /// <summary>
        /// Thực hiện truy vấn dữ liệu từ database sử dụng stored procedure
        /// </summary>
        Task<List<T>> QueryUsingStoreProcedure<T>(Guid databaseID, string procedureName, Dictionary<string, object> param);

        /// <summary>
        /// Thực hiện truy vấn dữ liệu từ database sử dụng stored procedure với connection có sẵn
        /// </summary>
        Task<List<T>> QueryUsingStoreProcedure<T>(IDbConnection cnn, string procedureName, Dictionary<string, object> param);

        /// <summary>
        /// Thực thi stored procedure (INSERT, UPDATE, DELETE)
        /// </summary>
        Task<bool> ExecuteUsingStoreProcedure(Guid databaseID, string procedureName, Dictionary<string, object> param);

        /// <summary>
        /// Thực thi stored procedure (INSERT, UPDATE, DELETE) với connection có sẵn
        /// </summary>
        Task<bool> ExecuteUsingStoreProcedure(IDbConnection cnn, string procedureName, Dictionary<string, object> param);

        /// <summary>
        /// Thực hiện truy vấn nhiều tập kết quả từ database sử dụng stored procedure
        /// </summary>
        Task<List<List<object>>> QueryMultipleUsingStoreProcedure(Guid databaseID, string procedureName, List<Type> types, Dictionary<string, object> param);

        /// <summary>
        /// Thực hiện truy vấn nhiều tập kết quả từ database sử dụng stored procedure với connection có sẵn
        /// </summary>
        Task<List<List<object>>> QueryMultipleUsingStoreProcedure(IDbConnection cnn, string procedureName, List<Type> types, Dictionary<string, object> param);

        /// <summary>
        /// Lấy theo ID
        /// </summary>
        Task<object> GetDataByID(Guid databaseID, Type modelType, string id, string columns = "*");

        /// <summary>
        /// Lấy theo ID
        /// </summary>
        Task<BaseModel> GetDataByID(Guid databaseID, Type modelType, string id);

        /// <summary>
        /// Lấy theo ID
        /// </summary>
        Task<T> GetDataByID<T>(Guid databaseID, string id) where T : BaseModel;
    }
}
