using System;
using System.Collections.Generic;
using System.Data;

namespace BASE.Service.Core.Services
{
    public interface IMySQLService
    {
        /// <summary>
        /// Truy vấn lấy dữ liệu
        /// </summary>
        /// <param name="databaseID">ID của database</param>
        /// <param name="sql">Câu lệnh SQL</param>
        /// <param name="param">Tham số</param>
        /// <param name="commandType">Loại command</param>
        /// <param name="transaction">Transaction</param>
        /// <param name="buffered">Buffered</param>
        /// <param name="commandTimeout">Timeout</param>
        IEnumerable<dynamic> Query(Guid databaseID, string sql, object param = null, CommandType? commandType = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null);

        /// <summary>
        /// Truy vấn lấy dữ liệu
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu trả về</typeparam>
        /// <param name="databaseID">ID của database</param>
        /// <param name="sql">Câu lệnh SQL</param>
        /// <param name="param">Tham số</param>
        /// <param name="commandType">Loại command</param>
        /// <param name="transaction">Transaction</param>
        /// <param name="buffered">Buffered</param>
        /// <param name="commandTimeout">Timeout</param>
        List<T> Query<T>(Guid databaseID, string sql, object param = null, CommandType? commandType = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null);

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
        List<List<object>> QueryMultiple(Guid databaseID, List<Type> types, string sql, object param = null, CommandType? commandType = null, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// Thực thi một câu lệnh
        /// </summary>
        /// <param name="databaseID">ID của database</param>
        /// <param name="sql">Câu lệnh SQL</param>
        /// <param name="param">Tham số</param>
        /// <param name="commandType">Loại command</param>
        /// <param name="transaction">Transaction</param>
        /// <param name="commandTimeout">Timeout</param>
        int Execute(Guid databaseID, string sql, object param = null, CommandType? commandType = null, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// Thực thi một câu lệnh trả về giá trị
        /// </summary>
        /// <param name="databaseID">ID của database</param>
        /// <param name="sql">Câu lệnh SQL</param>
        /// <param name="param">Tham số</param>
        /// <param name="commandType">Loại command</param>
        /// <param name="transaction">Transaction</param>
        /// <param name="commandTimeout">Timeout</param>
        object ExecuteScalar(Guid databaseID, string sql, object param = null, CommandType commandType = CommandType.Text, IDbTransaction transaction = null, int? commandTimeout = null);

        /// <summary>
        /// Thực hiện insert dữ liệu
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="databaseID">ID của database</param>
        /// <param name="record">Đối tượng cần insert</param>
        /// <returns>true nếu insert thành công, false nếu không insert được bản ghi nào</returns>
        bool Insert<T>(Guid databaseID, T record);

        /// <summary>
        /// Cập nhật dữ liệu theo khóa chính
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="databaseID">ID của database</param>
        /// <param name="record">Đối tượng cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công, false nếu không cập nhật được bản ghi nào</returns>
        bool Update<T>(Guid databaseID, T record);

        /// <summary>
        /// Lấy dữ liệu theo ID
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu trả về</typeparam>
        /// <param name="databaseID">ID của database</param>
        /// <param name="id">ID cần truy vấn</param>
        /// <returns>Dữ liệu theo kiểu T</returns>
        T GetByID<T>(Guid databaseID, Guid id);

        /// <summary>
        /// Lấy dữ liệu theo danh sách ID
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu trả về</typeparam>
        /// <param name="databaseID">ID của database</param>
        /// <param name="ids">Danh sách ID cần truy vấn</param>
        /// <returns>Danh sách dữ liệu theo kiểu T</returns>
        List<T> GetByListID<T>(Guid databaseID, List<Guid> ids);

        /// <summary>
        /// Xóa dữ liệu theo khóa chính
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="databaseID">ID của database</param>
        /// <param name="record">Đối tượng cần xóa</param>
        /// <returns>true nếu xóa thành công, false nếu không xóa được bản ghi nào</returns>
        bool Delete<T>(Guid databaseID, T record);

        /// <summary>
        /// Cập nhật một trường cụ thể của bản ghi theo ID
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="databaseID">ID của database</param>
        /// <param name="record">Đối tượng chứa dữ liệu và ID cần cập nhật</param>
        /// <param name="fieldName">Tên trường cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công, false nếu không cập nhật được bản ghi nào</returns>
        bool UpdateFieldByID<T>(Guid databaseID, T record, string fieldName);

        /// <summary>
        /// Xóa dữ liệu theo một trường cụ thể
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="databaseID">ID của database</param>
        /// <param name="record">Đối tượng chứa dữ liệu làm điều kiện xóa</param>
        /// <param name="fieldDelete">Tên trường làm điều kiện xóa</param>
        /// <returns>true nếu xóa thành công, false nếu không xóa được bản ghi nào</returns>
        bool DeleteByField<T>(Guid databaseID, T record, string fieldDelete);
    }
}
